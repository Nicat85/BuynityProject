namespace OnlineSohppingSystem.Application.Shared.Settings;

public sealed class IdentitySeedSettings
{
    
    public bool Enabled { get; set; } = true;
    public bool SeedRoles { get; set; } = true;
    public string[] Roles { get; set; } = new[] { "Buyer", "Seller", "Moderator", "StoreSeller" };
    public bool SeedAdmin { get; set; } = false;
    public string? BootstrapToken { get; set; }
}
