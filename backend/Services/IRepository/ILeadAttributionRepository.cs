using RB_Website_API.DTO;
using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface ILeadAttributionRepository
{
    Task<IReadOnlyList<Guid>> GetScopedPartnerIdsAsync(Guid actorId, CancellationToken ct);

    Task<List<User>> ListScopedMembersAsync(
        bool isSuperAdmin,
        Guid actorId,
        IReadOnlyList<Guid> partnerIds,
        CancellationToken ct);

    Task<Dictionary<Guid, int>> GetReportCountsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct);

    Task<Dictionary<Guid, (string? PlanCode, string? PlanName, string Status)>> GetActivePlansAsync(
        IReadOnlyList<Guid> userIds,
        DateTime now,
        CancellationToken ct);

    Task<Dictionary<Guid, int>> GetMembershipSalesCountsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct);

    Task<Dictionary<Guid, int>> GetReportPurchaseCountsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct);

    Task<Dictionary<Guid, FirstReferralRow>> GetFirstPaidReferralsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct);

    Task<Dictionary<Guid, RegistrationAdvisorRow>> GetRegistrationAdvisorsAsync(
        IReadOnlyList<Guid> userIds,
        CancellationToken ct);

    Task<IReadOnlyList<PaymentHistoryRow>> GetPaymentHistoryAsync(Guid userId, CancellationToken ct);

    Task<IReadOnlyList<ReferralPaymentRow>> GetReferralPaymentsForUsersAsync(
        IReadOnlyList<Guid> userIds,
        DateTime? paidFrom,
        DateTime? paidToExclusive,
        CancellationToken ct);

    Task<Dictionary<Guid, User>> GetUsersByIdsAsync(IReadOnlyList<Guid> userIds, CancellationToken ct);

    Task<User?> GetMemberAsync(Guid userId, CancellationToken ct);
}

public sealed record ReferralPaymentRow(
    Guid PaymentOrderId,
    Guid UserId,
    string MemberId,
    string FullName,
    string PlanCode,
    string PlanName,
    DateTime PaidAt,
    string ReferralCode,
    long TotalAmountPaise);

public sealed record PaymentHistoryRow(
    Guid PaymentOrderId,
    string PlanCode,
    string PlanName,
    DateTime PaidAt,
    string? ReferralCode,
    long TotalAmountPaise);

public sealed record FirstReferralRow(
    Guid PaymentOrderId,
    string? ReferralCode,
    DateTime PaidAt,
    string PlanCode,
    string PlanName);

public sealed record RegistrationAdvisorRow(
    string? AdvisorCode,
    string? ResolvedEmpCode,
    string? LeadType,
    int? BrokerId,
    bool UsedDefaultEmployee);
