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
        CancellationToken ct);
}
