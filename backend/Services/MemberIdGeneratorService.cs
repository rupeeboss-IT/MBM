using Microsoft.EntityFrameworkCore;
using RB_Website_API.Data;
using RB_Website_API.Exceptions;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services;

public sealed class MemberIdGeneratorService : IMemberIdGeneratorService
{
    public const string Prefix = "MBM";
    public const int MaxSequencePerYear = 9999;

    private readonly IMemberIdSequenceRepository _sequence;
    private readonly AppDbContext _db;
    private readonly ILogger<MemberIdGeneratorService> _logger;

    public MemberIdGeneratorService(
        IMemberIdSequenceRepository sequence,
        AppDbContext db,
        ILogger<MemberIdGeneratorService> logger)
    {
        _sequence = sequence;
        _db = db;
        _logger = logger;
    }

    public async Task<string> AllocateNextMemberIdAsync(CancellationToken ct)
    {
        var year = DateTime.Now.Year;
        var yy = year % 100;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var next = await _sequence.GetNextSequenceNumberAsync(year, ct);
            if (next > MaxSequencePerYear)
            {
                _logger.LogError("Member ID sequence exhausted for year {Year}.", year);
                throw new UserFacingException(
                    "We are unable to assign a new member ID at this time. Please contact support.");
            }

            var memberId = $"{Prefix}{yy:D2}{next:D4}";
            var exists = await _db.Users.AsNoTracking().AnyAsync(u => u.MemberId == memberId, ct);
            if (!exists) return memberId;

            _logger.LogWarning("Member ID collision on {MemberId}, retrying allocation.", memberId);
        }

        throw new UserFacingException("Unable to assign a member ID. Please try again.");
    }

    public string GetDisplayMemberId(User user) => MemberIdHelper.GetDisplayMemberId(user);

    public async Task<Guid?> ResolveUserIdAsync(string? memberIdOrLegacy, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(memberIdOrLegacy)) return null;

        var term = memberIdOrLegacy.Trim();

        if (MemberIdHelper.TryParseLegacyGuid(term, out var legacyId))
        {
            var legacyUser = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.UserId == legacyId, ct);
            return legacyUser?.UserId;
        }

        if (MemberIdHelper.IsNewFormat(term))
        {
            var normalized = term.ToUpperInvariant();
            var user = await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.MemberId != null && u.MemberId.ToUpper() == normalized, ct);
            return user?.UserId;
        }

        return null;
    }
}
