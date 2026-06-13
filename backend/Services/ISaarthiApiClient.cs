using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface ISaarthiApiClient
{
    Task<SaarthiSdrApiResult> GenerateSdrAsync(string udyamNumber, string requestId, CancellationToken ct);
}
