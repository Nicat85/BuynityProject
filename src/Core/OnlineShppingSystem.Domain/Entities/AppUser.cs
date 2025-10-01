using Microsoft.AspNetCore.Identity;

namespace OnlineShppingSystem.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
     public string? FullName { get; set; }
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public DateTime? LastLoginDate { get; set; }
    public string? ProfilePicture { get; set; }
    public string? AvatarText { get; set; }
    public string? Bio { get; set; }
    public string? Address { get; set; }

    public string? FinCode { get; set; } 

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Review> Reviews { get; set; } = new List<Review>();
    public ICollection<Favorite> Favorites { get; set; } = new List<Favorite>();
    public ICollection<IdentityUserRole<Guid>> UserRoles { get; set; } = new List<IdentityUserRole<Guid>>();
    public ICollection<CourierAssignment> CourierAssignments { get; set; } = new List<CourierAssignment>();
}

