namespace RB_Website_API.Auth;

public sealed class ConnectSettings
{
    public const string SectionName = "ConnectSettings";

    /// <summary>When false, Connect APIs return 503 and public page can fall back gracefully.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Lifetime contact unlock limit for basic plan.</summary>
    public int BasicStandardContactLimit { get; set; } = 5;
}

public enum ConnectAccessTier
{
    None,
    Limited,
    Unlimited,
}

public static class ConnectAccessRules
{
    public static ConnectAccessTier GetTier(string? planCode)
    {
        var code = (planCode ?? "").Trim().ToLowerInvariant();
        if (code is "premium" or "pro") return ConnectAccessTier.Unlimited;
        if (code is "basic") return ConnectAccessTier.Limited;
        return ConnectAccessTier.None;
    }
}
