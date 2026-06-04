using System.Data;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Referrals.Models;
using RB_Website_API.Referrals.Services;

namespace RB_Website_API.Services;

public sealed class LoanApplicationService : ILoanApplicationService
{
    private static readonly HashSet<int> AllowedLoanTypeIds =
    [
        7, 8, 9, 10, 11, 12, 13, 1003, 1014, 1024, 1008,
    ];

    private static readonly string[] BannedNameWords =
    [
        "test", "dummy", "fake", "unknown", "n/a", "na", "null", "none", "abcd", "asdf",
    ];

    private readonly AppDbContext _db;
    private readonly ReferralDbContext _referralDb;
    private readonly IEmployeeValidationService _employees;
    private readonly ReferralSettings _settings;
    private readonly ILogger<LoanApplicationService> _log;

    public LoanApplicationService(
        AppDbContext db,
        ReferralDbContext referralDb,
        IEmployeeValidationService employees,
        IOptions<ReferralSettings> settings,
        ILogger<LoanApplicationService> log)
    {
        _db = db;
        _referralDb = referralDb;
        _employees = employees;
        _settings = settings.Value;
        _log = log;
    }

    public Task<(bool Success, string? Message, int? LeadId)> SubmitAsync(
        string fullName,
        string mobile,
        string? email,
        string pincode,
        int loanTypeId,
        string loanAmount,
        bool consentAccepted,
        string? referralCode,
        CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => SubmitCoreAsync(
            fullName, mobile, email, pincode, loanTypeId, loanAmount, consentAccepted, referralCode, ct));
    }

    private async Task<(bool Success, string? Message, int? LeadId)> SubmitCoreAsync(
        string fullName,
        string mobile,
        string? email,
        string pincode,
        int loanTypeId,
        string loanAmount,
        bool consentAccepted,
        string? referralCode,
        CancellationToken ct)
    {
        var nameErr = ValidateName(fullName);
        if (nameErr != null) return (false, nameErr, null);

        var mobileErr = ValidateMobile(mobile);
        if (mobileErr != null) return (false, mobileErr, null);

        var emailErr = ValidateEmail(email);
        if (emailErr != null) return (false, emailErr, null);

        var pinErr = ValidatePincode(pincode);
        if (pinErr != null) return (false, pinErr, null);

        if (!AllowedLoanTypeIds.Contains(loanTypeId))
            return (false, "Please select a valid loan type.", null);

        var amtErr = ValidateLoanAmount(loanAmount, out var amount);
        if (amtErr != null) return (false, amtErr, null);

        if (consentAccepted != true)
            return (false, "Consent is required to submit this enquiry.", null);

        try
        {
            await LoanApplicationSchemaBootstrap.EnsureAsync(_db, _log, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "LoanApplications table is not available in MBM.");
            return (false, "Unable to submit your loan application. Please try again.", null);
        }

        // Resolve before sharing the RBMAIN connection with AppDbContext (GetConnectionString() is unreliable after SetDbConnection).
        var mbmCatalog = GetDatabaseName(_db.Database.GetConnectionString()) ?? "MBM";

        var empCode = _settings.DefaultEmployeeReferralCode?.Trim();
        if (string.IsNullOrWhiteSpace(empCode))
            empCode = "RB600000251";

        var employee = await _employees.ResolveEmployeeForLeadAsync(referralCode, ct);
        if (employee is not null && !string.IsNullOrWhiteSpace(employee.ReferralCode))
            empCode = employee.ReferralCode;

        var nowLocal = DateTime.Now;
        var consentAt = DateTime.Now;

        var lead = new LeadData
        {
            name = Truncate(fullName.Trim(), 100),
            mobile = Truncate(mobile.Trim(), 50),
            email = Truncate(string.IsNullOrWhiteSpace(email) ? null : email.Trim(), 300),
            productid = loanTypeId,
            Pincode = Truncate(pincode.Trim(), 10),
            profession = _settings.DefaultProfession,
            source_id = _settings.SourceId,
            lead_source = Truncate(_settings.LeadSource, 500),
            lead_type = Truncate(_settings.LeadType, 50),
            campaignName = Truncate(_settings.CampaignName, 200),
            emp_code = Truncate(empCode, 20),
            Lead_Status_id = _settings.LeadStatusId,
            sysdate = nowLocal,
            lead_date = nowLocal,
            Created_Datetime = nowLocal,
        };

        // One connection + local transaction (no MSDTC / implicit distributed transactions).
        await using var connection = _referralDb.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(ct);

        await using var dbTransaction = await connection.BeginTransactionAsync(ct);

        _db.Database.SetDbConnection(connection);
        await _referralDb.Database.UseTransactionAsync(dbTransaction, ct);
        await _db.Database.UseTransactionAsync(dbTransaction, ct);

        try
        {
            _referralDb.LeadData.Add(lead);
            await _referralDb.SaveChangesAsync(ct);

            var leadId = await ResolveLeadIdAsync(lead, ct);
            if (leadId <= 0)
                throw new InvalidOperationException("Failed to obtain lead_id after lead_data insert.");

            var loan = new LoanApplication
            {
                Id = leadId,
                FullName = fullName.Trim(),
                Phone = mobile.Trim(),
                Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
                Pincode = pincode.Trim(),
                LoanTypeId = loanTypeId,
                LoanAmount = amount,
                ConsentAccepted = true,
                ConsentAcceptedAt = consentAt,
                CreatedAt = consentAt,
            };

            connection.ChangeDatabase(mbmCatalog);

            await _db.LoanApplications.AddAsync(loan, ct);
            await _db.SaveChangesAsync(ct);

            await dbTransaction.CommitAsync(ct);

            _log.LogInformation(
                "Loan application saved: Lead_id {LeadId}, productid {ProductId}, phone {Phone}",
                leadId,
                loanTypeId,
                loan.Phone);

            return (true, "Application submitted successfully. Our team will contact you within 24 hours.", leadId);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(ct);
            _log.LogError(ex, "Loan application transaction failed for phone {Phone}.", mobile);
            return (false, "Unable to submit your loan application. Please try again.", null);
        }
        finally
        {
            _db.Database.SetDbConnection(null);
        }
    }

    private async Task<int> ResolveLeadIdAsync(LeadData lead, CancellationToken ct)
    {
        if (lead.Lead_id > 0)
            return lead.Lead_id;

        var scopeId = await _referralDb.Database
            .SqlQueryRaw<int>("SELECT CAST(SCOPE_IDENTITY() AS int) AS [Value]")
            .FirstOrDefaultAsync(ct);

        if (scopeId > 0)
            lead.Lead_id = scopeId;

        return lead.Lead_id;
    }

    private static string? GetDatabaseName(string? connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString)) return null;
        try
        {
            var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connectionString);
            return string.IsNullOrWhiteSpace(builder.InitialCatalog) ? null : builder.InitialCatalog;
        }
        catch
        {
            return null;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        return s.Length <= maxLength ? s : s[..maxLength];
    }

    private static string? ValidateName(string nameRaw)
    {
        var name = (nameRaw ?? "").Trim();
        if (name.Length == 0) return "Full name is required.";
        if (name.Length < 2) return "Please enter your full name.";
        if (name.Any(c => !char.IsLetter(c) && c != ' ')) return "Name can contain only letters and spaces.";
        if (name.Any(char.IsDigit)) return "Name cannot contain numbers.";
        if (System.Text.RegularExpressions.Regex.IsMatch(name.Replace(" ", ""), @"(.)\1{3,}", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return "Name looks invalid (repeated characters).";

        var words = name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => BannedNameWords.Contains(w)))
            return "Please enter a valid name.";

        return null;
    }

    private static string? ValidateMobile(string v)
    {
        var m = (v ?? "").Trim();
        if (m.Length == 0) return "Mobile number is required.";
        if (!m.All(char.IsDigit)) return "Mobile number must be numeric.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(m, @"^[6-9]\d{9}$"))
            return "Enter a valid 10-digit Indian mobile number (starts with 6-9).";
        return null;
    }

    private static string? ValidateEmail(string? v)
    {
        var e = (v ?? "").Trim();
        if (e.Length == 0) return null;
        if (!System.Text.RegularExpressions.Regex.IsMatch(e, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            return "Enter a valid email address.";
        return null;
    }

    private static string? ValidatePincode(string v)
    {
        var p = (v ?? "").Trim();
        if (p.Length == 0) return "Pincode is required.";
        if (!p.All(char.IsDigit)) return "Pincode must be numeric.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(p, @"^[1-9]\d{5}$"))
            return "Enter a valid 6-digit Indian pincode.";
        return null;
    }

    private static string? ValidateLoanAmount(string v, out long amount)
    {
        amount = 0;
        var a = (v ?? "").Trim();
        if (a.Length == 0) return "Loan amount is required.";
        if (!a.All(char.IsDigit)) return "Loan amount must be numeric.";
        if (!long.TryParse(a, out amount) || amount <= 0) return "Enter a valid loan amount.";
        return null;
    }
}
