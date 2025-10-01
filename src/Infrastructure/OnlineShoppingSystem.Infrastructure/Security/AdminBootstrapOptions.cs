namespace OnlineShoppingSystem.Infrastructure.Security;

public class AdminBootstrapOptions
{
    public bool Enabled { get; set; } = true;
    public List<string> AllowedIPs { get; set; } = new();
    public int RateLimitPerMinute { get; set; } = 5;
    public string? Token { get; set; }
    public int MaxUses { get; set; } = 1;
}
