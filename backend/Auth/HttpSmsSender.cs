using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RB_Website_API.Auth;

/// <summary>Sends SMS via RupeeBoss HTTP gateway (SendMsg.aspx).</summary>
public sealed class HttpSmsSender : ISmsSender
{
    public const string HttpClientName = "SmsGateway";

    private readonly IHttpClientFactory _httpFactory;
    private readonly SmsSettings _settings;
    private readonly ILogger<HttpSmsSender> _logger;

    public HttpSmsSender(
        IHttpClientFactory httpFactory,
        IOptions<SmsSettings> options,
        ILogger<HttpSmsSender> logger)
    {
        _httpFactory = httpFactory;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(string toPhone, string message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_settings.SendUrl))
        {
            _logger.LogWarning("SmsSettings.SendUrl is not set; SMS not sent to {Phone}", toPhone);
            throw new InvalidOperationException("SMS is not configured (missing SmsSettings:SendUrl).");
        }

        var dest = IndianPhone.Digits(toPhone);
        if (string.IsNullOrEmpty(dest))
            throw new InvalidOperationException("Invalid mobile number for SMS.");

        var url = QueryHelpers.AddQueryString(
            _settings.SendUrl.Trim(),
            new Dictionary<string, string?>
            {
                ["uname"] = _settings.Username,
                ["pass"] = _settings.Password,
                ["send"] = _settings.SenderId,
                ["dest"] = dest,
                ["msg"] = message,
            });

        var client = _httpFactory.CreateClient(HttpClientName);
        // Do not tie gateway delivery to the HTTP request token; slow gateways can outlive the browser call.
        ct.ThrowIfCancellationRequested();
        using var smsCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var response = await client.GetAsync(url, smsCts.Token);
        var body = (await response.Content.ReadAsStringAsync(smsCts.Token)).Trim();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SMS gateway HTTP {Status} for {Dest}. Body: {Body}",
                (int)response.StatusCode,
                dest,
                body);
            throw new InvalidOperationException("Unable to send the verification code via SMS. Please try again.");
        }

        if (LooksLikeGatewayFailure(body))
        {
            _logger.LogError(
                "SMS gateway rejected message for {Dest}. Body: {Body}",
                dest,
                body);
            throw new InvalidOperationException("Unable to send the verification code via SMS. Please try again.");
        }

        _logger.LogInformation("SMS gateway response for {Dest}: {Body}", dest, body);
    }

    private static bool LooksLikeGatewayFailure(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return false;

        var text = body.Trim();
        if (text.Equals("success", StringComparison.OrdinalIgnoreCase)
            || text.Equals("ok", StringComparison.OrdinalIgnoreCase)
            || text.StartsWith("success", StringComparison.OrdinalIgnoreCase)
            || text.Contains("submitted", StringComparison.OrdinalIgnoreCase)
            || text.Contains("sent", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return text.Contains("fail", StringComparison.OrdinalIgnoreCase)
               || text.Contains("error", StringComparison.OrdinalIgnoreCase)
               || text.Contains("invalid", StringComparison.OrdinalIgnoreCase)
               || text.Contains("reject", StringComparison.OrdinalIgnoreCase)
               || text.Contains("denied", StringComparison.OrdinalIgnoreCase);
    }
}
