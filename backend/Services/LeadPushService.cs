using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Referrals.Models;
using RB_Website_API.Referrals.Services;

namespace RB_Website_API.Services;

public sealed class LeadPushService : ILeadPushService
{
    private readonly AppDbContext _db;
    private readonly ReferralDbContext _referralDb;
    private readonly IEmployeeValidationService _employees;
    private readonly ReferralSettings _settings;
    private readonly ILogger<LeadPushService> _logger;

    public LeadPushService(
        AppDbContext db,
        ReferralDbContext referralDb,
        IEmployeeValidationService employees,
        IOptions<ReferralSettings> settings,
        ILogger<LeadPushService> logger)
    {
        _db = db;
        _referralDb = referralDb;
        _employees = employees;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> CreateLeadAfterRegistrationAsync(
        Guid userId,
        string? registrationSource,
        string? advisorCode,
        CancellationToken ct)
    {
        var existing = await _db.UserRegistrationLeads
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId, ct);

        if (existing?.LeadPushedAt != default)
        {
            _logger.LogInformation("Registration lead already pushed for user {UserId}.", userId);
            return true;
        }

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == userId && !u.IsDeleted, ct);

        if (user is null)
        {
            _logger.LogWarning("Registration lead push skipped: user {UserId} not found.", userId);
            return false;
        }

        var role = (user.Role ?? "").Trim().ToLowerInvariant();
        if (role is not ("member" or "partner"))
        {
            _logger.LogInformation("Registration lead push skipped: role {Role} for user {UserId}.", role, userId);
            return false;
        }

        var leadSource = ResolveRegistrationLeadSource(registrationSource);
        var assignment = await ResolveLeadAssignmentAsync(advisorCode, ct);
        var normalizedAdvisor = NormalizeAdvisorCode(advisorCode);

        return await InsertMembershipLeadAsync(
            user,
            leadSource,
            assignment.EmpCode,
            assignment.LeadType,
            assignment.BrokerId,
            onSuccess: async (now, leadId) =>
            {
                var row = await _db.UserRegistrationLeads.FirstOrDefaultAsync(x => x.UserId == userId, ct);
                if (row is null)
                {
                    _db.UserRegistrationLeads.Add(new UserRegistrationLead
                    {
                        UserId = userId,
                        RegistrationSource = leadSource,
                        AdvisorCode = normalizedAdvisor,
                        ResolvedEmpCode = assignment.EmpCode,
                        LeadType = assignment.LeadType,
                        BrokerId = assignment.BrokerId,
                        LeadId = leadId > 0 ? leadId : null,
                        UsedDefaultEmployee = assignment.UsedDefaultEmployee,
                        LeadPushedAt = now,
                        CreatedAt = now,
                        UpdatedAt = now,
                    });
                }
                else
                {
                    row.RegistrationSource = leadSource;
                    row.AdvisorCode = normalizedAdvisor;
                    row.ResolvedEmpCode = assignment.EmpCode;
                    row.LeadType = assignment.LeadType;
                    row.BrokerId = assignment.BrokerId;
                    row.LeadId = leadId > 0 ? leadId : row.LeadId;
                    row.UsedDefaultEmployee = assignment.UsedDefaultEmployee;
                    row.LeadPushedAt = now;
                    row.UpdatedAt = now;
                }

                await _db.SaveChangesAsync(ct);
            },
            logContext: $"registration user {userId}",
            ct);
    }

    public async Task<bool> CreateLeadAfterPaymentAsync(Guid paymentOrderId, CancellationToken ct)
    {
        var po = await _db.PaymentOrders.AsNoTracking()
            .FirstOrDefaultAsync(p => p.PaymentOrderId == paymentOrderId, ct);

        if (po is null || po.Status != "Paid")
        {
            _logger.LogWarning("Lead push skipped: order {PaymentOrderId} not paid.", paymentOrderId);
            return false;
        }

        var referralRow = await _db.PaymentOrderReferrals
            .FirstOrDefaultAsync(r => r.PaymentOrderId == paymentOrderId, ct);

        if (referralRow?.LeadPushedAt is not null)
        {
            _logger.LogInformation("Lead already pushed for order {PaymentOrderId}.", paymentOrderId);
            return true;
        }

        var registrationLead = await _db.UserRegistrationLeads
            .FirstOrDefaultAsync(x => x.UserId == po.UserId, ct);

        if (registrationLead?.LeadPushedAt != default)
        {
            _logger.LogInformation(
                "Payment lead push skipped: registration lead already exists for user {UserId} (order {PaymentOrderId}).",
                po.UserId,
                paymentOrderId);

            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == po.UserId, ct);

            if (user is not null && registrationLead is not null)
            {
                await ApplyPaymentAdvisorAssignmentAsync(
                    user,
                    registrationLead,
                    referralRow?.ReferralCode,
                    ct);
            }

            await MarkPaymentReferralLeadPushedAsync(paymentOrderId, referralRow, DateTime.Now, ct);
            return true;
        }

        var paymentUser = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == po.UserId, ct);

        if (paymentUser is null)
        {
            _logger.LogWarning("Lead push skipped: user not found for order {PaymentOrderId}.", paymentOrderId);
            return false;
        }

        var assignment = await ResolveLeadAssignmentAsync(referralRow?.ReferralCode, ct);
        var leadSource = ResolveMembershipLeadSourceFromPayment(po.Notes);

        return await InsertMembershipLeadAsync(
            paymentUser,
            leadSource,
            assignment.EmpCode,
            assignment.LeadType,
            assignment.BrokerId,
            onSuccess: async (now, _) =>
            {
                await MarkPaymentReferralLeadPushedAsync(paymentOrderId, referralRow, now, ct);
            },
            logContext: $"order {paymentOrderId}",
            ct);
    }

    public async Task<bool> CreateLeadAfterContactSubmissionAsync(
        string fullName,
        string mobile,
        string? email,
        string? leadSource,
        string? remark,
        CancellationToken ct)
    {
        var empCode = ResolveDefaultEmpCode();
        var resolvedSource = ContactLeadSources.Resolve(leadSource);
        var resolvedLeadType = ContactLeadSources.ResolveLeadType(leadSource);
        var now = DateTime.Now;

        var lead = new LeadData
        {
            name = Truncate(fullName, 100),
            mobile = Truncate(mobile, 50),
            email = Truncate(email, 300),
            productid = _settings.ProductId,
            Pincode = null,
            profession = _settings.DefaultProfession,
            source_id = _settings.SourceId,
            lead_source = Truncate(resolvedSource, 500),
            lead_type = Truncate(resolvedLeadType, 50),
            campaignName = Truncate(_settings.CampaignName, 200),
            emp_code = Truncate(empCode, 20),
            broker_id = null,
            Lead_Status_id = _settings.LeadStatusId,
            sysdate = now,
            lead_date = now,
            Created_Datetime = now,
            remark = Truncate(remark, 4000),
        };

        try
        {
            _referralDb.LeadData.Add(lead);
            await _referralDb.SaveChangesAsync(ct);

            var leadId = await ResolveLeadIdAfterInsertAsync(lead, ct);

            _logger.LogInformation(
                "Lead inserted into lead_data for contact submission, Lead_id {LeadId}, emp_code {EmpCode}, lead_source {LeadSource}, lead_type {LeadType}.",
                leadId,
                empCode,
                resolvedSource,
                resolvedLeadType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert lead_data for contact submission.");
            return false;
        }
    }

    public async Task<(bool Success, int? LeadId)> CreateLeadAfterCreditRepairSubmissionAsync(
        string fullName,
        string mobile,
        string? email,
        int productIdOverride,
        string leadSourceOverride,
        string campaignNameOverride,
        string? remark,
        string empCode,
        string leadType,
        int? brokerId,
        CancellationToken ct)
    {
        var now = DateTime.Now;

        var lead = new LeadData
        {
            name = Truncate(fullName, 100),
            mobile = Truncate(mobile, 50),
            email = Truncate(email, 300),
            productid = productIdOverride,
            Pincode = null,
            profession = _settings.DefaultProfession,
            source_id = _settings.SourceId,
            lead_source = Truncate(leadSourceOverride, 500),
            lead_type = Truncate(leadType, 50),
            campaignName = Truncate(campaignNameOverride, 200),
            emp_code = Truncate(empCode, 20),
            broker_id = brokerId,
            Lead_Status_id = _settings.LeadStatusId,
            sysdate = now,
            lead_date = now,
            Created_Datetime = now,
            remark = Truncate(remark, 4000),
        };

        try
        {
            _referralDb.LeadData.Add(lead);
            await _referralDb.SaveChangesAsync(ct);

            var leadId = await ResolveLeadIdAfterInsertAsync(lead, ct);

            if (leadId > 0)
            {
                // lead_data has INSERT triggers; re-apply credit-repair overrides after insert.
                await _referralDb.Database.ExecuteSqlInterpolatedAsync(
                    $"""
                     UPDATE lead_data
                     SET productid = {productIdOverride},
                         lead_source = {Truncate(leadSourceOverride, 500)},
                         campaignName = {Truncate(campaignNameOverride, 200)}
                     WHERE Lead_id = {leadId}
                     """,
                    ct);
            }

            _logger.LogInformation(
                "Lead inserted into lead_data for credit repair submission, Lead_id {LeadId}, productid {ProductId}, emp_code {EmpCode}, lead_source {LeadSource}, campaign {Campaign}.",
                leadId,
                productIdOverride,
                empCode,
                leadSourceOverride,
                campaignNameOverride);

            return (true, leadId > 0 ? leadId : null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert lead_data for credit repair submission.");
            return (false, null);
        }
    }

    /// <summary>
    /// Re-validates advisor code at payment time. Active codes map to the advisor bucket;
    /// inactive or invalid codes fall back to the default employee bucket in CRM.
    /// </summary>
    private async Task ApplyPaymentAdvisorAssignmentAsync(
        User user,
        UserRegistrationLead registrationLead,
        string? paymentReferralCode,
        CancellationToken ct)
    {
        var paymentCode = NormalizeAdvisorCode(paymentReferralCode);
        var registrationCode = NormalizeAdvisorCode(registrationLead.AdvisorCode);
        var effectiveCode = paymentCode ?? registrationCode;

        if (string.IsNullOrWhiteSpace(effectiveCode))
            return;

        var assignment = await ResolveLeadAssignmentAsync(effectiveCode, ct);

        var leadId = registrationLead.LeadId;
        if (leadId is null or <= 0)
            leadId = await FindMembershipLeadIdAsync(user, ct);

        if (leadId is null or <= 0)
        {
            _logger.LogWarning(
                "Payment advisor assignment skipped for user {UserId}: CRM Lead_id not found.",
                user.UserId);
            return;
        }

        try
        {
            await _referralDb.Database.ExecuteSqlInterpolatedAsync(
                $"""
                 UPDATE lead_data
                 SET emp_code = {Truncate(assignment.EmpCode, 20)},
                     lead_type = {Truncate(assignment.LeadType, 50)},
                     broker_id = {assignment.BrokerId}
                 WHERE Lead_id = {leadId.Value}
                 """,
                ct);

            registrationLead.AdvisorCode = effectiveCode;
            registrationLead.ResolvedEmpCode = assignment.EmpCode;
            registrationLead.LeadType = assignment.LeadType;
            registrationLead.BrokerId = assignment.BrokerId;
            registrationLead.LeadId = leadId;
            registrationLead.UsedDefaultEmployee = assignment.UsedDefaultEmployee;
            registrationLead.UpdatedAt = DateTime.Now;
            await _db.SaveChangesAsync(ct);

            if (assignment.UsedDefaultEmployee)
            {
                _logger.LogInformation(
                    "Payment advisor assignment for user {UserId}: code {Code} inactive/invalid — default employee {EmpCode} (Lead_id {LeadId}).",
                    user.UserId,
                    effectiveCode,
                    assignment.EmpCode,
                    leadId);
            }
            else
            {
                _logger.LogInformation(
                    "Payment advisor assignment for user {UserId}: active code {Code} → emp_code {EmpCode} (Lead_id {LeadId}).",
                    user.UserId,
                    effectiveCode,
                    assignment.EmpCode,
                    leadId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed payment advisor assignment for user {UserId} on CRM lead {LeadId}.",
                user.UserId,
                leadId);
        }
    }

    private async Task<int?> FindMembershipLeadIdAsync(User user, CancellationToken ct)
    {
        var phone = (user.Phone ?? "").Trim();
        if (phone.Length == 0) return null;

        return await _referralDb.LeadData.AsNoTracking()
            .Where(l => l.mobile == phone && l.productid == _settings.ProductId)
            .OrderByDescending(l => l.Lead_id)
            .Select(l => (int?)l.Lead_id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<LeadAssignment> ResolveLeadAssignmentAsync(string? advisorCode, CancellationToken ct)
    {
        var defaultEmpCode = ResolveDefaultEmpCode();
        var leadType = _settings.LeadType;
        int? brokerId = null;
        var usedDefault = true;

        var referral = await _employees.ResolveReferralForLeadAsync(advisorCode, ct);
        if (referral is not null && !string.IsNullOrWhiteSpace(referral.EmpCode))
        {
            usedDefault = referral.UsedDefaultEmployee;
            if (usedDefault && !string.IsNullOrWhiteSpace(advisorCode))
            {
                _logger.LogInformation(
                    "Lead assignment for advisor {Code}: using default employee {EmpCode}.",
                    advisorCode.Trim(),
                    referral.EmpCode);
            }

            return new LeadAssignment(
                referral.EmpCode,
                referral.ReferralType == ReferralType.RBA ? "RBA" : leadType,
                referral.ReferralType == ReferralType.RBA ? referral.BrokerId : null,
                usedDefault);
        }

        return new LeadAssignment(defaultEmpCode, leadType, brokerId, UsedDefaultEmployee: true);
    }

    private async Task<bool> InsertMembershipLeadAsync(
        User user,
        string leadSource,
        string empCode,
        string leadType,
        int? brokerId,
        Func<DateTime, int, Task> onSuccess,
        string logContext,
        CancellationToken ct)
    {
        var now = DateTime.Now;

        var lead = new LeadData
        {
            name = Truncate(user.FullName, 100),
            mobile = Truncate(user.Phone, 50),
            email = Truncate(user.Email, 300),
            productid = _settings.ProductId,
            Pincode = null,
            profession = _settings.DefaultProfession,
            source_id = _settings.SourceId,
            lead_source = Truncate(leadSource, 500),
            lead_type = Truncate(leadType, 50),
            campaignName = Truncate(_settings.CampaignName, 200),
            emp_code = Truncate(empCode, 20),
            broker_id = brokerId,
            Lead_Status_id = _settings.LeadStatusId,
            sysdate = now,
            lead_date = now,
            Created_Datetime = now,
        };

        try
        {
            _referralDb.LeadData.Add(lead);
            await _referralDb.SaveChangesAsync(ct);

            var leadId = await ResolveLeadIdAfterInsertAsync(lead, ct);
            await onSuccess(now, leadId);

            _logger.LogInformation(
                "Lead inserted into lead_data for {Context}, Lead_id {LeadId}, emp_code {EmpCode}, lead_source {LeadSource}, lead_type {LeadType}.",
                logContext,
                leadId,
                empCode,
                leadSource,
                leadType);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert lead_data for {Context}.", logContext);
            return false;
        }
    }

    private async Task<int> ResolveLeadIdAfterInsertAsync(LeadData lead, CancellationToken ct)
    {
        if (lead.Lead_id > 0)
            return lead.Lead_id;

        var scopeId = await _referralDb.Database
            .SqlQueryRaw<int>("SELECT CAST(SCOPE_IDENTITY() AS int) AS [Value]")
            .FirstOrDefaultAsync(ct);

        if (scopeId > 0)
            lead.Lead_id = scopeId;

        return lead.Lead_id;
    }

    private async Task MarkPaymentReferralLeadPushedAsync(
        Guid paymentOrderId,
        PaymentOrderReferral? referralRow,
        DateTime now,
        CancellationToken ct)
    {
        if (referralRow is null)
        {
            _db.PaymentOrderReferrals.Add(new PaymentOrderReferral
            {
                PaymentOrderId = paymentOrderId,
                ReferralCode = null,
                LeadPushedAt = now,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            referralRow.LeadPushedAt = now;
            referralRow.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }

    private static string? NormalizeAdvisorCode(string? advisorCode)
    {
        var trimmed = advisorCode?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private string ResolveDefaultEmpCode()
    {
        var empCode = _settings.DefaultEmployeeReferralCode?.Trim();
        return string.IsNullOrWhiteSpace(empCode) ? "RB600000251" : empCode;
    }

    private string ResolveRegistrationLeadSource(string? registrationSource)
    {
        var normalized = RegistrationLeadSources.Normalize(registrationSource);
        if (!string.IsNullOrWhiteSpace(normalized))
            return normalized;

        var configured = _settings.MembershipLeadSource?.Trim();
        if (!string.IsNullOrWhiteSpace(configured))
            return configured;

        return RegistrationLeadSources.MsmeRegistration;
    }

    private string ResolveMembershipLeadSourceFromPayment(string? paymentOrderNotes)
    {
        var fromOrder = PaymentOrderRegistrationSource.ReadFromNotes(paymentOrderNotes);
        if (!string.IsNullOrWhiteSpace(fromOrder))
            return fromOrder;

        return ResolveRegistrationLeadSource(null);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        return s.Length <= maxLength ? s : s[..maxLength];
    }

    private sealed record LeadAssignment(
        string EmpCode,
        string LeadType,
        int? BrokerId,
        bool UsedDefaultEmployee);
}
