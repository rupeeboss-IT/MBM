using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface ISchemeDiscoveryService
{
    Task<SchemeDiscoveryStatusDto> GetStatusAsync(Guid userId, CancellationToken ct);
    Task<SchemeDiscoverySubmitResponse> SaveDraftAsync(Guid userId, string udyamNumber, CancellationToken ct);
    Task<SchemeDiscoverySubmitResponse> FinalizeRequestAsync(Guid userId, Guid requestId, CancellationToken ct);
    Task<SchemeDiscoverySubmitResponse> SubmitRequestAsync(Guid userId, string udyamNumber, CancellationToken ct);
    Task<(bool Success, string? Message)> EmailReportAsync(Guid userId, Guid reportId, CancellationToken ct);
}
