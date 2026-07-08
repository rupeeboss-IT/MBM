using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;

namespace RB_Website_API.Services.Webhooks;

public sealed class ZohoFlowWebhookClient : IZohoFlowWebhookClient
{
    public const string HttpClientName = "ZohoFlow";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ZohoFlowSettings _settings;
    private readonly ILogger<ZohoFlowWebhookClient> _log;

    public ZohoFlowWebhookClient(
        IHttpClientFactory httpClientFactory,
        IOptions<ZohoFlowSettings> settings,
        ILogger<ZohoFlowWebhookClient> log)
    {
        _httpClientFactory = httpClientFactory;
        _settings = settings.Value;
        _log = log;
    }

    public async Task<(bool Success, int StatusCode, string? ResponseBody, string? ErrorMessage, long ElapsedMilliseconds)>
        SubmitCreditRepairLeadAsync(string name, string email, string phone, CancellationToken ct)
    {
        var webhookUrl = (_settings.WebhookUrl ?? "").Trim();
        if (string.IsNullOrWhiteSpace(webhookUrl))
            return (false, 0, null, "Webhook is not configured.", 0);

        var payload = new
        {
            name,
            email,
            phone,
            customer_type = "new",
            source = "RupeeBOSS",
        };

        var payloadJson = JsonSerializer.Serialize(payload, JsonOptions);

        var safeUrlForLog = MaskWebhookUrl(webhookUrl);

        _log.LogInformation(
            "Preparing Zoho Flow webhook request. Url {Url} Payload {Payload}",
            safeUrlForLog,
            payloadJson);

        var sw = Stopwatch.StartNew();
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);

            using var req = new HttpRequestMessage(HttpMethod.Post, webhookUrl)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json"),
            };
            req.Headers.Accept.Clear();
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseContentRead, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);

            sw.Stop();

            _log.LogInformation(
                "Zoho Flow webhook completed. Url {Url} Status {StatusCode} ElapsedMs {ElapsedMs} Body {Body}",
                safeUrlForLog,
                (int)resp.StatusCode,
                sw.ElapsedMilliseconds,
                body);

            return (resp.IsSuccessStatusCode && (int)resp.StatusCode == 200, (int)resp.StatusCode, body, null, sw.ElapsedMilliseconds);
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            sw.Stop();
            _log.LogError(ex, "Zoho Flow webhook timeout. Url {Url} ElapsedMs {ElapsedMs}", safeUrlForLog, sw.ElapsedMilliseconds);
            return (false, 408, null, "Webhook request timed out.", sw.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            sw.Stop();
            _log.LogError(ex, "Zoho Flow webhook network error. Url {Url} ElapsedMs {ElapsedMs}", safeUrlForLog, sw.ElapsedMilliseconds);
            return (false, 503, null, "Network error while calling webhook.", sw.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            _log.LogError(ex, "Zoho Flow webhook unexpected error. Url {Url} ElapsedMs {ElapsedMs}", safeUrlForLog, sw.ElapsedMilliseconds);
            return (false, 500, null, "Unexpected error while calling webhook.", sw.ElapsedMilliseconds);
        }
    }

    private static string MaskWebhookUrl(string rawUrl)
    {
        // Never log the zapikey (or any query values). Keep only scheme/host/path + parameter names.
        if (!Uri.TryCreate(rawUrl, UriKind.Absolute, out var uri))
            return "<invalid-url>";

        if (string.IsNullOrWhiteSpace(uri.Query))
            return uri.GetLeftPart(UriPartial.Path);

        var names = uri.Query
            .TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(p => p.Split('=', 2)[0])
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .ToArray();

        var safeQuery = string.Join("&", names.Select(n => $"{n}=***"));
        return $"{uri.GetLeftPart(UriPartial.Path)}?{safeQuery}";
    }
}

