namespace RB_Website_API.Services;

public interface ILeadPushService
{
    /// <summary>
    /// Inserts lead into RBMain lead_data immediately after successful member registration.
    /// Idempotent per user. Resolves advisor/referral code to emp_code (or default employee).
    /// </summary>
    Task<bool> CreateLeadAfterRegistrationAsync(
        Guid userId,
        string? registrationSource,
        string? advisorCode,
        CancellationToken ct);

    /// <summary>
    /// Fallback: inserts lead after payment when no registration lead exists (legacy users).
    /// When a registration lead exists, reconciles checkout advisor onto the existing CRM lead if needed.
    /// Idempotent per payment order.
    /// </summary>
    Task<bool> CreateLeadAfterPaymentAsync(Guid paymentOrderId, CancellationToken ct);

    /// <summary>
    /// Inserts lead into RBMain lead_data after a contact page submission.
    /// Always assigns the default employee; lead_type is msmecontact.
    /// </summary>
    Task<bool> CreateLeadAfterContactSubmissionAsync(
        string fullName,
        string mobile,
        string? email,
        string? leadSource,
        string? remark,
        CancellationToken ct);
}
