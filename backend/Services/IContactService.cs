namespace RB_Website_API.Services;

public interface IContactService
{
    Task<(bool Success, string? Message, int? SubmissionId)> SubmitAsync(
        string fullName,
        string mobile,
        string email,
        int subjectId,
        string message,
        bool consentAccepted,
        string? leadSource,
        CancellationToken ct);

    Task<(bool Success, string? Message, int? SubmissionId)> SubmitCallbackAsync(
        string fullName,
        string mobile,
        int subjectId,
        string message,
        bool consentAccepted,
        CancellationToken ct);
}
