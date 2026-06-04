namespace RB_Website_API.Services;

public static class InvoiceNumber
{
    public static string ForPayment(Guid paymentId, DateTime paidAtUtc)
        => $"MBM-{paidAtUtc:yyyyMMdd}-{paymentId.ToString("N")[..8].ToUpperInvariant()}";
}
