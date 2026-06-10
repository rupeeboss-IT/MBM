using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;
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

        var user = await _db.Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.UserId == po.UserId, ct);

        if (user is null)
        {
            _logger.LogWarning("Lead push skipped: user not found for order {PaymentOrderId}.", paymentOrderId);
            return false;
        }

        var empCode = _settings.DefaultEmployeeReferralCode?.Trim();
        if (string.IsNullOrWhiteSpace(empCode))
            empCode = "RB600000251";

        var employee = await _employees.ResolveEmployeeForLeadAsync(referralRow?.ReferralCode, ct);
        if (employee is not null && !string.IsNullOrWhiteSpace(employee.ReferralCode))
            empCode = employee.ReferralCode;

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
            lead_source = Truncate(_settings.LeadSource, 500),
            lead_type = Truncate(_settings.LeadType, 50),
            campaignName = Truncate(_settings.CampaignName, 200),
            emp_code = Truncate(empCode, 20),
            Lead_Status_id = _settings.LeadStatusId,
            sysdate = now,
            lead_date = now,
            Created_Datetime = now,
        };

        try
        {
            _referralDb.LeadData.Add(lead);
            await _referralDb.SaveChangesAsync(ct);

            var nowUtc = DateTime.Now;
            if (referralRow is null)
            {
                _db.PaymentOrderReferrals.Add(new Models.PaymentOrderReferral
                {
                    PaymentOrderId = paymentOrderId,
                    ReferralCode = null,
                    LeadPushedAt = nowUtc,
                    CreatedAt = nowUtc,
                    UpdatedAt = nowUtc,
                });
            }
            else
            {
                referralRow.LeadPushedAt = nowUtc;
                referralRow.UpdatedAt = nowUtc;
            }

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation(
                "Lead inserted into lead_data (Lead_id pending) for order {PaymentOrderId}, emp_code {EmpCode}.",
                paymentOrderId,
                empCode);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to insert lead_data for order {PaymentOrderId}.", paymentOrderId);
            return false;
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var s = value.Trim();
        return s.Length <= maxLength ? s : s[..maxLength];
    }
}
