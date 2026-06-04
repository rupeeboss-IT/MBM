using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class ReferralService : IReferralService
{
    private readonly AppDbContext _db;

    public ReferralService(AppDbContext db)
    {
        _db = db;
    }

    public async Task SaveReferralForOrderAsync(Guid paymentOrderId, string? referralCode, CancellationToken ct)
    {
        var code = string.IsNullOrWhiteSpace(referralCode) ? null : referralCode.Trim();
        var now = DateTime.UtcNow;

        var existing = await _db.PaymentOrderReferrals
            .FirstOrDefaultAsync(r => r.PaymentOrderId == paymentOrderId, ct);

        if (existing is null)
        {
            _db.PaymentOrderReferrals.Add(new PaymentOrderReferral
            {
                PaymentOrderId = paymentOrderId,
                ReferralCode = code,
                CreatedAt = now,
                UpdatedAt = now,
            });
        }
        else
        {
            existing.ReferralCode = code;
            existing.UpdatedAt = now;
        }

        await _db.SaveChangesAsync(ct);
    }
}
