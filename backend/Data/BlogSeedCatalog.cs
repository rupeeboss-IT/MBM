namespace RB_Website_API.Data;

/// <summary>
/// Canonical seed metadata for the three legacy static articles.
/// Full HTML bodies live in Data/BlogContent/{slug}.html.
/// </summary>
public static class BlogSeedCatalog
{
    public sealed record BlogSeedEntry(
        string Slug,
        string Title,
        string Crumb,
        string Meta,
        string Category,
        string DateLabel,
        string Summary,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        string? ImageUrl,
        string? SeoTitle,
        string? MetaDescription
    );

    public static readonly IReadOnlyList<BlogSeedEntry> Articles =
    [
        new(
            Slug: "biggest-msme-challenges-2026",
            Title: "Biggest Challenges MSMEs Face in India Today",
            Crumb: "MSME Challenges 2026",
            Meta: "Blog · MSME Challenges 2026 · 15 min read",
            Category: "blog",
            DateLabel: "Challenges 2026",
            Summary: "Top MSME challenges in 2026: loans, cash flow, delayed payments, competition, digital gap, compliance, manpower, marketing, and practical solutions.",
            BadgeText: "MSME",
            BadgeClass: "badge badge-green",
            CardIcon: "⚠️",
            CardClass: "news-img cat-blog",
            ImageUrl: "/BlogImg/blog4.png",
            SeoTitle: "Biggest Challenges MSMEs Face in India Today | Problems & Solutions for MSMEs in 2026",
            MetaDescription: "Discover the biggest challenges faced by MSMEs in India including loan access, cash flow issues, delayed payments, competition, compliance burden, digital transformation, and skilled manpower shortages in 2026."
        ),
        new(
            Slug: "latest-msme-schemes-2026",
            Title: "Latest MSME Schemes Every Business Owner Must Apply For in 2026",
            Crumb: "MSME Schemes 2026",
            Meta: "Blog · MSME Schemes 2026 · 14 min read",
            Category: "blog",
            DateLabel: "Schemes 2026",
            Summary: "Complete 2026 guide to CGTMSE, Mudra, PMEGP, TReDS, ZED, Stand-Up India, GeM, SME Growth Fund, and more — eligibility and benefits for MSMEs.",
            BadgeText: "MSME",
            BadgeClass: "badge badge-green",
            CardIcon: "📋",
            CardClass: "news-img cat-blog",
            ImageUrl: "/BlogImg/blog3.png",
            SeoTitle: "Latest MSME Schemes 2026: Best Government Schemes & Benefits for Small Businesses",
            MetaDescription: "Discover the latest MSME schemes in India for 2026 including Mudra Loan, CGTMSE, PMEGP, TReDS, ZED Certification, Stand-Up India, and export incentives. Learn eligibility, benefits, and how MSMEs can apply."
        ),
        new(
            Slug: "union-budget-2026-msme",
            Title: "Union Budget 2026: What MSMEs Should Know",
            Crumb: "Union Budget 2026",
            Meta: "Blog · Union Budget 2026 · 12 min read",
            Category: "blog",
            DateLabel: "Budget 2026",
            Summary: "Key Union Budget 2026 announcements for MSMEs: SME Growth Fund, easier loans, GST reforms, TReDS, export support, AI adoption, and growth opportunities.",
            BadgeText: "MSME",
            BadgeClass: "badge badge-green",
            CardIcon: "📊",
            CardClass: "news-img cat-blog",
            ImageUrl: "/BlogImg/blog2.png",
            SeoTitle: "Union Budget 2026 for MSMEs: Key Highlights, Benefits & Business Opportunities",
            MetaDescription: "Explore the major announcements of Union Budget 2026 for MSMEs including SME Growth Fund, easier loans, GST simplification, TReDS reforms, export support, AI adoption, and business growth opportunities for Indian MSMEs."
        ),
    ];

    public static bool IsPlaceholderContent(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return true;
        var trimmed = content.Trim();
        if (trimmed.StartsWith('`')) return true;
        if (trimmed.Length < 400) return true;
        return trimmed.Contains("Please update the content through the admin panel", StringComparison.OrdinalIgnoreCase);
    }
}
