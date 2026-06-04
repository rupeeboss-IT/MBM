using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;

namespace RB_Website_API.Services;

/// <summary>
/// Marks active plans as Expired when ActiveTo has passed.
/// </summary>
public sealed class SubscriptionExpiryHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SubscriptionExpiryHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);

    public SubscriptionExpiryHostedService(IServiceProvider services, ILogger<SubscriptionExpiryHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpirePlansAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription expiry job failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task ExpirePlansAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;

        var expired = await db.UserPlans
            .Where(up => up.Status == "Active" && up.ActiveTo != null && up.ActiveTo <= now)
            .ToListAsync(ct);

        if (expired.Count == 0) return;

        foreach (var up in expired)
        {
            up.Status = "Expired";
            up.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Marked {Count} user plan(s) as Expired.", expired.Count);
    }
}
