namespace OnlineSohppingSystem.Application.Shared.Helpers;

public static class RedisKeyHelper
{
    public static string GetNotificationKey(Guid userId) => $"notifications:{userId}";
}
