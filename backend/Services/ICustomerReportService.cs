using RB_Website_API.DTO;

namespace RB_Website_API.Services;

public interface ICustomerReportService
{
    Task<IReadOnlyList<CustomerSearchResultDto>> SearchCustomersAsync(
        string? memberId,
        string? mobile,
        string? email,
        string? customerName,
        CancellationToken ct);

    Task<(bool Ok, string? Error, Guid? SubscriptionId)> ValidateActiveSubscriptionAsync(Guid customerId, CancellationToken ct);

    Task<(bool Success, string? Message, Guid? ReportId)> UploadReportAsync(
        Guid adminUserId,
        Guid customerId,
        IFormFile file,
        string? ipAddress,
        CancellationToken ct);

    Task<IReadOnlyList<CustomerReportListItemDto>> ListCustomerReportsAsync(Guid customerId, CancellationToken ct);

    Task<PagedResultDto<AdminReportHistoryItemDto>> ListAdminHistoryAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken ct);

    Task<(bool Allowed, string? Error, string? PhysicalPath, string? DownloadName)> GetDownloadForCustomerAsync(
        Guid customerId,
        Guid reportId,
        string? ipAddress,
        CancellationToken ct);
}
