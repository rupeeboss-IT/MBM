using RB_Website_API.Data;

namespace RB_Website_API.Auth;

/// <summary>
/// Ensures loan application table exists in MBM at startup (no EF migrations in this project).
/// </summary>
public sealed class LoanApplicationSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LoanApplicationSchemaHostedService> _logger;

    public LoanApplicationSchemaHostedService(IServiceProvider services, ILogger<LoanApplicationSchemaHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await LoanApplicationSchemaBootstrap.EnsureAsync(db, _logger, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
