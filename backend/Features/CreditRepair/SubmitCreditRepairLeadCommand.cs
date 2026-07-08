namespace RB_Website_API.Features.CreditRepair;

public sealed record SubmitCreditRepairLeadCommand(
    string FullName,
    string Mobile,
    string Email,
    bool ConsentAccepted,
    string? AdvisorCode = null);

