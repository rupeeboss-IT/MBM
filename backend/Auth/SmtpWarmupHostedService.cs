namespace RB_Website_API.Auth;

/// <summary>
/// Warms SMTP DNS/TCP before the first user-facing email send (e.g. registration OTP).
/// </summary>
public sealed class SmtpWarmupHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SmtpWarmupHostedService> _logger;

    public SmtpWarmupHostedService(IServiceProvider services, ILogger<SmtpWarmupHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = WarmupInBackgroundAsync(cancellationToken);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WarmupInBackgroundAsync(CancellationToken stoppingToken)
    {
        try
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), stoppingToken);
            var email = _services.GetRequiredService<IEmailSender>();
            _logger.LogInformation("SMTP warmup starting.");
            await email.WarmupAsync(stoppingToken);
            _logger.LogInformation("SMTP warmup finished.");
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // App shutting down during warmup.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP warmup background task failed.");
        }
    }
}
