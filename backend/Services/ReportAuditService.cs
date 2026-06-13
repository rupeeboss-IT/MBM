using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class ReportAuditService : IReportAuditService
{
    public const string ActionUpload = "ReportUpload";
    public const string ActionDownload = "ReportDownload";
    public const string ActionEmailSent = "EmailSent";
    public const string ActionRequestCreated = "RequestCreated";
    public const string ActionRequestApproved = "RequestApproved";
    public const string ActionRequestRejected = "RequestRejected";
    public const string ActionDirectDelete = "DirectDelete";
    public const string ActionDirectReplace = "DirectReplace";
    public const string ActionDirectEdit = "DirectEdit";
    public const string ActionSoftDelete = "ReportSoftDelete";
    public const string ActionReplace = "ReportReplace";
    public const string ActionEdit = "ReportEdit";

    private readonly IReportAuditRepository _audit;

    public ReportAuditService(IReportAuditRepository audit) => _audit = audit;

    public Task LogAsync(
        string action,
        Guid? actorUserId,
        Guid? reportId,
        Guid? customerId,
        string? ipAddress,
        CancellationToken ct)
    {
        var entry = CreateEntry(action, actorUserId, reportId, customerId, null, null, null, null, null, null, ipAddress);
        return _audit.AddAsync(entry, ct);
    }

    public Task StageChangeLogAsync(
        string action,
        Guid? actorUserId,
        Guid? reportId,
        Guid? customerId,
        Guid? requestId,
        string? remarks,
        string? previousReportPath,
        string? newReportPath,
        string? previousValues,
        string? newValues,
        string? ipAddress,
        CancellationToken ct)
    {
        var entry = CreateEntry(
            action,
            actorUserId,
            reportId,
            customerId,
            requestId,
            remarks,
            previousReportPath,
            newReportPath,
            previousValues,
            newValues,
            ipAddress);
        return _audit.StageAsync(entry, ct);
    }

    private static ReportAuditLog CreateEntry(
        string action,
        Guid? actorUserId,
        Guid? reportId,
        Guid? customerId,
        Guid? requestId,
        string? remarks,
        string? previousReportPath,
        string? newReportPath,
        string? previousValues,
        string? newValues,
        string? ipAddress)
    {
        return new ReportAuditLog
        {
            AuditId = Guid.NewGuid(),
            UserId = actorUserId,
            Action = action,
            ReportId = reportId,
            CustomerId = customerId,
            CreatedAt = DateTime.Now,
            IpAddress = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim(),
            RequestId = requestId,
            Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim(),
            PreviousReportPath = previousReportPath,
            NewReportPath = newReportPath,
            PreviousValues = previousValues,
            NewValues = newValues,
        };
    }
}
