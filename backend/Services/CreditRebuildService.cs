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

public sealed class CreditRebuildService : ICreditRebuildService
{
    private static readonly string[] BannedNameWords =
    [
        "test", "dummy", "fake", "unknown", "n/a", "na", "null", "none", "abcd", "asdf",
    ];

    private readonly AppDbContext _db;
    private readonly ReferralDbContext _referralDb;
    private readonly IEmployeeValidationService _employees;
    private readonly ReferralSettings _settings;
    private readonly ILogger<CreditRebuildService> _log;

    public CreditRebuildService(
        AppDbContext db,
        ReferralDbContext referralDb,
        IEmployeeValidationService employees,
        IOptions<ReferralSettings> settings,
        ILogger<CreditRebuildService> log)
    {
        _db = db;
        _referralDb = referralDb;
        _employees = employees;
        _settings = settings.Value;
        _log = log;
    }

    public Task<(bool Success, string? Message, int? LeadId)> SubmitEnquiryAsync(
        string fullName,
        string mobile,
        string email,
        bool consentAccepted,
        string? advisorCode,
        CancellationToken ct)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        return strategy.ExecuteAsync(() => SubmitCoreAsync(
            fullName, mobile, email, consentAccepted, advisorCode, ct));
    }

    private async Task<(bool Success, string? Message, int? LeadId)> SubmitCoreAsync(
        string fullName,
        string mobile,
        string email,
        bool consentAccepted,
        string? advisorCode,
        CancellationToken ct)
    {
        var nameErr = ValidateName(fullName);
        if (nameErr != null) return (false, nameErr, null);

        var mobileErr = ValidateMobile(mobile);
        if (mobileErr != null) return (false, mobileErr, null);

        var emailErr = ValidateEmail(email);
        if (emailErr != null) return (false, emailErr, null);

        if (consentAccepted != true)
            return (false, "Consent is required to submit this enquiry.", null);

        var mbmCatalog = GetDatabaseName(_db.Database.GetConnectionString()) ?? "MBM";

        var normalizedAdvisor = NormalizeAdvisorCode(advisorCode);
        var empCode = ResolveDefaultEmpCode();
        var leadType = _settings.LeadType;
        int? brokerId = null;

        var referral = await _employees.ResolveReferralForLeadAsync(normalizedAdvisor, ct);
        if (referral is not null && !string.IsNullOrWhiteSpace(referral.EmpCode))
        {
            empCode = referral.EmpCode;
            if (referral.ReferralType == ReferralType.RBA)
            {
                leadType = "RBA";
                brokerId = referral.BrokerId;
            }
        }

        var campaignName = ResolveCreditRebuildCampaignName();
        var nowLocal = DateTime.Now;
        var consentAt = DateTime.Now;

        var lead = new LeadData
        {
            name = Truncate(fullName.Trim(), 100),
            mobile = Truncate(mobile.Trim(), 50),
            email = Truncate(email.Trim(), 300),
            productid = _settings.ProductId,
            Pincode = null,
            profession = _settings.DefaultProfession,
            source_id = _settings.SourceId,
            lead_source = Truncate(_settings.LeadSource, 500),
            lead_type = Truncate(leadType, 50),
            campaignName = Truncate(campaignName, 200),
            emp_code = Truncate(empCode, 20),
            broker_id = brokerId,
            Lead_Status_id = _settings.LeadStatusId,
            sysdate = nowLocal,
            lead_date = nowLocal,
            Created_Datetime = nowLocal,
            remark = "MSME Bharat Manch — Credit Rebuild enquiry",
        };

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

            var enquiry = new CreditRebuildEnquiry
            {
                Id = leadId,
                FullName = fullName.Trim(),
                Phone = mobile.Trim(),
                Email = email.Trim(),
                AdvisorCode = normalizedAdvisor,
                ConsentAccepted = true,
                ConsentAcceptedAt = consentAt,
                CreatedAt = consentAt,
            };

            connection.ChangeDatabase(mbmCatalog);

            await _db.CreditRebuildEnquiries.AddAsync(enquiry, ct);
            await _db.SaveChangesAsync(ct);

            await dbTransaction.CommitAsync(ct);

            _log.LogInformation(
                "Credit rebuild enquiry saved: Lead_id {LeadId}, campaign {Campaign}, emp_code {EmpCode}, phone {Phone}",
                leadId,
                campaignName,
                empCode,
                enquiry.Phone);

            return (
                true,
                "Thank you! We have received your enquiry. Our credit rebuild team will contact you shortly.",
                leadId);
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(ct);
            _log.LogError(ex, "Credit rebuild enquiry transaction failed for phone {Phone}.", mobile);
            return (false, "Unable to submit your enquiry. Please try again.", null);
        }
        finally
        {
            _db.Database.SetDbConnection(null);
        }
    }

    private string ResolveCreditRebuildCampaignName()
    {
        var configured = _settings.CreditRebuildCampaignName?.Trim();
        return string.IsNullOrWhiteSpace(configured) ? "MSME credit rebuild" : configured;
    }

    private string ResolveDefaultEmpCode()
    {
        var empCode = _settings.DefaultEmployeeReferralCode?.Trim();
        return string.IsNullOrWhiteSpace(empCode) ? "RB600000251" : empCode;
    }

    private static string? NormalizeAdvisorCode(string? advisorCode)
    {
        var trimmed = advisorCode?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
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
        if (name.Length == 0) return "Your name is required.";
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

    private static string? ValidateEmail(string v)
    {
        var e = (v ?? "").Trim();
        if (e.Length == 0) return "Email address is required.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(e, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            return "Enter a valid email address.";
        return null;
    }
}
