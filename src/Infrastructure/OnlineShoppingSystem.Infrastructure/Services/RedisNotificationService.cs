using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Notifications;
using OnlineSohppingSystem.Application.Shared.Helpers;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineShppingSystem.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore; 

public class RedisNotificationService : IRedisNotificationService
{
    private readonly IDistributedCache _redis;
    private readonly RedisSettings _redisSettings;
    private readonly UserManager<AppUser> _userManager;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisNotificationService(
        IDistributedCache redis,
        IOptions<RedisSettings> opts,
        UserManager<AppUser> userManager)
    {
        _redis = redis;
        _redisSettings = opts.Value;
        _userManager = userManager;
    }

    private DistributedCacheEntryOptions EntryOptions() => new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.NotificationTtlMinutes)
    };

    private static void Log(string msg) =>
        Console.WriteLine($"[Notif][{DateTime.UtcNow:O}] {msg}");

    public async Task<NotificationDto> CreateNotificationAsync(Guid userId, NotificationDto incoming, CancellationToken ct = default)
    {
        var key = RedisKeyHelper.GetNotificationKey(userId);

        string? senderName = incoming.SenderName;
        string? senderAvatarUrl = incoming.SenderAvatarUrl;

        if (incoming.SenderId != Guid.Empty)
        {
           
            var sender = await _userManager.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == incoming.SenderId, ct);

            if (sender != null)
            {
                senderName = string.IsNullOrWhiteSpace(sender.FullName)
                    ? sender.UserName
                    : sender.FullName;

                senderAvatarUrl = !string.IsNullOrWhiteSpace(sender.ProfilePicture)
                    ? sender.ProfilePicture
                    : "/images/avatars/default.png";
            }
        }

        var notif = new NotificationDto
        {
            Id = incoming.Id == Guid.Empty ? Guid.NewGuid() : incoming.Id,
            Title = string.IsNullOrWhiteSpace(incoming.Title) ? "Bildiriş" : incoming.Title,
            Message = incoming.Message ?? string.Empty,
            Type = string.IsNullOrWhiteSpace(incoming.Type) ? "info" : incoming.Type,
            IsRead = false,
            CreatedAt = incoming.CreatedAt == default ? DateTimeOffset.UtcNow : incoming.CreatedAt,
            Link = incoming.Link,

            SenderId = incoming.SenderId,
            SenderName = senderName,
            SenderAvatarUrl = senderAvatarUrl
        };

        var list = await ReadListAsync(key, ct);
        list.Insert(0, notif);

        if (list.Count > 100)
            list.RemoveRange(100, list.Count - 100);

        await WriteListAsync(key, list, ct);
        return notif;
    }

    public async Task<List<NotificationDto>> GetUserNotificationsAsync(Guid userId, CancellationToken ct = default)
    {
        var key = RedisKeyHelper.GetNotificationKey(userId);
        Log($"Get user={userId} key={key}");
        return await ReadListAsync(key, ct);
    }

    public async Task<bool> MarkAsReadAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var key = RedisKeyHelper.GetNotificationKey(userId);
        Log($"MarkAsRead user={userId} id={notificationId} key={key}");

        var list = await ReadListAsync(key, ct);
        var n = list.FirstOrDefault(x => x.Id == notificationId);
        if (n is null) return false;

        n.IsRead = true;
        await WriteListAsync(key, list, ct);
        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid userId, CancellationToken ct = default)
    {
        var key = RedisKeyHelper.GetNotificationKey(userId);
        Log($"MarkAllAsRead user={userId} key={key}");

        var list = await ReadListAsync(key, ct);
        if (list.Count == 0) return 0;

        var changed = 0;
        foreach (var n in list)
        {
            if (!n.IsRead) { n.IsRead = true; changed++; }
        }
        if (changed > 0)
            await WriteListAsync(key, list, ct);

        return changed;
    }

    public async Task<bool> DeleteAsync(Guid userId, Guid notificationId, CancellationToken ct = default)
    {
        var key = RedisKeyHelper.GetNotificationKey(userId);
        Log($"Delete user={userId} id={notificationId} key={key}");

        var list = await ReadListAsync(key, ct);
        var before = list.Count;
        list = list.Where(n => n.Id != notificationId).ToList();

        if (list.Count == before) return false;

        await WriteListAsync(key, list, ct);
        return true;
    }

    public async Task<int> DeleteAllAsync(Guid userId, CancellationToken ct = default)
    {
        var key = RedisKeyHelper.GetNotificationKey(userId);
        Log($"DeleteAll user={userId} key={key}");

        var list = await ReadListAsync(key, ct);
        var count = list.Count;

        await _redis.RemoveAsync(key, ct);
        return count;
    }

    private async Task<List<NotificationDto>> ReadListAsync(string key, CancellationToken ct)
    {
        var json = await _redis.GetStringAsync(key, ct);
        return string.IsNullOrEmpty(json)
            ? new List<NotificationDto>()
            : (JsonSerializer.Deserialize<List<NotificationDto>>(json, _json) ?? new List<NotificationDto>());
    }

    private async Task WriteListAsync(string key, List<NotificationDto> list, CancellationToken ct)
    {
        var updated = JsonSerializer.Serialize(list, _json);
        await _redis.SetStringAsync(key, updated, EntryOptions(), ct);
    }
}
