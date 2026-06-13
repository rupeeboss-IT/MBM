using RB_Website_API.Referrals.Models;

namespace RB_Website_API.Referrals.Services;

public interface IEmployeeValidationService
{
    Task<ReferralValidationResult> ValidateReferralCodeAsync(string referralCode, CancellationToken ct);

    /// <summary>Uses referral code when valid; otherwise default employee from configuration.</summary>
    Task<ResolvedReferral?> ResolveReferralForLeadAsync(string? referralCode, CancellationToken ct);
}

public sealed record ResolvedReferral(
    ReferralType ReferralType,
    string DisplayName,
    string ReferralCode,
    string EmpCode,
    int? EmployeeId,
    int? BrokerId,
    bool UsedDefaultEmployee);

public sealed record ReferralValidationResult(
    bool IsValid,
    string? Message = null,
    ReferralType? ReferralType = null,
    string? DisplayName = null,
    int? EmployeeId = null,
    int? BrokerId = null,
    string? ReferralCode = null,
    string? EmpCode = null);
