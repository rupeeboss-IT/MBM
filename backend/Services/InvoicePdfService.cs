using System.Globalization;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RB_Website_API.Auth;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class InvoicePdfService
{
    private readonly InvoiceSettings _settings;

    public InvoicePdfService(IOptions<InvoiceSettings> settings)
    {
        _settings = settings.Value;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(
        Payment payment,
        PaymentOrder order,
        Plan plan,
        User user,
        DateTime activeFrom,
        DateTime? activeTo)
    {
        var invoiceNo = InvoiceNumber.ForPayment(payment.PaymentId, payment.PaidAt);
        var isTaxInvoice = !string.IsNullOrWhiteSpace(_settings.Gstin);
        var benefits = PlanBenefitsCatalog.GetBenefits(plan.Code);
        var paidIst = ToIst(payment.PaidAt);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(col =>
                {
                    col.Item().Text(isTaxInvoice ? "TAX INVOICE" : "PAYMENT RECEIPT")
                        .FontSize(18).Bold().FontColor(Colors.Red.Medium);
                    col.Item().Text(_settings.LegalName).FontSize(12).SemiBold();
                    if (!string.IsNullOrWhiteSpace(_settings.Address))
                        col.Item().Text(_settings.Address);
                    if (isTaxInvoice)
                    {
                        col.Item().Text($"GSTIN: {_settings.Gstin}");
                        if (!string.IsNullOrWhiteSpace(_settings.Pan))
                            col.Item().Text($"PAN: {_settings.Pan}");
                    }
                });

                page.Content().PaddingVertical(16).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Bill To").SemiBold();
                            c.Item().Text(user.FullName);
                            c.Item().Text(user.Email);
                            c.Item().Text(user.Phone);
                        });
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text($"Invoice #: {invoiceNo}");
                            c.Item().Text($"Date: {paidIst:dd MMM yyyy}");
                            c.Item().Text($"Payment ID: {payment.RazorpayPaymentId}");
                            if (isTaxInvoice && !string.IsNullOrWhiteSpace(_settings.SacCode))
                                c.Item().Text($"SAC: {_settings.SacCode}");
                        });
                    });

                    col.Item().LineHorizontal(1);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(3);
                            cols.RelativeColumn(1);
                        });

                        table.Header(header =>
                        {
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).Text("Description").SemiBold();
                            header.Cell().Background(Colors.Grey.Lighten3).Padding(4).AlignRight().Text("Amount (INR)").SemiBold();
                        });

                        if (isTaxInvoice)
                        {
                            table.Cell().Padding(4).Text($"{plan.Name} — annual membership");
                            table.Cell().Padding(4).AlignRight().Text(FormatInr(order.BaseAmountPaise));

                            table.Cell().Padding(4).Text($"GST ({plan.GstPercent.ToString("0.##", CultureInfo.InvariantCulture)}%)");
                            table.Cell().Padding(4).AlignRight().Text(FormatInr(order.GstPaise));

                            table.Cell().Padding(4).Text("Total").SemiBold();
                            table.Cell().Padding(4).AlignRight().Text(FormatInr(order.TotalAmountPaise)).SemiBold();
                        }
                        else
                        {
                            table.Cell().Padding(4).Text($"{plan.Name} — annual membership");
                            table.Cell().Padding(4).AlignRight().Text(FormatInr(order.TotalAmountPaise));
                        }
                    });

                    col.Item().Text($"Membership period: {ToIst(activeFrom):dd MMM yyyy} — {(activeTo.HasValue ? ToIst(activeTo.Value).ToString("dd MMM yyyy", CultureInfo.InvariantCulture) : "—")}");

                    if (benefits.Count > 0)
                    {
                        col.Item().PaddingTop(8).Text("Benefits included:").SemiBold();
                        foreach (var b in benefits)
                            col.Item().Text($"• {b}");
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Thank you for choosing MSME Bharat Manch. ");
                    if (!string.IsNullOrWhiteSpace(_settings.SupportEmail))
                        text.Span($"Support: {_settings.SupportEmail}");
                });
            });
        }).GeneratePdf();
    }

    private static string FormatInr(long paise)
        => (paise / 100m).ToString("N2", CultureInfo.GetCultureInfo("en-IN"));

    private static DateTime ToIst(DateTime utc)
    {
        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("India Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(
                utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc), tz);
        }
        catch
        {
            return utc.AddHours(5.5);
        }
    }
}
