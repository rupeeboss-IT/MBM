namespace RB_Website_API.Auth;

public sealed class AdminSeedSettings
{
    public const string SectionName = "AdminSeed";

    public bool Enabled { get; set; } = true;

    public string FullName { get; set; } = "Super Admin";

    public string Email { get; set; } = "";

    public string Phone { get; set; } = "";

    public string Password { get; set; } = "";
}

