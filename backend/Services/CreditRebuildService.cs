namespace RB_Website_API.Services;

/// <summary>
/// Credit Rebuild page enquiries delegate to the credit repair pipeline
/// (Zoho webhook → CreditRepair_Lead → lead_data).
/// </summary>
public sealed class CreditRebuildService : ICreditRebuildService
{
    private readonly ICreditRepairLeadService _creditRepair;

    public CreditRebuildService(ICreditRepairLeadService creditRepair) => _creditRepair = creditRepair;

    public Task<(bool Success, string? Message, int? LeadId)> SubmitEnquiryAsync(
        string fullName,
        string mobile,
        string email,
        bool consentAccepted,
        string? advisorCode,
        CancellationToken ct)
    {
        var correlationId = Guid.NewGuid().ToString("n");
        return _creditRepair.SubmitAsync(
            fullName,
            mobile,
            email,
            consentAccepted,
            advisorCode,
            correlationId,
            ct);
    }
}
