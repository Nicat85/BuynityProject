using OnlineShppingSystem.Application.Shared.Settings;

namespace OnlineShppingSystem.Application.Authorization;

public static class RoleTemplates
{
    public const string Admin = "Admin";
    public const string Seller = "Seller";
    public const string Buyer = "Buyer";
    public const string Moderator = "Moderator";
    public const string StoreSeller = "StoreSeller";

    public static readonly IReadOnlyDictionary<string, IReadOnlyCollection<string>> ByRole
        = new Dictionary<string, IReadOnlyCollection<string>>(StringComparer.OrdinalIgnoreCase)
        {
            [Buyer] = new[]
            {
                Permissions.Products.ReadById, Permissions.Products.ReadMy,
                Permissions.Favorites.Read, Permissions.Favorites.Create, Permissions.Favorites.Delete,
                Permissions.Orders.Create, Permissions.Orders.ReadMy, Permissions.Orders.ReadById,
                Permissions.Review.Create, Permissions.Review.Update, Permissions.Review.Delete, Permissions.Review.ReadMy,
                Permissions.Users.ReadProfile, Permissions.Users.UpdateProfile, Permissions.Users.UploadProfilePicture
            },

            [Seller] = new[]
            {
                Permissions.Products.Create, Permissions.Products.Update, Permissions.Products.Delete, Permissions.Products.Restore,
                Permissions.Products.CreateStore, Permissions.Products.CreateSecondHand, Permissions.Products.ReadMy, Permissions.Products.ReadById,
                Permissions.Orders.ReadById,
                Permissions.Users.ReadProfile, Permissions.Users.UpdateProfile, Permissions.Users.UploadProfilePicture
            },

            [Moderator] = new[]
            {
                Permissions.Categories.Create, Permissions.Categories.Update, Permissions.Categories.Delete, Permissions.Categories.Restore,
                Permissions.Products.Restore, Permissions.Products.Delete, Permissions.Products.Update, Permissions.Products.ReadById,
                Permissions.Messages.Read, Permissions.Messages.Delete, Permissions.Messages.MarkRead,
                Permissions.Notifications.Read, Permissions.Notifications.Delete, Permissions.Notifications.DeleteAll
            },

            
            [Admin] = Permissions.GetAll(),

            
            [StoreSeller] = new[]
            {
                Permissions.Products.CreateStore,
                Permissions.Products.CreateSecondHand,
                Permissions.Products.Create, Permissions.Products.Update, Permissions.Products.Delete, Permissions.Products.Restore,
                Permissions.Products.ReadMy, Permissions.Products.ReadById,
                Permissions.Orders.ReadById,
                Permissions.Users.ReadProfile, Permissions.Users.UpdateProfile, Permissions.Users.UploadProfilePicture
            }
        };
}
