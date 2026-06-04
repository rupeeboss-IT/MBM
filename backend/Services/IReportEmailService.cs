using RB_Website_API.Models;

namespace RB_Website_API.Services;

public interface IReportEmailService
{
    Task SendReportReadyEmailAsync(User customer, CustomerReport report, CancellationToken ct);
}
