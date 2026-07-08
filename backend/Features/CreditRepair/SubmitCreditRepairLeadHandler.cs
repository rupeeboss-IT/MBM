using System.Diagnostics;
using RB_Website_API.Services;

namespace RB_Website_API.Features.CreditRepair;

public interface ISubmitCreditRepairLeadHandler
{
    Task<(bool Success, string? Message, int? LeadId)> HandleAsync(SubmitCreditRepairLeadCommand cmd, string correlationId, CancellationToken ct);
}

public sealed class SubmitCreditRepairLeadHandler : ISubmitCreditRepairLeadHandler
{
    private readonly ICreditRepairLeadService _service;
    private readonly ILogger<SubmitCreditRepairLeadHandler> _log;

    public SubmitCreditRepairLeadHandler(ICreditRepairLeadService service, ILogger<SubmitCreditRepairLeadHandler> log)
    {
        _service = service;
        _log = log;
    }

    public async Task<(bool Success, string? Message, int? LeadId)> HandleAsync(
        SubmitCreditRepairLeadCommand cmd,
        string correlationId,
        CancellationToken ct)
    {
        using var scope = _log.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = correlationId,
        });

        var sw = Stopwatch.StartNew();
        _log.LogInformation("Credit repair submission received.");

        var result = await _service.SubmitAsync(
            cmd.FullName,
            cmd.Mobile,
            cmd.Email,
            cmd.ConsentAccepted,
            cmd.AdvisorCode,
            correlationId,
            ct);

        sw.Stop();
        _log.LogInformation(
            "Credit repair submission completed. Success {Success} LeadId {LeadId} ElapsedMs {ElapsedMs}",
            result.Success,
            result.LeadId,
            sw.ElapsedMilliseconds);

        return result;
    }
}

