using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Models;
using RB_Website_API.Referrals.Models;
using RB_Website_API.Referrals.Services;
using RB_Website_API.Services.IRepository;
using RB_Website_API.Services.Webhooks;

namespace RB_Website_API.Services;

public sealed class CreditRepairLeadService : ICreditRepairLeadService
{
    private static readonly string[] BannedNameWords =
    [
        "test", "dummy", "fake", "unknown", "n/a", "na", "null", "none", "abcd", "asdf",
    ];

    private readonly IZohoFlowWebhookClient _webhook;
    private readonly ICreditRepairLeadRepository _repo;
    private readonly ILeadPushService _leadPush;
    private readonly IEmployeeValidationService _employees;
    private readonly ReferralSettings _settings;
    private readonly ILogger<CreditRepairLeadService> _log;

    public CreditRepairLeadService(
        IZohoFlowWebhookClient webhook,
        ICreditRepairLeadRepository repo,
        ILeadPushService leadPush,
        IEmployeeValidationService employees,
        IOptions<ReferralSettings> settings,
        ILogger<CreditRepairLeadService> log)
    {
        _webhook = webhook;
        _repo = repo;
        _leadPush = leadPush;
        _employees = employees;
        _settings = settings.Value;
        _log = log;
    }

    public async Task<(bool Success, string? Message, int? LeadId)> SubmitAsync(
        string fullName,
        string mobile,
        string email,
        bool consentAccepted,
        string? advisorCode,
        string correlationId,
        CancellationToken ct)
    {
        using var scope = _log.BeginScope(new Dictionary<string, object?>
        {
            ["CorrelationId"] = correlationId,
            ["RequestId"] = correlationId,
        });

        var nameErr = ValidateName(fullName);
        if (nameErr != null)
        {
            _log.LogWarning("Validation failed: {Reason}", nameErr);
            return (false, nameErr, null);
        }

        var mobileErr = ValidateMobile(mobile);
        if (mobileErr != null)
        {
            _log.LogWarning("Validation failed: {Reason}", mobileErr);
            return (false, mobileErr, null);
        }

        var emailErr = ValidateEmail(email);
        if (emailErr != null)
        {
            _log.LogWarning("Validation failed: {Reason}", emailErr);
            return (false, emailErr, null);
        }

        if (consentAccepted != true)
        {
            _log.LogWarning("Validation failed: consent missing.");
            return (false, "Consent is required to submit this enquiry.", null);
        }

        _log.LogInformation("Validation successful. Preparing webhook request.");

        var webhookResult = await _webhook.SubmitCreditRepairLeadAsync(
            name: fullName.Trim(),
            email: email.Trim(),
            phone: mobile.Trim(),
            ct);

        if (!webhookResult.Success)
        {
            _log.LogWarning(
                "Webhook failed. StatusCode {StatusCode} ElapsedMs {ElapsedMs} Error {Error} Body {Body}",
                webhookResult.StatusCode,
                webhookResult.ElapsedMilliseconds,
                webhookResult.ErrorMessage,
                webhookResult.ResponseBody);

            return (false, "Unable to submit your enquiry right now. Please try again.", null);
        }

        _log.LogInformation("Webhook success. Saving CreditRepair_Lead.");

        var productId = _settings.CreditRepairProductId > 0 ? _settings.CreditRepairProductId : 1102;
        var leadSource = ResolveCreditRepairLeadSource();
        var campaignName = ResolveCreditRepairCampaignName();

        _log.LogInformation(
            "Credit repair lead_data overrides: productid {ProductId}, lead_source {LeadSource}, campaign {Campaign}.",
            productId,
            leadSource,
            campaignName);

        long creditRepairLeadId;
        try
        {
            creditRepairLeadId = await _repo.InsertAsync(new CreditRepairLead
            {
                FullName = fullName.Trim(),
                Phone = mobile.Trim(),
                Email = email.Trim(),
                ConsentAccepted = true,
                CreatedAt = DateTime.Now,
                Source = leadSource,
                CampaignName = campaignName,
            }, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Failed to save CreditRepair_Lead. Ensure table dbo.CreditRepair_Lead exists (see database/credit-repair-lead-schema.sql).");
            return (false, "Unable to submit your enquiry. Please try again.", null);
        }

        _log.LogInformation(
            "Saving Lead_Data (RBMAIN lead_data) with productid {ProductId}, lead_source {LeadSource}, campaign {Campaign}.",
            productId,
            leadSource,
            campaignName);

        var assignment = await ResolveLeadAssignmentAsync(advisorCode, ct);

        var (pushed, leadId) = await _leadPush.CreateLeadAfterCreditRepairSubmissionAsync(
            fullName: fullName.Trim(),
            mobile: mobile.Trim(),
            email: email.Trim(),
            productIdOverride: productId,
            leadSourceOverride: leadSource,
            campaignNameOverride: campaignName,
            remark: "MSME Bharat Manch — Credit Rebuild enquiry",
            empCode: assignment.EmpCode,
            leadType: assignment.LeadType,
            brokerId: assignment.BrokerId,
            ct);

        if (!pushed || leadId is null or <= 0)
        {
            _log.LogError("Lead_Data insert failed after CreditRepair_Lead saved for phone {Phone}.", mobile);
            return (false, "Unable to submit your enquiry. Please try again.", null);
        }

        try
        {
            await _repo.UpdateLeadIdAsync(creditRepairLeadId, leadId.Value, ct);
        }
        catch (Exception ex)
        {
            _log.LogError(
                ex,
                "Failed to link CreditRepair_Lead Id {CreditRepairLeadId} to lead_data lead_id {LeadId}.",
                creditRepairLeadId,
                leadId);
        }

        _log.LogInformation("Completed successfully. Lead_id {LeadId}", leadId);
        return (
            true,
            "Thank you! We have received your enquiry. Our credit rebuild team will contact you shortly.",
            leadId);
    }

    private string ResolveCreditRepairLeadSource()
    {
        var configured = _settings.CreditRepairLeadSource?.Trim();
        return string.IsNullOrWhiteSpace(configured) ? "mbmwebsite" : configured;
    }

    private string ResolveCreditRepairCampaignName()
    {
        var configured = _settings.CreditRepairCampaignName?.Trim();
        return string.IsNullOrWhiteSpace(configured) ? "creditrepair" : configured;
    }

    private string ResolveDefaultEmpCode()
    {
        var empCode = _settings.DefaultEmployeeReferralCode?.Trim();
        return string.IsNullOrWhiteSpace(empCode) ? "RB600000251" : empCode;
    }

    private static string? NormalizeAdvisorCode(string? advisorCode)
    {
        var trimmed = advisorCode?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private async Task<(string EmpCode, string LeadType, int? BrokerId)> ResolveLeadAssignmentAsync(
        string? advisorCode,
        CancellationToken ct)
    {
        var empCode = ResolveDefaultEmpCode();
        var leadType = _settings.LeadType;
        int? brokerId = null;

        var normalizedAdvisor = NormalizeAdvisorCode(advisorCode);
        var referral = await _employees.ResolveReferralForLeadAsync(normalizedAdvisor, ct);
        if (referral is not null && !string.IsNullOrWhiteSpace(referral.EmpCode))
        {
            empCode = referral.EmpCode;
            if (referral.ReferralType == ReferralType.RBA)
            {
                leadType = "RBA";
                brokerId = referral.BrokerId;
            }
        }

        return (empCode, leadType, brokerId);
    }

    private static string? ValidateName(string nameRaw)
    {
        var name = (nameRaw ?? "").Trim();
        if (name.Length == 0) return "Your name is required.";
        if (name.Length < 2) return "Please enter your full name.";
        if (name.Any(c => !char.IsLetter(c) && c != ' ')) return "Name can contain only letters and spaces.";
        if (name.Any(char.IsDigit)) return "Name cannot contain numbers.";
        if (Regex.IsMatch(name.Replace(" ", ""), @"(.)\1{3,}", RegexOptions.IgnoreCase))
            return "Name looks invalid (repeated characters).";

        var words = name.ToLowerInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Any(w => BannedNameWords.Contains(w)))
            return "Please enter a valid name.";

        return null;
    }

    private static string? ValidateMobile(string v)
    {
        var m = (v ?? "").Trim();
        if (m.Length == 0) return "Mobile number is required.";
        if (!m.All(char.IsDigit)) return "Mobile number must be numeric.";
        if (!Regex.IsMatch(m, @"^[6-9]\d{9}$"))
            return "Enter a valid 10-digit Indian mobile number (starts with 6-9).";
        return null;
    }

    private static string? ValidateEmail(string v)
    {
        var e = (v ?? "").Trim();
        if (e.Length == 0) return "Email address is required.";
        if (!Regex.IsMatch(e, @"^[^\s@]+@[^\s@]+\.[^\s@]{2,}$"))
            return "Enter a valid email address.";
        return null;
    }
}
