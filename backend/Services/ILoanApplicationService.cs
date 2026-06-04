namespace RB_Website_API.Services;

public interface ILoanApplicationService
{
    Task<(bool Success, string? Message, int? LeadId)> SubmitAsync(
        string fullName,
        string mobile,
        string? email,
        string pincode,
        int loanTypeId,
        string loanAmount,
        bool consentAccepted,
        string? referralCode,
        CancellationToken ct);
}
