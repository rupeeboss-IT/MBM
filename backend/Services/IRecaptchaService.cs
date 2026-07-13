namespace RB_Website_API.Services;

public interface IRecaptchaService
{
    /// <summary>
    /// Verifies a reCAPTCHA v3 token obtained from the frontend.
    /// Returns (true, null) on success or (false, reason) on failure.
    /// </summary>
    Task<(bool Success, string? FailReason)> VerifyAsync(string? token, string expectedAction, CancellationToken ct = default);
}
