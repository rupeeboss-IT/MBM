namespace RB_Website_API.Auth;

/// <summary>
/// Counts for static website content (mirrors frontend data files).
/// Update when events or schemes are added in the Angular app.
/// Blogs are now stored in the database — count is derived dynamically.
/// </summary>
public static class ContentCatalog
{
    public const int Events = 6;
    public const int Schemes = 6;
}
