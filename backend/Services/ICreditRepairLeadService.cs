namespace RB_Website_API.Services;

public interface ICreditRepairLeadService
{
    Task<(bool Success, string? Message, int? LeadId)> SubmitAsync(
        string fullName,
        string mobile,
        string email,
        bool consentAccepted,
        string? advisorCode,
        string correlationId,
        CancellationToken ct);
}

