namespace RB_Website_API.Data;

public static class BlogBadgeSeedCatalog
{
    public sealed record BlogBadgeSeedEntry(
        string Slug,
        string Label,
        string BadgeText,
        string BadgeClass,
        string CardIcon,
        string CardClass,
        int SortOrder
    );

    public static readonly IReadOnlyList<BlogBadgeSeedEntry> Badges =
    [
        new("msme-green", "MSME (green)", "MSME", "badge badge-green", "📰", "news-img cat-blog", 1),
        new("msme-blue", "MSME (blue)", "MSME", "badge badge-blue", "📢", "news-img cat-news", 2),
        new("msme-amber", "MSME (amber)", "MSME", "badge badge-amber", "🌟", "news-img cat-success", 3),
    ];
}
