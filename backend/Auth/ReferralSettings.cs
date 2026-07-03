namespace RB_Website_API.Auth;

public sealed class ReferralSettings
{
    public const string SectionName = "ReferralSettings";

    /// <summary>Default Emp_Code when buyer skips referral (e.g. RB600000251).</summary>
    public string DefaultEmployeeReferralCode { get; set; } = "RB600000251";

    public int ProductId { get; set; } = 1039;

    public string LeadSource { get; set; } = "MBM Website";

    /// <summary>lead_data.lead_source for membership payment leads when no per-order override is stored.</summary>
    public string MembershipLeadSource { get; set; } = RegistrationLeadSources.MsmeRegistration;

    public string CampaignName { get; set; } = "Online Leads";

    /// <summary>lead_data.campaignName for credit rebuild enquiries.</summary>
    public string CreditRebuildCampaignName { get; set; } = "MSME credit rebuild";

    public int LeadStatusId { get; set; } = 46;

    public string LeadType { get; set; } = "Telecalling";

    public int SourceId { get; set; }
    /// <summary>lead_data.profession (int). Use 0 when employment type is unknown.</summary>
    public int DefaultProfession { get; set; }

    /// <summary>
    /// When true, invalid referral codes block order creation (user must fix or skip on frontend).
    /// </summary>
    public bool StrictReferralValidationOnOrderCreate { get; set; } = true;
}
