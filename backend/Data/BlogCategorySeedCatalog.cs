namespace RB_Website_API.Data;

public static class BlogCategorySeedCatalog
{
    public sealed record BlogCategorySeedEntry(
        string Slug,
        string Label,
        int SortOrder,
        bool ShowInFilter
    );

    public static readonly IReadOnlyList<BlogCategorySeedEntry> Categories =
    [
        new("blog", "Blog", 1, true),
        new("news", "News", 2, true),
        new("success", "Success Stories", 3, true),
    ];
}
