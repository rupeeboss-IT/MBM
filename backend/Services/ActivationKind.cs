namespace RB_Website_API.Services;

public enum ActivationKind
{
    None = 0,
    FirstPurchase,
    Renewal,
    Upgrade,
    OneTimeReport,
}

public sealed record ActivationResult(
    bool Activated,
    ActivationKind Kind,
    Guid? PaymentId,
    string? PlanCode,
    string? PlanName,
    string? PreviousPlanCode,
    string? PreviousPlanName,
    DateTime? ActiveFrom,
    DateTime? ActiveTo);
