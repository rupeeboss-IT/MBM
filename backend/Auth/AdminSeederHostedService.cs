using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RB_Website_API.Data;
using RB_Website_API.Models;

namespace RB_Website_API.Auth;

public sealed class AdminSeederHostedService : IHostedService
{
    private readonly IServiceProvider _sp;
    private readonly AdminSeedSettings _seed;
    private readonly ILogger<AdminSeederHostedService> _log;

    public AdminSeederHostedService(IServiceProvider sp, IOptions<AdminSeedSettings> seed, ILogger<AdminSeederHostedService> log)
    {
        _sp = sp;
        _seed = seed.Value;
        _log = log;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (_seed.Enabled != true) return;

        var email = (_seed.Email ?? "").Trim().ToLowerInvariant();
        var phone = IndianPhone.Digits(_seed.Phone ?? "");
        var password = (_seed.Password ?? "").Trim();
        var fullName = (_seed.FullName ?? "").Trim();

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(password))
        {
            _log.LogWarning("AdminSeed is enabled but Email/Phone/Password are missing. Skipping admin seed.");
            return;
        }

        if (phone.Length != 10)
        {
            _log.LogWarning("AdminSeed phone must be 10 digits. Skipping admin seed.");
            return;
        }

        await using var scope = _sp.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        try
        {
            var existing = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (existing is not null)
            {
                _log.LogInformation("Superadmin already exists ({Email}).", email);
                return;
            }

            var (hash, salt) = PasswordHasher.Hash(password);
            var now = DateTime.UtcNow;

            db.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                Role = "superadmin",
                FullName = string.IsNullOrWhiteSpace(fullName) ? "Super Admin" : fullName,
                Email = email,
                Phone = phone,
                CompanyName = null,
                PasswordHash = hash,
                PasswordSalt = salt,
                EmailVerifiedAt = now,
                PhoneVerifiedAt = now,
                ConsentAccepted = true,
                ConsentAcceptedAt = now,
                IsActive = true,
                CreatedAt = now,
                UpdatedAt = now,
            });

            await db.SaveChangesAsync(cancellationToken);
            _log.LogInformation("Seeded superadmin ({Email}).", email);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to seed superadmin.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

