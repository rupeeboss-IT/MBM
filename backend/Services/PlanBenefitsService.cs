using Microsoft.EntityFrameworkCore;
using RB_Website_API.Auth;
using RB_Website_API.Data;

namespace RB_Website_API.Services;

public interface IPlanBenefitsService
{
    Task<IReadOnlyList<string>> GetBenefitTextsAsync(string planCode, CancellationToken ct = default);
}

public sealed class PlanBenefitsService : IPlanBenefitsService
{
    private readonly AppDbContext _db;

    public PlanBenefitsService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<string>> GetBenefitTextsAsync(string planCode, CancellationToken ct = default)
    {
        var code = planCode.Trim();
        if (string.IsNullOrWhiteSpace(code)) return Array.Empty<string>();

        if (!PlanCmsCodes.IsManaged(code))
            return PlanBenefitsCatalog.GetBenefits(code);

        var features = await (
            from f in _db.PlanFeatures.AsNoTracking()
            join p in _db.Plans.AsNoTracking() on f.PlanId equals p.PlanId
            where p.Code == code
            orderby f.SortOrder, f.PlanFeatureId
            select f.Text
        ).ToListAsync(ct);

        if (features.Count > 0) return features;

        return PlanBenefitsCatalog.GetBenefits(code);
    }
}
