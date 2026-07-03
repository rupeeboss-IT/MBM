using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RB_Website_API.Auth;
using RB_Website_API.DTO;
using RB_Website_API.Services;

namespace RB_Website_API.Controllers;

[ApiController]
[Route("api/admin/bulk-member-import")]
[Authorize(Policy = "SuperAdminOnly")]
public sealed class BulkMemberImportController : ControllerBase
{
    private readonly IBulkMemberImportService _import;

    public BulkMemberImportController(IBulkMemberImportService import) => _import = import;

    [HttpPost("process-batch")]
    public Task<BulkMemberImportBatchResponse> ProcessBatch(
        [FromBody] BulkMemberImportBatchRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return Task.FromResult(new BulkMemberImportBatchResponse(false, "Request is required."));

        return _import.ProcessBatchAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            req,
            ct);
    }

    [HttpPost("send-emails-batch")]
    public Task<BulkMemberImportBatchResponse> SendEmailsBatch(
        [FromBody] BulkImportEmailBatchRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return Task.FromResult(new BulkMemberImportBatchResponse(false, "Request is required."));

        return _import.SendWelcomeEmailsBatchAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            req,
            ct);
    }

    [HttpPost("send-emails-lookup-batch")]
    public Task<BulkMemberImportBatchResponse> SendEmailsLookupBatch(
        [FromBody] BulkImportEmailLookupBatchRequest? req,
        CancellationToken ct)
    {
        if (req is null)
            return Task.FromResult(new BulkMemberImportBatchResponse(false, "Request is required."));

        return _import.SendWelcomeEmailsLookupBatchAsync(
            CurrentUser.RequireUserId(User),
            CurrentUser.GetRole(User) ?? "",
            req,
            ct);
    }
}
