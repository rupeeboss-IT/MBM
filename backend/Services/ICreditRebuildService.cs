namespace RB_Website_API.Services;

public interface ICreditRebuildService
{
    Task<(bool Success, string? Message, int? LeadId)> SubmitEnquiryAsync(
        string fullName,
        string mobile,
        string email,
        bool consentAccepted,
        string? advisorCode,
        CancellationToken ct);
}
