namespace RB_Website_API.Referrals.Services;

public interface IEmployeeValidationService
{
    Task<EmployeeValidationResult> ValidateReferralCodeAsync(string referralCode, CancellationToken ct);

    /// <summary>Uses referral code when valid; otherwise default employee from configuration.</summary>
    Task<ResolvedEmployee?> ResolveEmployeeForLeadAsync(string? referralCode, CancellationToken ct);
}

public sealed record ResolvedEmployee(
    int EmployeeId,
    string EmployeeName,
    string ReferralCode,
    bool UsedDefaultEmployee);

public sealed record EmployeeValidationResult(
    bool IsValid,
    string? Message = null,
    int? EmployeeId = null,
    string? EmployeeName = null,
    string? ReferralCode = null);

