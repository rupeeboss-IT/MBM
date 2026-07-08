namespace RB_Website_API.Services.Webhooks;

public interface IZohoFlowWebhookClient
{
    Task<(bool Success, int StatusCode, string? ResponseBody, string? ErrorMessage, long ElapsedMilliseconds)> SubmitCreditRepairLeadAsync(
        string name,
        string email,
        string phone,
        CancellationToken ct);
}

