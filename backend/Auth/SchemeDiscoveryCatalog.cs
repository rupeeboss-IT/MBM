namespace RB_Website_API.Auth;

public static class SchemeDiscoveryCatalog
{
    public const string ReportType = "SchemeDiscovery";
    public const string OneTimePlanCode = "scheme-report-onetime";
    public const string EntitlementMembership = "MembershipBenefit";
    public const string EntitlementOneTime = "OneTimePurchase";
    public const string EntitlementAdmin = "AdminGeneration";

    public const string StatusAwaitingPayment = "AwaitingPayment";
    public const string StatusPending = "Pending";
    public const string StatusProcessing = "Processing";
    public const string StatusCompleted = "Completed";
    public const string StatusFailed = "Failed";

    private static readonly HashSet<string> IncludedPlanCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "premium",
        "pro",
    };

    public static bool IsIncludedPlan(string? planCode)
        => !string.IsNullOrWhiteSpace(planCode) && IncludedPlanCodes.Contains(planCode.Trim());

    public static bool IsOneTimeReportPlan(string? planCode)
        => string.Equals(planCode?.Trim(), OneTimePlanCode, StringComparison.OrdinalIgnoreCase);

    public static bool IsAwaitingPaymentStatus(string? status)
        => string.Equals(status?.Trim(), StatusAwaitingPayment, StringComparison.OrdinalIgnoreCase);

    public static bool IsActiveRequestStatus(string? status)
    {
        var s = (status ?? "").Trim();
        return s is StatusPending or StatusProcessing or StatusCompleted;
    }
}
