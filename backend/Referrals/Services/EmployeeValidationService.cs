using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;

namespace RB_Website_API.Referrals.Services;

public sealed class EmployeeValidationService : IEmployeeValidationService
{
    private readonly ReferralDbContext _db;
    private readonly ReferralSettings _settings;
    private readonly ILogger<EmployeeValidationService> _logger;

    public EmployeeValidationService(
        ReferralDbContext db,
        IOptions<ReferralSettings> settings,
        ILogger<EmployeeValidationService> logger)
    {
        _db = db;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<EmployeeValidationResult> ValidateReferralCodeAsync(string referralCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return new EmployeeValidationResult(false, "Referral code is required.");

        var code = referralCode.Trim();
        try
        {
            var emp = await _db.EmployeeMaster.AsNoTracking()
                .Where(e => e.EmpId == Convert.ToInt64(code))
                .Select(e => new { e.Emp_Code, e.Emp_Name, e.EmpId, e.Is_Active })
                .FirstOrDefaultAsync(ct);

            if (emp is null)
                return new EmployeeValidationResult(false, "Invalid referral code.");

            if (!emp.Is_Active)
                return new EmployeeValidationResult(false, "Referral code is inactive.");

            return new EmployeeValidationResult(true, "Valid referral code.", emp.EmpId, emp.Emp_Name, emp.Emp_Code);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating referral code.");
            return new EmployeeValidationResult(false, "Could not validate referral code right now. Please try again.");
        }
    }

    public async Task<ResolvedEmployee?> ResolveEmployeeForLeadAsync(string? referralCode, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(referralCode))
        {
            var validated = await ValidateReferralCodeAsync(referralCode, ct);
            if (validated.IsValid && validated.EmployeeId is not null)
            {
                return new ResolvedEmployee(
                    validated.EmployeeId.Value,
                    validated.EmployeeName ?? "",
                    validated.ReferralCode ?? referralCode.Trim(),
                    UsedDefaultEmployee: false);
            }
        }

        var defaultCode = _settings.DefaultEmployeeReferralCode?.Trim();
        if (string.IsNullOrWhiteSpace(defaultCode))
        {
            _logger.LogWarning("DefaultEmployeeReferralCode is not configured.");
            return null;
        }

        var defaultEmp = await _db.EmployeeMaster.AsNoTracking()
            .Where(e => e.Emp_Code == defaultCode && e.Is_Active)
            .Select(e => new { e.Emp_Code, e.Emp_Name, e.EmpId })
            .FirstOrDefaultAsync(ct);

        if (defaultEmp is null)
        {
            _logger.LogWarning("Default employee not found or inactive for code {Code}.", defaultCode);
            return null;
        }

        return new ResolvedEmployee(
            defaultEmp.EmpId,
            defaultEmp.Emp_Name,
            defaultEmp.Emp_Code,
            UsedDefaultEmployee: true);
    }
}

