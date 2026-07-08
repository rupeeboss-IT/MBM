namespace RB_Website_API.DTO;

public sealed record CreditRepairManagementStatsDto(
    int TotalLeads,
    int TodayLeads,
    int Last7DaysLeads,
    int LinkedToLeadData,
    int UnlinkedLeads);

public sealed record CreditRepairSourceBreakdownDto(string Source, int Count);

public sealed record CreditRepairCampaignBreakdownDto(string CampaignName, int Count);

public sealed record CreditRepairListItemDto(
    long Id,
    string FullName,
    string Phone,
    string? Email,
    string Source,
    string CampaignName,
    bool ConsentAccepted,
    DateTime CreatedAt,
    int? LeadId);

public sealed record CreditRepairManagementStatsResponse(
    bool Success,
    string? Message = null,
    CreditRepairManagementStatsDto? Stats = null,
    IReadOnlyList<CreditRepairSourceBreakdownDto>? SourceBreakdown = null,
    IReadOnlyList<CreditRepairCampaignBreakdownDto>? CampaignBreakdown = null);

public sealed record CreditRepairManagementListResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<CreditRepairListItemDto>? Leads = null,
    int Total = 0,
    int Page = 1,
    int PageSize = 100);

public sealed record CreditRepairFiltersResponse(
    bool Success,
    string? Message = null,
    IReadOnlyList<string>? Sources = null,
    IReadOnlyList<string>? Campaigns = null);
