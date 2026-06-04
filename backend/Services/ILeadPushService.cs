namespace RB_Website_API.Services;

public interface ILeadPushService
{
    /// <summary>
    /// Inserts lead into DB2 lead_data immediately after successful payment activation.
    /// Idempotent per payment order.
    /// </summary>
    Task<bool> CreateLeadAfterPaymentAsync(Guid paymentOrderId, CancellationToken ct);
}
