using RB_Website_API.Models;

namespace RB_Website_API.Services;

public interface IMemberIdGeneratorService
{
    /// <summary>Allocates the next MBM{YY}{####} member ID for the current calendar year.</summary>
    Task<string> AllocateNextMemberIdAsync(CancellationToken ct);

    /// <summary>Display member ID: new format when present, legacy hex for older accounts.</summary>
    string GetDisplayMemberId(User user);

    /// <summary>Resolves a member ID search term to internal UserId when possible.</summary>
    Task<Guid?> ResolveUserIdAsync(string? memberIdOrLegacy, CancellationToken ct);
}
