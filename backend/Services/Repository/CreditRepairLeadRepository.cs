using RB_Website_API.Data;
using RB_Website_API.Models;
using RB_Website_API.Services.IRepository;

namespace RB_Website_API.Services.Repository;

public sealed class CreditRepairLeadRepository : ICreditRepairLeadRepository
{
    private readonly AppDbContext _db;
    private readonly ILogger<CreditRepairLeadRepository> _log;

    public CreditRepairLeadRepository(AppDbContext db, ILogger<CreditRepairLeadRepository> log)
    {
        _db = db;
        _log = log;
    }

    public async Task<long> InsertAsync(CreditRepairLead lead, CancellationToken ct)
    {
        _db.CreditRepairLeads.Add(lead);
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "CreditRepair_Lead saved: Id {Id}, phone {Phone}, campaign {Campaign}, source {Source}.",
            lead.Id,
            lead.Phone,
            lead.CampaignName,
            lead.Source);

        return lead.Id;
    }

    public async Task UpdateLeadIdAsync(long creditRepairLeadId, int leadId, CancellationToken ct)
    {
        var lead = await _db.CreditRepairLeads.FindAsync([creditRepairLeadId], ct);
        if (lead is null)
        {
            _log.LogWarning("CreditRepair_Lead not found for lead_id update. Id {Id}", creditRepairLeadId);
            return;
        }

        lead.LeadId = leadId;
        await _db.SaveChangesAsync(ct);

        _log.LogInformation(
            "CreditRepair_Lead linked to lead_data. Id {Id}, lead_id {LeadId}.",
            creditRepairLeadId,
            leadId);
    }
}

