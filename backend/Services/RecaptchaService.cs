using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;

namespace RB_Website_API.Services;

public sealed class RecaptchaService : IRecaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";

    private readonly IHttpClientFactory _httpFactory;
    private readonly RecaptchaSettings _settings;
    private readonly ILogger<RecaptchaService> _logger;

    public RecaptchaService(
        IHttpClientFactory httpFactory,
        IOptions<RecaptchaSettings> options,
        ILogger<RecaptchaService> logger)
    {
        _httpFactory = httpFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, string? FailReason)> VerifyAsync(
        string? token, string expectedAction, CancellationToken ct = default)
    {
        if (!_settings.Enabled)
            return (true, null);

        if (string.IsNullOrWhiteSpace(_settings.SecretKey))
        {
            _logger.LogWarning("reCAPTCHA secret key is not configured — skipping verification for action={Action}", expectedAction);
            return (true, null);
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("reCAPTCHA token missing for action={Action}", expectedAction);
            return (false, "reCAPTCHA token is required.");
        }

        try
        {
            var client = _httpFactory.CreateClient("Recaptcha");
            var content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = _settings.SecretKey,
                ["response"] = token,
            });

            var response = await client.PostAsync(VerifyUrl, content, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<RecaptchaVerifyResponse>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (result is null || !result.Success)
            {
                var codes = result?.ErrorCodes is { Length: > 0 } ec ? string.Join(", ", ec) : "unknown";
                _logger.LogWarning("reCAPTCHA verification failed action={Action} codes={Codes}", expectedAction, codes);
                return (false, "reCAPTCHA verification failed. Please try again.");
            }

            if (result.Score < _settings.MinimumScore)
            {
                _logger.LogWarning(
                    "reCAPTCHA score too low action={Action} score={Score} threshold={Threshold}",
                    expectedAction, result.Score, _settings.MinimumScore);
                return (false, "Request blocked by reCAPTCHA. Please try again.");
            }

            _logger.LogDebug(
                "reCAPTCHA OK action={Action} score={Score}", expectedAction, result.Score);
            return (true, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "reCAPTCHA HTTP call failed for action={Action}", expectedAction);
            // Fail open on network errors to avoid blocking legitimate users.
            return (true, null);
        }
    }

    private sealed class RecaptchaVerifyResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string? Action { get; set; }

        [JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
