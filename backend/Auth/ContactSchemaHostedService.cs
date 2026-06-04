using RB_Website_API.Data;

namespace RB_Website_API.Auth;

public sealed class ContactSchemaHostedService : IHostedService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ContactSchemaHostedService> _logger;

    public ContactSchemaHostedService(IServiceProvider services, ILogger<ContactSchemaHostedService> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await ContactSchemaBootstrap.EnsureAsync(db, _logger, cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
