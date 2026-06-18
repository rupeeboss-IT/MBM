using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Referrals.Models;

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

    public async Task<ReferralValidationResult> ValidateReferralCodeAsync(string referralCode, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(referralCode))
            return new ReferralValidationResult(false, "Referral code is required.");

        var code = referralCode.Trim();
        try
        {
            if (code.Length >= 2 && code.StartsWith("RB", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(code[2..], out var empId))
                    return new ReferralValidationResult(false, "Invalid referral code.");

                var emp = await _db.EmployeeMaster.AsNoTracking()
                    .Where(e => e.EmpId == empId)
                    .Select(e => new { e.Emp_Code, e.Emp_Name, e.EmpId, e.Is_Active })
                    .FirstOrDefaultAsync(ct);

                if (emp is null)
                    return new ReferralValidationResult(false, "Invalid referral code.");

                if (!emp.Is_Active)
                    return new ReferralValidationResult(false, "Referral code is inactive.");

                return new ReferralValidationResult(
                    true,
                    "Valid referral code.",
                    ReferralType.Employee,
                    emp.Emp_Name,
                    emp.EmpId,
                    BrokerId: null,
                    emp.Emp_Code,
                    emp.Emp_Code);
            }

            if (code.Length >= 2 && code.StartsWith("GP", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(code[2..], out var brokerId))
                    return new ReferralValidationResult(false, "Invalid referral code.");

                var broker = await _db.BrokerMaster.AsNoTracking()
                    .Where(b => b.Broker_id == brokerId && b.Is_Active == 1)
                    .Select(b => new { b.Broker_id, b.Broker_Name, b.Emp_Code })
                    .FirstOrDefaultAsync(ct);

                if (broker is null)
                    return new ReferralValidationResult(false, "Invalid referral code.");

                return new ReferralValidationResult(
                    true,
                    "Valid referral code.",
                    ReferralType.RBA,
                    broker.Broker_Name,
                    EmployeeId: null,
                    broker.Broker_id,
                    code,
                    broker.Emp_Code);
            }

            return new ReferralValidationResult(false, "Invalid referral code.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating referral code.");
            return new ReferralValidationResult(false, "Could not validate referral code right now. Please try again.");
        }
    }

    public async Task<ResolvedReferral?> ResolveReferralForLeadAsync(string? referralCode, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(referralCode))
        {
            var validated = await ValidateReferralCodeAsync(referralCode, ct);
            if (validated.IsValid && validated.ReferralType is not null)
            {
                return new ResolvedReferral(
                    validated.ReferralType.Value,
                    validated.DisplayName ?? "",
                    validated.ReferralCode ?? referralCode.Trim(),
                    validated.EmpCode ?? "",
                    validated.EmployeeId,
                    validated.BrokerId,
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

        return new ResolvedReferral(
            ReferralType.Employee,
            defaultEmp.Emp_Name,
            defaultEmp.Emp_Code,
            defaultEmp.Emp_Code,
            defaultEmp.EmpId,
            BrokerId: null,
            UsedDefaultEmployee: true);
    }
}
