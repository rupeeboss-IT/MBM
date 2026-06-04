namespace RB_Website_API.Services;

public interface IReferralService
{
    /// <summary>Stores optional referral code for a payment order (DB1).</summary>
    Task SaveReferralForOrderAsync(Guid paymentOrderId, string? referralCode, CancellationToken ct);
}
