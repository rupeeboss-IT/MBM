using RB_Website_API.Models;

namespace RB_Website_API.Services.IRepository;

public interface ICreditRepairLeadRepository
{
    Task<long> InsertAsync(CreditRepairLead lead, CancellationToken ct);

    Task UpdateLeadIdAsync(long creditRepairLeadId, int leadId, CancellationToken ct);
}

