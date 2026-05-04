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
        using var response = await client.GetAsync(url, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "SMS gateway HTTP {Status} for {Dest}. Body: {Body}",
                (int)response.StatusCode,
                dest,
                body);
            throw new InvalidOperationException($"SMS gateway returned {(int)response.StatusCode}.");
        }

        _logger.LogInformation("SMS gateway response for {Dest}: {Body}", dest, body);
    }
}
