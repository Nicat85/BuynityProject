namespace OnlineShppingSystem.Application.Shared.Helpers;

public static class AvatarHelper
{
    public static string GenerateAvatarText(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "NN";

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
            return parts[0].Substring(0, 1).ToUpper();

        return string.Concat(parts[0][0], parts[1][0]).ToUpper();
    }
}
