using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Auth;
using RB_Website_API.Data;

namespace RB_Website_API.Services;

/// <summary>
/// Sends renewal reminder emails for plans expiring within 30 days (not cancelled at period end).
/// </summary>
public sealed class SubscriptionReminderHostedService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SubscriptionReminderHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);

    public SubscriptionReminderHostedService(IServiceProvider services, ILogger<SubscriptionReminderHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SendRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Subscription reminder job failed.");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }

    private async Task SendRemindersAsync(CancellationToken ct)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetService<IEmailSender>();
        var urls = scope.ServiceProvider.GetRequiredService<IOptions<ApplicationUrlsSettings>>().Value;
        if (email is null) return;

        var now = DateTime.Now;
        var windowEnd = now.AddDays(30);

        var due = await (
            from up in db.UserPlans.AsNoTracking()
            join u in db.Users.AsNoTracking() on up.UserId equals u.UserId
            join p in db.Plans.AsNoTracking() on up.PlanId equals p.PlanId
            where up.Status == "Active"
                  && !up.CancelAtPeriodEnd
                  && up.ActiveTo != null
                  && up.ActiveTo > now
                  && up.ActiveTo <= windowEnd
            select new { u.Email, u.FullName, p.Name, up.ActiveTo }
        ).ToListAsync(ct);

        foreach (var row in due)
        {
            if (string.IsNullOrWhiteSpace(row.Email) || row.ActiveTo is null) continue;
            var days = (int)Math.Ceiling((row.ActiveTo.Value - now).TotalDays);
            if (days != 30 && days != 7 && days != 1) continue;

            var subject = $"Your MSME Bharat Manch {row.Name} plan expires in {days} day(s)";
            var body = $"""
                <p>Hello {row.FullName},</p>
                <p>Your <strong>{row.Name}</strong> membership expires on <strong>{row.ActiveTo:dd MMM yyyy}</strong>.</p>
                <p>Renew at <a href="{urls.MembershipUrl}">{urls.MembershipUrl}</a> to keep your benefits.</p>
                <p>— MSME Bharat Manch</p>
                """;

            try
            {
                await email.SendAsync(row.Email, subject, body, ct);
                _logger.LogInformation("Renewal reminder sent to {Email} ({Days} days left).", row.Email, days);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not send renewal reminder to {Email}.", row.Email);
            }
        }
    }
}
