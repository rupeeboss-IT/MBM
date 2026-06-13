using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public sealed class SaarthiApiClient : ISaarthiApiClient
{
    private static int _outboundIpLogged;

    private static readonly string[] PrepSteps =
    [
        "get-nic-codes",
        "get-scheme-list",
        "get-scheme-count",
    ];

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly SaarthiApiSettings _settings;
    private readonly ILogger<SaarthiApiClient> _logger;

    public SaarthiApiClient(
        IHttpClientFactory httpClientFactory,
        IOptions<SdrReportSettings> reportSettings,
        ILogger<SaarthiApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settings = reportSettings.Value.Saarthi;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
            _logger.LogWarning("Saarthi API credentials are not configured in SdrReportSettings:Saarthi.");
    }

    public async Task<SaarthiSdrApiResult> GenerateSdrAsync(
        string udyamNumber,
        string requestId,
        CancellationToken ct)
    {
        _logger.LogInformation(
            "[SDR Pipeline] START CorrelationId={CorrelationId} Udyam={UdyamMasked} Steps={Steps}",
            requestId,
            SaarthiApiLogHelper.MaskUdyam(udyamNumber),
            string.Join(" → ", PrepSteps.Concat(["generate-sdr"])));

        await LogOutboundPublicIpOnceAsync(ct);

        if (!TryValidateConfig(out var configError))
        {
            _logger.LogError("[SDR Pipeline] ABORT CorrelationId={CorrelationId} Reason={Reason}", requestId, configError);
            return new SaarthiSdrApiResult(false, null, configError, null);
        }

        _logger.LogInformation(
            "[SDR Pipeline] Config CorrelationId={CorrelationId} BaseUrl={BaseUrl} {AuthSummary} TimeoutSeconds={TimeoutSeconds}",
            requestId,
            _settings.BaseUrl.Trim().TrimEnd('/'),
            SaarthiApiLogHelper.BuildAuthLogSummary(_settings.ApiKey),
            _settings.TimeoutSeconds);

        var stepIndex = 0;
        foreach (var step in PrepSteps)
        {
            stepIndex++;
            var path = ResolvePath(step);
            var stepRequestId = $"{requestId}-{step}";

            _logger.LogInformation(
                "[SDR Pipeline] Step {StepIndex}/{TotalSteps} PREPARE CorrelationId={CorrelationId} Step={Step} Path={Path} XRequestId={XRequestId}",
                stepIndex,
                PrepSteps.Length + 1,
                requestId,
                step,
                path,
                stepRequestId);

            var prep = await PostUdyamStepAsync(step, path, udyamNumber, stepRequestId, requestId, ct);
            if (!prep.Success)
            {
                _logger.LogError(
                    "[SDR Pipeline] Step {Step} FAILED CorrelationId={CorrelationId} Error={Error} ResponseSnippet={Response}",
                    step,
                    requestId,
                    prep.ErrorMessage,
                    SaarthiApiLogHelper.Truncate(prep.ExternalReference, 500));
                _logger.LogError("[SDR Pipeline] ABORT CorrelationId={CorrelationId} FailedAt={Step}", requestId, step);
                return new SaarthiSdrApiResult(false, null, prep.ErrorMessage, prep.ExternalReference);
            }

            _logger.LogInformation(
                "[SDR Pipeline] Step {Step} COMPLETE CorrelationId={CorrelationId}",
                step,
                requestId);
        }

        _logger.LogInformation(
            "[SDR Pipeline] Step {StepIndex}/{TotalSteps} PREPARE CorrelationId={CorrelationId} Step=generate-sdr Path={Path}",
            PrepSteps.Length + 1,
            PrepSteps.Length + 1,
            requestId,
            ResolvePath("generate-sdr"));

        var generateResult = await PostGenerateSdrAsync(
            udyamNumber,
            $"{requestId}-generate-sdr",
            requestId,
            ct);

        if (generateResult.Success)
        {
            _logger.LogInformation(
                "[SDR Pipeline] COMPLETE CorrelationId={CorrelationId} PdfBytes={PdfBytes}",
                requestId,
                generateResult.PdfBytes?.Length ?? 0);
        }
        else
        {
            _logger.LogError(
                "[SDR Pipeline] FAILED CorrelationId={CorrelationId} Step=generate-sdr Error={Error} ResponseSnippet={Response}",
                requestId,
                generateResult.ErrorMessage,
                SaarthiApiLogHelper.Truncate(generateResult.ExternalReference, 500));
        }

        return generateResult;
    }

    private bool TryValidateConfig(out string error)
    {
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            error = "SDR API is not configured.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.ApiSecret))
        {
            error = "SDR API credentials are not configured.";
            return false;
        }

        error = "";
        return true;
    }

    private string ResolvePath(string step) => step switch
    {
        "get-nic-codes" => _settings.GetNicCodesPath,
        "get-scheme-list" => _settings.GetSchemeListPath,
        "get-scheme-count" => _settings.GetSchemeCountPath,
        "generate-sdr" => _settings.GeneratePath,
        _ => step,
    };

    private async Task<SaarthiStepResult> PostUdyamStepAsync(
        string stepName,
        string path,
        string udyamNumber,
        string stepRequestId,
        string correlationId,
        CancellationToken ct)
    {
        var response = await SendUdyamPostAsync(stepName, path, udyamNumber, stepRequestId, correlationId, ct);
        if (response.TransportError is not null)
            return SaarthiStepResult.Fail($"{stepName}: {response.TransportError}");

        if (!response.IsHttpSuccess)
        {
            var errorMessage = BuildHttpErrorMessage(stepName, response.StatusCode, response.BodyText);
            return SaarthiStepResult.Fail(errorMessage, SaarthiApiLogHelper.Truncate(response.BodyText, 1000));
        }

        if (!TryReadJsonSuccess(response.BodyText, out var apiMessage))
        {
            _logger.LogWarning(
                "[SDR API] {Step} unexpected JSON CorrelationId={CorrelationId} XRequestId={XRequestId} Body={Body}",
                stepName,
                correlationId,
                stepRequestId,
                SaarthiApiLogHelper.FormatResponseBodyForLog(response.ContentType, response.BodyBytes, response.BodyText));
            return SaarthiStepResult.Fail(
                $"{stepName} returned an invalid response.",
                SaarthiApiLogHelper.Truncate(response.BodyText, 1000));
        }

        _logger.LogInformation(
            "[SDR API] {Step} business OK CorrelationId={CorrelationId} ApiMessage={ApiMessage}",
            stepName,
            correlationId,
            apiMessage ?? "OK");

        return SaarthiStepResult.Ok(SaarthiApiLogHelper.Truncate(response.BodyText, 1000));
    }

    private async Task<SaarthiSdrApiResult> PostGenerateSdrAsync(
        string udyamNumber,
        string stepRequestId,
        string correlationId,
        CancellationToken ct)
    {
        const string stepName = "generate-sdr";
        var path = ResolvePath(stepName);
        var response = await SendUdyamPostAsync(stepName, path, udyamNumber, stepRequestId, correlationId, ct);

        if (response.TransportError is not null)
            return new SaarthiSdrApiResult(false, null, $"{stepName}: {response.TransportError}", null);

        if (!response.IsHttpSuccess)
        {
            var errorMessage = BuildHttpErrorMessage(stepName, response.StatusCode, response.BodyText);
            return new SaarthiSdrApiResult(false, null, errorMessage, SaarthiApiLogHelper.Truncate(response.BodyText, 1000));
        }

        var contentType = response.ContentType;
        var bytes = response.BodyBytes;

        if (LooksLikePdf(bytes) || contentType.Contains("pdf", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogInformation(
                "[SDR API] generate-sdr PDF received CorrelationId={CorrelationId} Bytes={Bytes}",
                correlationId,
                bytes.Length);
            return ValidatePdfBytes(bytes, null);
        }

        var body = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : "";
        if (string.IsNullOrWhiteSpace(body))
            return new SaarthiSdrApiResult(false, null, "generate-sdr returned an empty response.", null);

        try
        {
            using var doc = JsonDocument.Parse(body);
            var pdfBase64 = FindPdfBase64(doc.RootElement);
            if (string.IsNullOrWhiteSpace(pdfBase64))
            {
                _logger.LogWarning(
                    "[SDR API] generate-sdr JSON missing PDF CorrelationId={CorrelationId} Body={Body}",
                    correlationId,
                    SaarthiApiLogHelper.FormatResponseBodyForLog(contentType, bytes, body));
                return new SaarthiSdrApiResult(false, null, "generate-sdr returned an invalid response.", doc.RootElement.GetRawText());
            }

            byte[] pdfBytes;
            try
            {
                pdfBytes = Convert.FromBase64String(pdfBase64.Trim());
            }
            catch (FormatException)
            {
                return new SaarthiSdrApiResult(false, null, "generate-sdr returned invalid PDF data.", doc.RootElement.GetRawText());
            }

            _logger.LogInformation(
                "[SDR API] generate-sdr PDF decoded from JSON CorrelationId={CorrelationId} Bytes={Bytes}",
                correlationId,
                pdfBytes.Length);
            return ValidatePdfBytes(pdfBytes, doc.RootElement.GetRawText());
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "[SDR API] generate-sdr non-JSON response CorrelationId={CorrelationId} Body={Body}",
                correlationId,
                SaarthiApiLogHelper.FormatResponseBodyForLog(contentType, bytes, body));
            return new SaarthiSdrApiResult(false, null, "generate-sdr returned an invalid response.", null);
        }
    }

    private async Task<SaarthiHttpResponse> SendUdyamPostAsync(
        string stepName,
        string path,
        string udyamNumber,
        string stepRequestId,
        string correlationId,
        CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        var baseUrl = _settings.BaseUrl.Trim().TrimEnd('/');
        var relativePath = (path ?? "").Trim().TrimStart('/');
        var url = $"{baseUrl}/{relativePath}";
        var requestPayload = SaarthiApiLogHelper.BuildRequestPayloadLog(udyamNumber);

        _logger.LogInformation(
            "[SDR API] REQUEST START Step={Step} CorrelationId={CorrelationId} XRequestId={XRequestId} Method=POST Url={Url} Payload={Payload}",
            stepName,
            correlationId,
            stepRequestId,
            url,
            requestPayload);

        var client = _httpClientFactory.CreateClient("Saarthi");
        client.Timeout = TimeSpan.FromSeconds(Math.Clamp(_settings.TimeoutSeconds, 30, 300));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
        var authJson = JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["MARKETPLACE_API_KEY"] = _settings.ApiKey.Trim(),
            ["MARKETPLACE_API_SECRET"] = _settings.ApiSecret.Trim(),
        });
        httpRequest.Headers.TryAddWithoutValidation("marketplace-auth", authJson);
        httpRequest.Headers.TryAddWithoutValidation("X-Request-ID", stepRequestId);
        if (string.Equals(stepName, "generate-sdr", StringComparison.OrdinalIgnoreCase))
            httpRequest.Headers.Accept.ParseAdd("application/pdf");
        // eMSME rejects "application/json; charset=utf-8" (default StringContent) — Postman sends plain application/json.
        httpRequest.Content = CreateSaarthiJsonContent(JsonSerializer.Serialize(new { udyamNumber }));

        _logger.LogDebug(
            "[SDR API] REQUEST HEADERS Step={Step} CorrelationId={CorrelationId} XRequestId={XRequestId} ContentType=application/json {AuthSummary}",
            stepName,
            correlationId,
            stepRequestId,
            SaarthiApiLogHelper.BuildAuthLogSummary(_settings.ApiKey));

        try
        {
            using var response = await client.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var bodyText = bytes.Length > 0 ? Encoding.UTF8.GetString(bytes) : "";
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "";
            var elapsed = SaarthiApiLogHelper.ElapsedMs(sw);

            var responseLog = SaarthiApiLogHelper.FormatResponseBodyForLog(contentType, bytes, bodyText);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "[SDR API] RESPONSE OK Step={Step} CorrelationId={CorrelationId} XRequestId={XRequestId} StatusCode={StatusCode} ElapsedMs={ElapsedMs} ContentType={ContentType} Body={Body}",
                    stepName,
                    correlationId,
                    stepRequestId,
                    (int)response.StatusCode,
                    elapsed,
                    contentType,
                    responseLog);
            }
            else
            {
                _logger.LogWarning(
                    "[SDR API] RESPONSE ERROR Step={Step} CorrelationId={CorrelationId} XRequestId={XRequestId} StatusCode={StatusCode} ElapsedMs={ElapsedMs} ContentType={ContentType} Body={Body}",
                    stepName,
                    correlationId,
                    stepRequestId,
                    (int)response.StatusCode,
                    elapsed,
                    contentType,
                    responseLog);
            }

            return new SaarthiHttpResponse(
                (int)response.StatusCode,
                response.IsSuccessStatusCode,
                contentType,
                bytes,
                bodyText,
                null);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(
                ex,
                "[SDR API] TIMEOUT Step={Step} CorrelationId={CorrelationId} XRequestId={XRequestId} ElapsedMs={ElapsedMs} Url={Url}",
                stepName,
                correlationId,
                stepRequestId,
                SaarthiApiLogHelper.ElapsedMs(sw),
                url);
            return SaarthiHttpResponse.TransportFailure("SDR API request timed out.");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SDR API] TRANSPORT ERROR Step={Step} CorrelationId={CorrelationId} XRequestId={XRequestId} ElapsedMs={ElapsedMs} Url={Url}",
                stepName,
                correlationId,
                stepRequestId,
                SaarthiApiLogHelper.ElapsedMs(sw),
                url);
            return SaarthiHttpResponse.TransportFailure("SDR API request failed.");
        }
    }

    private static bool TryReadJsonSuccess(string body, out string? message)
    {
        message = null;
        if (string.IsNullOrWhiteSpace(body)) return false;

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return false;

            if (doc.RootElement.TryGetProperty("success", out var successProp)
                && successProp.ValueKind is JsonValueKind.True or JsonValueKind.False)
            {
                if (successProp.GetBoolean())
                {
                    if (doc.RootElement.TryGetProperty("message", out var msgProp)
                        && msgProp.ValueKind == JsonValueKind.String)
                    {
                        message = msgProp.GetString();
                    }
                    return true;
                }

                if (doc.RootElement.TryGetProperty("message", out var errMsg)
                    && errMsg.ValueKind == JsonValueKind.String)
                {
                    message = errMsg.GetString();
                }
                return false;
            }

            return doc.RootElement.TryGetProperty("data", out _);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static StringContent CreateSaarthiJsonContent(string json)
    {
        var content = new StringContent(json, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        return content;
    }

    private async Task LogOutboundPublicIpOnceAsync(CancellationToken ct)
    {
        if (Interlocked.CompareExchange(ref _outboundIpLogged, 1, 0) != 0)
            return;

        try
        {
            var probe = _httpClientFactory.CreateClient();
            probe.Timeout = TimeSpan.FromSeconds(8);
            var ip = (await probe.GetStringAsync("https://api.ipify.org", ct)).Trim();
            _logger.LogInformation(
                "[SDR API] Outbound public IP (share with eMSME for whitelist): {OutboundIp}",
                ip);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[SDR API] Could not determine outbound public IP — ask eMSME to whitelist your API server's public IP");
        }
    }

    private static string BuildHttpErrorMessage(string stepName, int statusCode, string? body)
    {
        if (statusCode == 403 && LooksLikeHtmlForbidden(body))
        {
            return $"Saarthi {stepName} returned 403 Forbidden from the eMSME gateway. "
                   + "This is usually an IP whitelist issue: share your API server's outbound public IP with eMSME "
                   + "(see log line 'Outbound public IP') and confirm UAT API keys are active.";
        }

        var suffix = statusCode switch
        {
            403 => "access denied — verify API credentials and server IP whitelist",
            401 => "authentication failed",
            500 => "server error — Udyam may be invalid or unavailable",
            _ => "request failed",
        };

        var detail = ExtractJsonMessage(body);
        var baseMsg = $"Saarthi {stepName} returned {statusCode} ({suffix}).";
        return string.IsNullOrWhiteSpace(detail) ? baseMsg : $"{baseMsg} {detail}";
    }

    private static bool LooksLikeHtmlForbidden(string? body) =>
        !string.IsNullOrWhiteSpace(body)
        && body.Contains("<title>403 Forbidden</title>", StringComparison.OrdinalIgnoreCase);

    private static string? ExtractJsonMessage(string? body)
    {
        if (string.IsNullOrWhiteSpace(body)) return null;
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("message", out var msg)
                && msg.ValueKind == JsonValueKind.String)
            {
                return msg.GetString();
            }
        }
        catch (JsonException)
        {
            // ignore
        }

        return null;
    }

    private static bool LooksLikePdf(byte[] bytes) =>
        bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46;

    private static SaarthiSdrApiResult ValidatePdfBytes(byte[]? pdfBytes, string? externalReference)
    {
        if (pdfBytes is null || pdfBytes.Length == 0)
            return new SaarthiSdrApiResult(false, null, "generate-sdr returned an empty PDF.", externalReference);

        if (pdfBytes.Length < 5 || pdfBytes[0] != 0x25 || pdfBytes[1] != 0x50 || pdfBytes[2] != 0x44 || pdfBytes[3] != 0x46)
            return new SaarthiSdrApiResult(false, null, "generate-sdr returned an invalid PDF file.", externalReference);

        return new SaarthiSdrApiResult(true, pdfBytes, null, externalReference);
    }

    private static string? FindPdfBase64(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
            {
                var s = element.GetString();
                if (string.IsNullOrWhiteSpace(s)) return null;
                if (s.Length > 100 && LooksLikeBase64Pdf(s)) return s;
                return null;
            }
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var name = prop.Name.ToLowerInvariant();
                    if (name is "pdf" or "file" or "report" or "reportpdf" or "sdrpdf" or "document" or "data" or "content")
                    {
                        var found = FindPdfBase64(prop.Value);
                        if (!string.IsNullOrWhiteSpace(found)) return found;
                    }
                }

                foreach (var prop in element.EnumerateObject())
                {
                    var found = FindPdfBase64(prop.Value);
                    if (!string.IsNullOrWhiteSpace(found)) return found;
                }
                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var found = FindPdfBase64(item);
                    if (!string.IsNullOrWhiteSpace(found)) return found;
                }
                break;
        }

        return null;
    }

    private static bool LooksLikeBase64Pdf(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("JVBER", StringComparison.Ordinal)) return true;
        try
        {
            var bytes = Convert.FromBase64String(trimmed);
            return bytes.Length >= 4 && bytes[0] == 0x25 && bytes[1] == 0x50;
        }
        catch
        {
            return false;
        }
    }

    private sealed record SaarthiHttpResponse(
        int StatusCode,
        bool IsHttpSuccess,
        string ContentType,
        byte[] BodyBytes,
        string BodyText,
        string? TransportError)
    {
        public static SaarthiHttpResponse TransportFailure(string message) =>
            new(0, false, "", [], "", message);
    }

    private sealed record SaarthiStepResult(bool Success, string? ErrorMessage, string? ExternalReference)
    {
        public static SaarthiStepResult Ok(string? reference) => new(true, null, reference);
        public static SaarthiStepResult Fail(string error, string? reference = null) => new(false, error, reference);
    }
}
