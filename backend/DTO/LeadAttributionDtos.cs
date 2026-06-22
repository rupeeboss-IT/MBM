namespace RB_Website_API.DTO;

public static class LeadSourceTypes
{
    public const string Employee = "Employee";
    public const string Partner = "Partner";
    public const string Direct = "Direct Website";
    public const string Organic = "Organic";
    public const string Campaign = "Campaign";
    public const string Unknown = "Unknown";
}

public sealed record LeadAttributionStatsDto(
    int TotalLeads,
    int DirectLeads,
    int EmployeeLeads,
    int PartnerLeads,
    int OrganicLeads,
    int CampaignLeads,
    int UnknownLeads,
    int TotalMembershipSales,
    int TotalReportRequests);

public sealed record LeadSourceBreakdownDto(
    string SourceType,
    int Count);

public sealed record LeadPerformerDto(
    string Name,
    string? Code,
    int LeadsGenerated,
    int MembershipsSold,
    int ReportsGenerated,
    decimal ConversionRate,
    long RevenuePaise,
    int PaymentCount);

public sealed record LeadPerformerPaymentDto(
    Guid UserId,
    string MemberId,
    string FullName,
    string PlanCode,
    string PlanName,
    string OrderType,
    DateTime PaidAt,
    long TotalAmountPaise,
    string? ReferralCode);

public sealed record LeadPerformerDetailResponse(
    bool Success,
    string? Message = null,
    string? PerformerName = null,
    string? PerformerCode = null,
    long TotalRevenuePaise = 0,
    int PaymentCount = 0,
    IReadOnlyList<LeadPerformerPaymentDto>? Payments = null);

public sealed record LeadCustomerListItemDto(
    Guid UserId,
    string MemberId,
    string FullName,
    string Phone,
    string Email,
    DateTime RegistrationDate,
    string SourceType,
    string? SourceName,
    string? SourceCode,
    string? AssignedEmployee,
    string? MembershipStatus,
    string? PlanName,
    int MembershipSalesCount,
    int ReportPurchaseCount,
    int ReportGeneratedCount,
    int ReportCount);

public sealed record LeadPaymentHistoryItemDto(
    Guid PaymentOrderId,
    string PlanCode,
    string PlanName,
    string OrderType,
    DateTime PaidAt,
    string? ReferralCode,
    long TotalAmountPaise);

public sealed record LeadCustomerDetailDto(
    Guid UserId,
    string MemberId,
    string FullName,
    string Phone,
    string Email,
    DateTime RegistrationDate,
    string SourceType,
    string? SourceName,
    string? SourceCode,
    string? AssignedAdvisor,
    string? AssignedEmployee,
    string? CreatedThrough,
    string? MembershipStatus,
    string? PlanCode,
    string? PlanName,
    int ReportPurchaseCount,
    int ReportGeneratedCount,
    int ReportCount,
    int MembershipSalesCount,
    DateTime? FirstPaidAt,
    string? FirstPaidPlanCode,
    string? FirstPaidPlanName,
    string? FirstReferralCodeRaw,
    string? RegistrationAdvisorCodeRaw,
    IReadOnlyList<LeadPaymentHistoryItemDto>? PaymentHistory);

public sealed record LeadAttributionStatsResponse(
    bool Success,
    string? Message = null,
    LeadAttributionStatsDto? Stats = null,
    IReadOnlyList<LeadSourceBreakdownDto>? SourceBreakdown = null,
    IReadOnlyList<LeadPerformerDto>? TopEmployees = null,
    IReadOnlyList<LeadPerformerDto>? TopPartners = null);

public sealed record LeadCustomerListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<LeadCustomerListItemDto>? Customers = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 10);

public sealed record LeadCustomerDetailResponse(
    bool Success,
    string? Message = null,
    LeadCustomerDetailDto? Customer = null);

public sealed record LeadFilterOptionsResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<string>? SourceTypes = null,
    IReadOnlyList<string>? SourceNames = null,
    IReadOnlyList<string>? EmployeeNames = null,
    IReadOnlyList<string>? PartnerNames = null,
    IReadOnlyList<string>? PlanCodes = null);
