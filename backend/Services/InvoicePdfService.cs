using System.Globalization;
using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using RB_Website_API.Auth;
using static RB_Website_API.Auth.AppDateTime;
using RB_Website_API.Models;

namespace RB_Website_API.Services;

public sealed class InvoicePdfService
{
    private const string LogoFileName = "mbmPoweredlogo.png";

    private static readonly string BrandRed = "#C8102E";
    private static readonly string TextDark = "#1A1A2E";
    private static readonly string TextMuted = "#5C5C6F";
    private static readonly string BorderColor = "#DEE2E6";
    private static readonly string SectionBg = "#F8F9FA";
    private static readonly string TableHeaderBg = "#EEF0F4";

    private readonly InvoiceSettings _settings;
    private readonly IWebHostEnvironment _env;

    public InvoicePdfService(IOptions<InvoiceSettings> settings, IWebHostEnvironment env)
    {
        _settings = settings.Value;
        _env = env;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Generate(
        Payment payment,
        PaymentOrder order,
        Plan plan,
        User user,
        DateTime activeFrom,
        DateTime? activeTo,
        IReadOnlyList<string>? benefits = null)
    {
        var invoiceNo = InvoiceNumber.ForPayment(payment.PaymentId, payment.PaidAt);
        var isTaxInvoice = !string.IsNullOrWhiteSpace(_settings.Gstin);
        var hasGst = order.GstPaise > 0 || plan.GstPercent > 0;
        var benefitList = benefits ?? PlanBenefitsCatalog.GetBenefits(plan.Code);
        var paidAt = Normalize(payment.PaidAt);
        var logo = TryLoadLogo();
        var orderNo = !string.IsNullOrWhiteSpace(payment.RazorpayOrderId)
            ? payment.RazorpayOrderId
            : order.RazorpayOrderId ?? order.PaymentOrderId.ToString();
        var membershipStatus = activeTo is null || Normalize(activeTo.Value) >= Now.Date
            ? "Active"
            : "Expired";
        var durationLabel = FormatDuration(plan.DurationDays);
        var halfGstRate = plan.GstPercent / 2m;
        var cgstPaise = order.GstPaise / 2;
        var sgstPaise = order.GstPaise - cgstPaise;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(36);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextDark));

                page.Header().Column(header =>
                {
                    header.Item().Row(row =>
                    {
                        row.RelativeItem(3).Column(left =>
                        {
                            if (logo is not null)
                            {
                                left.Item().Height(48).Image(logo).FitHeight();
                            }
                        });

                        row.RelativeItem(2).AlignRight().Column(right =>
                        {
                            right.Item().Text(isTaxInvoice ? "TAX INVOICE" : "PAYMENT RECEIPT")
                                .FontSize(20).Bold().FontColor(BrandRed);
                            right.Item().PaddingTop(8).Column(meta =>
                            {
                                MetaLine(meta, "Invoice Number", invoiceNo);
                                MetaLine(meta, "Invoice Date", FormatDate(paidAt));
                                MetaLine(meta, "Payment Date", FormatDateTime(paidAt));
                                MetaLine(meta, "Order Number", orderNo);
                                MetaLine(meta, "Payment Status", payment.Status);
                            });
                        });
                    });

                    header.Item().PaddingTop(12).LineHorizontal(1).LineColor(BorderColor);
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(12);

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => BillFromBox(c));
                        row.ConstantItem(12);
                        row.RelativeItem().Element(c => BillToBox(c, user));
                    });

                    col.Item().Element(c => SectionBox(c, "Plan Details", inner =>
                    {
                        inner.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2.2f);
                                cols.RelativeColumn(1.6f);
                                cols.RelativeColumn(1.1f);
                                cols.RelativeColumn(1.2f);
                                cols.RelativeColumn(0.8f);
                                cols.RelativeColumn(1.1f);
                                cols.RelativeColumn(1.2f);
                            });

                            TableHeaderCell(table, "Description");
                            TableHeaderCell(table, "Plan Name");
                            TableHeaderCell(table, "Duration");
                            TableHeaderCell(table, "Base Amount");
                            TableHeaderCell(table, "GST %");
                            TableHeaderCell(table, "GST Amount");
                            TableHeaderCell(table, "Total Amount");

                            var description = $"{plan.Name} — annual membership";
                            if (hasGst && !string.IsNullOrWhiteSpace(_settings.SacCode))
                                description += $" (SAC: {_settings.SacCode})";

                            TableBodyCell(table, description);
                            TableBodyCell(table, plan.Name);
                            TableBodyCell(table, durationLabel);
                            TableBodyCell(table, FormatInr(order.BaseAmountPaise), alignRight: true);
                            TableBodyCell(table, hasGst
                                ? $"{plan.GstPercent.ToString("0.##", CultureInfo.InvariantCulture)}%"
                                : "—");
                            TableBodyCell(table, hasGst ? FormatInr(order.GstPaise) : "—", alignRight: true);
                            TableBodyCell(table, FormatInr(order.TotalAmountPaise), alignRight: true, bold: true);
                        });
                    }));

                    if (hasGst)
                    {
                        col.Item().AlignRight().Width(260).Element(c => SectionBox(c, "GST Breakup", inner =>
                        {
                            inner.Item().Column(totals =>
                            {
                                TotalsLine(totals, "Subtotal", FormatInr(order.BaseAmountPaise));

                                if (_settings.UseIgst)
                                {
                                    TotalsLine(totals,
                                        $"IGST @ {plan.GstPercent.ToString("0.##", CultureInfo.InvariantCulture)}%",
                                        FormatInr(order.GstPaise));
                                }
                                else
                                {
                                    TotalsLine(totals,
                                        $"CGST @ {halfGstRate.ToString("0.##", CultureInfo.InvariantCulture)}%",
                                        FormatInr(cgstPaise));
                                    TotalsLine(totals,
                                        $"SGST @ {halfGstRate.ToString("0.##", CultureInfo.InvariantCulture)}%",
                                        FormatInr(sgstPaise));
                                }

                                totals.Item().PaddingVertical(4).LineHorizontal(0.5f).LineColor(BorderColor);
                                TotalsLine(totals, "Total GST", FormatInr(order.GstPaise), semiBold: true);
                                TotalsLine(totals, "Grand Total", FormatInr(order.TotalAmountPaise), bold: true, accent: true);
                            });
                        }));
                    }
                    else
                    {
                        col.Item().AlignRight().Width(220).Element(c => SectionBox(c, "Amount Summary", inner =>
                        {
                            inner.Item().Column(totals =>
                            {
                                TotalsLine(totals, "Grand Total", FormatInr(order.TotalAmountPaise), bold: true, accent: true);
                            });
                        }));
                    }

                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Element(c => SectionBox(c, "Payment Information", inner =>
                        {
                            InfoLine(inner, "Payment Status", payment.Status);
                            InfoLine(inner, "Payment Method", string.IsNullOrWhiteSpace(payment.Method) ? "—" : payment.Method);
                            InfoLine(inner, "Transaction ID", payment.RazorpayPaymentId);
                            InfoLine(inner, "Razorpay Payment ID", payment.RazorpayPaymentId);
                            InfoLine(inner, "Order ID", orderNo);
                            InfoLine(inner, "Paid On", FormatDateTime(paidAt));
                        }));

                        row.ConstantItem(12);

                        row.RelativeItem().Element(c => SectionBox(c, "Membership Information", inner =>
                        {
                            InfoLine(inner, "Plan Name", plan.Name);
                            InfoLine(inner, "Membership Type", "Annual Membership");
                            InfoLine(inner, "Valid From", FormatDate(activeFrom));
                            InfoLine(inner, "Valid Until", FormatDate(activeTo));
                            InfoLine(inner, "Membership Status", membershipStatus);
                        }));
                    });

                    if (benefitList.Count > 0)
                    {
                        col.Item().Element(c => SectionBox(c, "Benefits Included", inner =>
                        {
                            foreach (var benefit in benefitList)
                            {
                                inner.Item().PaddingBottom(3).Row(r =>
                                {
                                    r.ConstantItem(14).Text("✓").FontColor(BrandRed).SemiBold();
                                    r.RelativeItem().Text(benefit);
                                });
                            }
                        }));
                    }
                });

                page.Footer().Column(footer =>
                {
                    footer.Item().LineHorizontal(1).LineColor(BorderColor);
                    footer.Item().PaddingTop(8).AlignCenter().Column(center =>
                    {
                        center.Item().Text("For support and assistance:").FontSize(8).FontColor(TextMuted);
                        center.Item().PaddingTop(2).Text(text =>
                        {
                            text.DefaultTextStyle(x => x.FontSize(8).FontColor(TextDark));
                            if (!string.IsNullOrWhiteSpace(_settings.SupportEmail))
                                text.Span(_settings.SupportEmail).SemiBold();
                            if (!string.IsNullOrWhiteSpace(_settings.Website))
                            {
                                text.Span("  |  ");
                                text.Span(_settings.Website);
                            }
                        });
                        center.Item().PaddingTop(6).Text("Thank you for choosing MSME Bharat Manch.")
                            .FontSize(9).SemiBold().FontColor(BrandRed);
                        center.Item().PaddingTop(6).Text("This is a computer-generated invoice and does not require a physical signature.")
                            .FontSize(7).Italic().FontColor(TextMuted);
                    });
                });
            });
        }).GeneratePdf();
    }

    private byte[]? TryLoadLogo()
    {
        var path = Path.Combine(_env.ContentRootPath, "Assets", LogoFileName);
        if (!File.Exists(path)) return null;
        return File.ReadAllBytes(path);
    }

    private void BillFromBox(IContainer container)
    {
        SectionBox(container, "Bill From", inner =>
        {
            inner.Item().Text(_settings.LegalName).SemiBold();
            if (IsTaxInvoiceConfigured())
                inner.Item().Text($"GSTIN: {_settings.Gstin}");
            if (!string.IsNullOrWhiteSpace(_settings.Pan))
                inner.Item().Text($"PAN: {_settings.Pan}");
            if (!string.IsNullOrWhiteSpace(_settings.SupportEmail))
                inner.Item().Text(_settings.SupportEmail).FontColor(TextMuted);
            if (!string.IsNullOrWhiteSpace(_settings.Website))
                inner.Item().Text(_settings.Website).FontColor(TextMuted);
            if (!string.IsNullOrWhiteSpace(_settings.SupportPhone))
                inner.Item().Text(_settings.SupportPhone).FontColor(TextMuted);
            if (!string.IsNullOrWhiteSpace(_settings.Address))
                inner.Item().PaddingTop(4).Text(_settings.Address).FontColor(TextMuted);
        });
    }

    private static void BillToBox(IContainer container, User user)
    {
        SectionBox(container, "Bill To", inner =>
        {
            inner.Item().Text(user.FullName).SemiBold();
            inner.Item().Text(user.Email);
            if (!string.IsNullOrWhiteSpace(user.Phone))
                inner.Item().Text(user.Phone);
            if (!string.IsNullOrWhiteSpace(user.MemberId))
                inner.Item().Text($"Membership ID: {user.MemberId}").FontColor(TextMuted);
            inner.Item().Text($"Customer ID: {user.UserId}").FontColor(TextMuted);
        });
    }

    private bool IsTaxInvoiceConfigured() => !string.IsNullOrWhiteSpace(_settings.Gstin);

    private static void SectionBox(IContainer container, string title, Action<ColumnDescriptor> content)
    {
        container.Border(1).BorderColor(BorderColor).Background(SectionBg).Padding(10).Column(col =>
        {
            col.Item().PaddingBottom(6).Text(title).FontSize(10).Bold().FontColor(BrandRed);
            content(col);
        });
    }

    private static void MetaLine(ColumnDescriptor col, string label, string value)
    {
        col.Item().Row(row =>
        {
            row.ConstantItem(100).AlignRight().Text($"{label}:").FontSize(8).FontColor(TextMuted);
            row.ConstantItem(6);
            row.RelativeItem().AlignRight().Text(value).FontSize(8).SemiBold();
        });
    }

    private static void InfoLine(ColumnDescriptor col, string label, string value)
    {
        col.Item().PaddingBottom(3).Row(row =>
        {
            row.RelativeItem(2).Text(label).FontColor(TextMuted);
            row.RelativeItem(3).AlignRight().Text(value).SemiBold();
        });
    }

    private static void TotalsLine(
        ColumnDescriptor col,
        string label,
        string value,
        bool semiBold = false,
        bool bold = false,
        bool accent = false)
    {
        col.Item().PaddingBottom(3).Row(row =>
        {
            row.RelativeItem().Text(label).FontColor(accent ? TextDark : TextMuted);
            var text = row.RelativeItem().AlignRight().Text(value);
            if (bold) text.Bold().FontColor(accent ? BrandRed : TextDark);
            else if (semiBold) text.SemiBold();
        });
    }

    private static void TableHeaderCell(TableDescriptor table, string text)
    {
        table.Cell().Background(TableHeaderBg).BorderBottom(1).BorderColor(BorderColor)
            .PaddingVertical(6).PaddingHorizontal(4)
            .Text(text).FontSize(8).SemiBold();
    }

    private static void TableBodyCell(
        TableDescriptor table,
        string text,
        bool alignRight = false,
        bool bold = false)
    {
        var cell = table.Cell().BorderBottom(1).BorderColor(BorderColor)
            .PaddingVertical(6).PaddingHorizontal(4);
        var item = alignRight ? cell.AlignRight().Text(text) : cell.Text(text);
        if (bold) item.SemiBold();
    }

    private static string FormatDuration(int days)
        => days switch
        {
            365 => "1 Year",
            366 => "1 Year",
            _ => $"{days} Days",
        };

    private static string FormatInr(long paise)
        => (paise / 100m).ToString("N2", CultureInfo.GetCultureInfo("en-IN"));
}
