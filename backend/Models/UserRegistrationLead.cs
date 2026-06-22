using System.ComponentModel.DataAnnotations;

using System.ComponentModel.DataAnnotations.Schema;

using RB_Website_API.Auth;



namespace RB_Website_API.Models;



/// <summary>

/// Tracks membership lead_data push at registration (idempotent per user).

/// </summary>

[Table("UserRegistrationLeads")]

public sealed class UserRegistrationLead

{

    [Key]

    public Guid UserId { get; set; }



    [MaxLength(50)]

    public string RegistrationSource { get; set; } = RegistrationLeadSources.MsmeRegistration;



    /// <summary>Buyer-entered advisor / referral code (RB* or GP*) at registration, if any.</summary>

    [MaxLength(50)]

    public string? AdvisorCode { get; set; }



    /// <summary>emp_code written to RBMAIN lead_data.</summary>

    [MaxLength(50)]

    public string? ResolvedEmpCode { get; set; }



    [MaxLength(50)]

    public string? LeadType { get; set; }



    public int? BrokerId { get; set; }



    /// <summary>RBMAIN lead_data.Lead_id after insert (for payment-time reconciliation).</summary>

    public int? LeadId { get; set; }



    public bool UsedDefaultEmployee { get; set; } = true;



    public DateTime LeadPushedAt { get; set; }



    public DateTime CreatedAt { get; set; }



    public DateTime UpdatedAt { get; set; }

}


