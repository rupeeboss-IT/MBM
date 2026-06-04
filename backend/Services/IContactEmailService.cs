using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed record ContactEmailResult(bool CustomerNotified, bool SupportNotified);

public interface IContactEmailService
{
    Task<ContactEmailResult> TrySendEmailsAsync(ContactSubmission submission, CancellationToken ct);
}
