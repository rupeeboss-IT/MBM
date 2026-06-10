namespace RB_Website_API.Auth;

public sealed class InvoiceSettings
{
    public const string SectionName = "InvoiceSettings";

    public string LegalName { get; set; } = "MSME Bharat Manch";
    public string Address { get; set; } = "";
    public string? Gstin { get; set; }
    public string? Pan { get; set; }
    public string SacCode { get; set; } = "998314";
    public string SupportEmail { get; set; } = "sales@rupeeboss.com";
    public string? SupportPhone { get; set; }
}
