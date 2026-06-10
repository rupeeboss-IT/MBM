namespace RB_Website_API.Middleware;

/// <summary>
/// Permanent (301) redirects for legacy indexed URLs and renamed routes.
/// Runs before static files so crawlers receive real redirects instead of SPA soft-404s.
/// </summary>
public sealed class LegacyUrlRedirectMiddleware
{
    private static readonly Dictionary<string, string> Redirects = new(StringComparer.OrdinalIgnoreCase)
    {
        ["/about-us"] = "/about",
        ["/the-board"] = "/about",
        ["/our-team"] = "/about",
        ["/our-partners"] = "/about",
        ["/our-mission"] = "/about",
        ["/finance"] = "/loans",
        ["/contact-us"] = "/contact",
        ["/services"] = "/our-services",
        ["/partners"] = "/about",
        ["/privacy"] = "/privacy-policy",
        ["/blog"] = "/news",
        ["/news-blog"] = "/news",
        ["/member/login"] = "/login",
        ["/service/human-resources"] = "/service/hr-recruitment",
        ["/service/accounting-virtual-cfo"] = "/service/accounting-cfo",
    };

    private readonly RequestDelegate _next;

    public LegacyUrlRedirectMiddleware(RequestDelegate next) => _next = next;

    public Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
            return _next(context);

        if (context.Request.Path.StartsWithSegments("/api"))
            return _next(context);

        var host = context.Request.Host.Host;
        if (host.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
        {
            var canonicalHost = host[4..];
            var target = $"{context.Request.Scheme}://{canonicalHost}{context.Request.Path}{context.Request.QueryString}";
            context.Response.Redirect(target, permanent: true);
            return Task.CompletedTask;
        }

        var path = NormalizePath(context.Request.Path.Value);
        if (Redirects.TryGetValue(path, out var destination))
        {
            context.Response.Redirect(destination + context.Request.QueryString, permanent: true);
            return Task.CompletedTask;
        }

        return _next(context);
    }

    private static string NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return "/";

        var trimmed = path.TrimEnd('/');
        return trimmed.Length == 0 ? "/" : trimmed;
    }
}

public static class LegacyUrlRedirectMiddlewareExtensions
{
    public static IApplicationBuilder UseLegacyUrlRedirects(this IApplicationBuilder app)
        => app.UseMiddleware<LegacyUrlRedirectMiddleware>();
}
