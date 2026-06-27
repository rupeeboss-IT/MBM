using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public interface ISdrReportService
{
    Task<CustomerReport?> HasValidGeneratedReportAsync(Guid userId, CancellationToken ct);

    Task<SdrGenerationResult> GenerateAfterOneTimePaymentAsync(
        Guid userId,
        SchemeDiscoveryRequest request,
        CancellationToken ct);

    Task<SdrGenerationResult> GenerateForAdminAsync(
        Guid adminUserId,
        Guid customerId,
        string udyamNumber,
        CancellationToken ct);
}
