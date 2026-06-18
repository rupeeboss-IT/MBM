namespace RB_Website_API.Auth;

public sealed class InvoiceSettings
{
    public const string SectionName = "InvoiceSettings";

    public string LegalName { get; set; } = "MSME Bharat Manch";
    public string Address { get; set; } = "";
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string SacCode { get; set; } = "998314";
    public string Website { get; set; } = "www.msmebharatmanch.com";
    public string SupportEmail { get; set; } = "support@msmebharatmanch.com";
    public string? SupportPhone { get; set; }

    /// <summary>When true, GST is shown as IGST; otherwise CGST + SGST (half rate each).</summary>
    public bool UseIgst { get; set; }
}
