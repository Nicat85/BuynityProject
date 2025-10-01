using System.Reflection;

namespace OnlineShppingSystem.Application.Shared.Helpers;

public static class PermissionsHelper
{
    public static List<string> GetAllPermissions()
    {
        var allPermissions = new List<string>();

        var nestedTypes = typeof(OnlineShppingSystem.Application.Shared.Settings.Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var nestedType in nestedTypes)
        {
            var allField = nestedType.GetField("All", BindingFlags.Public | BindingFlags.Static);
            if (allField != null)
            {
                var permissions = allField.GetValue(null) as List<string>;
                if (permissions != null)
                {
                    allPermissions.AddRange(permissions);
                }
            }
        }

        return allPermissions;
    }
}
