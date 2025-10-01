using System.Collections.Generic;
using System.Linq;

namespace OnlineShppingSystem.Application.Shared.Settings
{
    
    public static class Permissions
    {
        
        public const string ClaimType = "Permission";

        

        public static class Categories
        {
            public const string Create = "Permissions.Categories.Create";
            public const string Update = "Permissions.Categories.Update";
            public const string Delete = "Permissions.Categories.Delete";
            public const string Restore = "Permissions.Categories.Restore";

            public static readonly IReadOnlyList<string> All = new[] { Create, Update, Delete, Restore };
        }

        public static class Roles
        {
            public const string Read = "Permissions.Roles.Read";
            public const string Create = "Permissions.Roles.Create";
            public const string Update = "Permissions.Roles.Update";
            public const string Delete = "Permissions.Roles.Delete";
            public const string UpdatePermissions = "Permissions.Roles.UpdatePermissions";
            public const string AssignToUser = "Permissions.Roles.AssignToUser";

            public static readonly IReadOnlyList<string> All =
                new[] { Read, Create, Update, Delete, UpdatePermissions, AssignToUser };
        }

        public static class Users
        {
            public const string ReadProfile = "Permissions.Users.ReadProfile";
            public const string UpdateProfile = "Permissions.Users.UpdateProfile";
            public const string ReadAll = "Permissions.Users.ReadAll";
            public const string UploadProfilePicture = "Permissions.Users.UploadProfilePicture";

            public static readonly IReadOnlyList<string> All =
                new[] { ReadProfile, UpdateProfile, ReadAll, UploadProfilePicture };
        }

        public static class Products
        {
            public const string Create = "Permissions.Products.Create";
            public const string Update = "Permissions.Products.Update";
            public const string Delete = "Permissions.Products.Delete";
            public const string Restore = "Permissions.Products.Restore";
            public const string ReadMy = "Permissions.Products.ReadMy";
            public const string ReadById = "Permissions.Products.ReadById";
            public const string CreateStore = "Permissions.Products.CreateStore";
            public const string CreateSecondHand = "Permissions.Products.CreateSecondHand";

            public static readonly IReadOnlyList<string> All =
                new[] { Create, Update, Delete, Restore, ReadMy, ReadById, CreateStore, CreateSecondHand };
        }

        public static class Favorites
        {
            public const string Read = "Permissions.Favorites.Read";
            public const string Create = "Permissions.Favorites.Create";
            public const string Delete = "Permissions.Favorites.Delete";

            public static readonly IReadOnlyList<string> All = new[] { Read, Create, Delete };
        }

        public static class Follows
        {
            public const string Create = "Permissions.Follows.Create";
            public const string Delete = "Permissions.Follows.Delete";

            public static readonly IReadOnlyList<string> All = new[] { Create, Delete };
        }

        public static class Messages
        {
            public const string Read = "Permissions.Messages.Read";
            public const string Send = "Permissions.Messages.Send";
            public const string Delete = "Permissions.Messages.Delete";
            public const string MarkRead = "Permissions.Messages.MarkRead";

            public static readonly IReadOnlyList<string> All = new[] { Read, Send, Delete, MarkRead };
        }

        public static class Notifications
        {
            public const string Read = "Permissions.Notifications.Read";
            public const string Create = "Permissions.Notifications.Create";
            public const string MarkRead = "Permissions.Notifications.MarkRead";
            public const string MarkAllRead = "Permissions.Notifications.MarkAllRead";
            public const string Delete = "Permissions.Notifications.Delete";
            public const string DeleteAll = "Permissions.Notifications.DeleteAll";

            public static readonly IReadOnlyList<string> All =
                new[] { Read, Create, MarkRead, MarkAllRead, Delete, DeleteAll };
        }

        public static class Payments
        {
            public const string Create = "Permissions.Payments.Create";
            public const string Refund = "Permissions.Payments.Refund";

            public static readonly IReadOnlyList<string> All = new[] { Create, Refund };
        }

        public static class PhoneVerification
        {
            public const string Send = "Permissions.PhoneVerification.Send";
            public const string Verify = "Permissions.PhoneVerification.Verify";

            public static readonly IReadOnlyList<string> All = new[] { Send, Verify };
        }

        public static class Auth
        {
            public const string ChangePassword = "Permissions.Auth.ChangePassword";
            public const string SetPassword = "Permissions.Auth.SetPassword";
            public const string Logout = "Permissions.Auth.Logout";
            public const string DeleteAccount = "Permissions.Auth.DeleteAccount";

            public static readonly IReadOnlyList<string> All =
                new[] { ChangePassword, SetPassword, Logout, DeleteAccount };
        }

        public static class Review
        {
            public const string Create = "Permissions.Review.Create";
            public const string Update = "Permissions.Review.Update";
            public const string Delete = "Permissions.Review.Delete";
            public const string ReadMy = "Permissions.Review.ReadMy";
            public const string FullAccess = "Permissions.Review.FullAccess";

            public static readonly IReadOnlyList<string> All =
                new[] { Create, Update, Delete, ReadMy, FullAccess };
        }

        public static class Orders
        {
            public const string Create = "Permissions.Orders.Create";
            public const string ReadMy = "Permissions.Orders.ReadMy";
            public const string ReadById = "Permissions.Orders.ReadById";

            public static readonly IReadOnlyList<string> All = new[] { Create, ReadMy, ReadById };
        }

        public static class SupportChat
        {
            
            public const string Read = "Permissions.SupportChat.Read";
            public const string Write = "Permissions.SupportChat.Write";
            public const string Assign = "Permissions.SupportChat.Assign";
            public const string ChangeStatus = "Permissions.SupportChat.ChangeStatus";
            public const string Close = "Permissions.SupportChat.Close";

            
            public const string CreateThread = "Permissions.SupportChat.CreateThread";
            public const string ReadMyThreads = "Permissions.SupportChat.ReadMyThreads";
            public const string ReadMessages = "Permissions.SupportChat.ReadMessages";
            public const string SendMessage = "Permissions.SupportChat.SendMessage";

            public static readonly IReadOnlyList<string> All =
                new[]
                {
            Read, Write, Assign, ChangeStatus, Close,
            CreateThread, ReadMyThreads, ReadMessages, SendMessage
                };
        }

        public static class Couriers
        {
            public const string ReadMy = "Permissions.Couriers.ReadMy";
            public const string Take = "Permissions.Couriers.Take";              
            public const string UpdateOrderStatus = "Permissions.Couriers.UpdateOrderStatus";

            public static readonly IReadOnlyList<string> All = new[] { ReadMy, Take, UpdateOrderStatus };
        }



        public static List<string> GetAll()
        {
            return Enumerable.Empty<string>()
                .Concat(Categories.All)
                .Concat(Roles.All)
                .Concat(Users.All)
                .Concat(Products.All)
                .Concat(Favorites.All)
                .Concat(Follows.All)
                .Concat(Messages.All)
                .Concat(Notifications.All)
                .Concat(Payments.All)
                .Concat(PhoneVerification.All)
                .Concat(Auth.All)
                .Concat(Review.All)
                .Concat(Orders.All)
                .Concat(SupportChat.All)
                .Concat(Couriers.All) 
                .Distinct(System.StringComparer.Ordinal)
                .OrderBy(x => x, System.StringComparer.Ordinal)
                .ToList();
        }



        public static HashSet<string> GetAllAsHashSet()
            => new HashSet<string>(GetAll(), System.StringComparer.Ordinal);

       
        public static bool Exists(string permission)
            => GetAllAsHashSet().Contains(permission);
    }
}
