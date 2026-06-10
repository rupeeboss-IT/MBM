using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class ContactService : IContactService
{
    private static readonly string[] BannedNameWords =
    [
        "test", "dummy", "fake", "unknown", "n/a", "na", "null", "none", "abcd", "asdf",
    ];

    private readonly AppDbContext _db;
    private readonly IContactEmailService _email;
    private readonly ILogger<ContactService> _log;

    public ContactService(AppDbContext db, IContactEmailService email, ILogger<ContactService> log)
    {
        _db = db;
        _email = email;
        _log = log;
    }

    public async Task<(bool Success, string? Message, int? SubmissionId)> SubmitAsync(
        string fullName,
        string mobile,
        string email,
        int subjectId,
        string message,
        bool consentAccepted,
        CancellationToken ct)
    {
        var nameErr = ValidateName(fullName);
        if (nameErr != null) return (false, nameErr, null);

        var mobileErr = ValidateMobile(mobile);
        if (mobileErr != null) return (false, mobileErr, null);

        var emailErr = ValidateEmail(email);
        if (emailErr != null) return (false, emailErr, null);

        if (!ContactSubjectCatalog.IsValid(subjectId))
            return (false, "Please select a valid subject.", null);

        var messageErr = ValidateMessage(message);
        if (messageErr != null) return (false, messageErr, null);

        if (consentAccepted != true)
            return (false, "Consent is required to send your message.", null);

        try
        {
            await ContactSchemaBootstrap.EnsureAsync(_db, _log, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "ContactSubmissions table is not available.");
            return (false, "Unable to send your message right now. Please try again.", null);
        }

        var now = DateTime.Now;
        var entity = new ContactSubmission
        {
            FullName = fullName.Trim(),
            Phone = mobile.Trim(),
            Email = email.Trim(),
            SubjectId = subjectId,
            Message = message.Trim(),
            ConsentAccepted = true,
            ConsentAcceptedAt = now,
            CreatedAt = now,
        };

        try
        {
            await _db.ContactSubmissions.AddAsync(entity, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to save contact submission.");
            return (false, "Unable to send your message right now. Please try again.", null);
        }

        var emailResult = await _email.TrySendEmailsAsync(entity, ct);
        if (emailResult.CustomerNotified)
        {
            entity.ConfirmationEmailSentAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);
        }

        _log.LogInformation(
            "Contact submission {Id} saved (customer email: {Customer}, support email: {Support}).",
            entity.Id,
            emailResult.CustomerNotified,
            emailResult.SupportNotified);

        return (
            true,
            "Thank you! We have received your query. Our team will connect with you shortly.",
            entity.Id);
    }

    private static string? ValidateName(string nameRaw)
    {
        var name = (nameRaw ?? "").Trim();
        if (name.Length == 0) return "Your name is required.";
        if (name.Length < 2) return "Please enter your full name.";
        if (name.Any(c => !char.IsLetter(c) && c != ' ')) return "Name can contain only letters and spaces.";
        if (name.Any(char.IsDigit)) return "Name cannot contain numbers.";
        if (System.Text.RegularExpressions.Regex.IsMatch(name.Replace(" ", ""), @"(.)\1{3,}", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
            return "Name looks invalid (repeated characters).";

        var words = name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => BannedNameWords.Contains(w)))
            return "Please enter a valid name.";

        return null;
    }

    private static string? ValidateMobile(string v)
    {
        var m = (v ?? "").Trim();
        if (m.Length == 0) return "Mobile number is required.";
        if (!m.All(char.IsDigit)) return "Mobile number must be numeric.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(m, @"^[6-9]\d{9}$"))
            return "Enter a valid 10-digit Indian mobile number (starts with 6-9).";
        return null;
    }

    private static string? ValidateEmail(string v)
    {
        var e = (v ?? "").Trim();
        if (e.Length == 0) return "Email address is required.";
        if (!System.Text.RegularExpressions.Regex.IsMatch(e, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            return "Enter a valid email address.";
        return null;
    }

    private static string? ValidateMessage(string v)
    {
        var m = (v ?? "").Trim();
        if (m.Length == 0) return "Message is required.";
        if (m.Length < 10) return "Please enter at least 10 characters in your message.";
        if (m.Length > 4000) return "Message is too long (maximum 4000 characters).";
        return null;
    }
}
