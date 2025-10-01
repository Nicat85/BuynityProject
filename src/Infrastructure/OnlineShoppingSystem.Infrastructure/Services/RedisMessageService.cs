using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.MessageDto;
using OnlineSohppingSystem.Application.DTOs.Notifications;
using OnlineSohppingSystem.Application.Shared.Settings;
using OnlineSohppingSystem.Domain.Enums;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Infrastructure.Services
{
    public class RedisMessageService : IRedisMessageService
    {
        private readonly IDistributedCache _redis;
        private readonly RedisSettings _redisSettings;
        private readonly IRedisNotificationService _notificationService;
        private readonly UserManager<AppUser> _userManager;                     

        private const string MessageKeyPrefix = "messages:";
        private const string DupeKeyPrefix = "msg:dupe:";
        private const string DupeCacheKeyPrefix = "msg:dupe:resp:";

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public RedisMessageService(
            IDistributedCache redis,
            IOptions<RedisSettings> options,
            IRedisNotificationService notificationService,
            UserManager<AppUser> userManager)                                
        {
            _redis = redis;
            _redisSettings = options.Value;
            _notificationService = notificationService;
            _userManager = userManager;                                        
        }

        private static string ChatKey(Guid a, Guid b)
        {
            var (x, y) = a.CompareTo(b) < 0 ? (a, b) : (b, a);
            return $"{MessageKeyPrefix}{x}:{y}";
        }

        private static string DupeKey(Guid senderId, Guid clientMessageId) =>
            $"{DupeKeyPrefix}{senderId}:{clientMessageId}";

        private static string DupeRespKey(Guid senderId, Guid clientMessageId) =>
            $"{DupeCacheKeyPrefix}{senderId}:{clientMessageId}";

        public async Task<MessageResponse> SendAsync(Guid senderId, SendMessageRequest req, CancellationToken ct = default)
        {
            if (senderId == Guid.Empty) throw new ArgumentException("senderId boş ola bilməz.");
            if (req.ReceiverId == Guid.Empty) throw new ArgumentException("ReceiverId boş ola bilməz.");
            if (string.IsNullOrWhiteSpace(req.Content)) throw new ArgumentException("Content boş ola bilməz.");
            if (senderId == req.ReceiverId) throw new ArgumentException("Özünə mesaj göndərə bilməzsən.");

            var effectiveClientMessageId = req.ClientMessageId ?? Guid.NewGuid();

            var dupeKey = DupeKey(senderId, effectiveClientMessageId);
            var dupeRespKey = DupeRespKey(senderId, effectiveClientMessageId);

            var already = await _redis.GetStringAsync(dupeKey, ct);
            if (!string.IsNullOrEmpty(already))
            {
                var cachedResp = await _redis.GetStringAsync(dupeRespKey, ct);
                if (!string.IsNullOrEmpty(cachedResp))
                {
                    var parsed = JsonSerializer.Deserialize<MessageResponse>(cachedResp, _json);
                    if (parsed is not null) return parsed;
                }
                throw new InvalidOperationException("Duplicate clientMessageId.");
            }

            var key = ChatKey(senderId, req.ReceiverId);

            var existingJson = await _redis.GetStringAsync(key, ct);
            var list = string.IsNullOrEmpty(existingJson)
                ? new List<MessageDto>()
                : (JsonSerializer.Deserialize<List<MessageDto>>(existingJson, _json) ?? new());

            var stored = new MessageDto
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = req.ReceiverId,
                Content = req.Content,
                SentAt = DateTime.UtcNow,
                IsRead = false,
                DeletedForUserIds = new List<Guid>()
            };

            list.Add(stored);

            var updatedJson = JsonSerializer.Serialize(list, _json);
            await _redis.SetStringAsync(key, updatedJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.MessageTtlMinutes)
            }, ct);

            
            var dupeTtl = TimeSpan.FromDays(1);
            await _redis.SetStringAsync(dupeKey, "1", new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = dupeTtl
            }, ct);

           
            string? senderName = null;
            string? senderAvatarUrl = null;
            var sender = await _userManager.FindByIdAsync(senderId.ToString());
            if (sender != null)
            {
                senderName = string.IsNullOrWhiteSpace(sender.FullName) ? sender.UserName : sender.FullName;
                senderAvatarUrl = !string.IsNullOrWhiteSpace(sender.ProfilePicture)
                    ? sender.ProfilePicture
                    : "/images/avatars/default.png";
            }

            var response = new MessageResponse
            {
                Id = stored.Id,
                SenderId = stored.SenderId,
                ReceiverId = stored.ReceiverId,
                Content = stored.Content,
                SentAt = new DateTimeOffset(stored.SentAt, TimeSpan.Zero),
                IsRead = stored.IsRead,

                
                SenderName = senderName,
                SenderAvatarUrl = senderAvatarUrl
            };
            await _redis.SetStringAsync(dupeRespKey, JsonSerializer.Serialize(response, _json), new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = dupeTtl
            }, ct);

           
            await _notificationService.CreateNotificationAsync(req.ReceiverId, new NotificationDto
            {
                Title = "Yeni mesaj",
                Message = req.Content,
                Type = NotificationType.Message.ToString(),
                CreatedAt = DateTimeOffset.UtcNow,
                SenderId = senderId,
                SenderName = senderName,
                SenderAvatarUrl = senderAvatarUrl
            }, ct);

            return response;
        }

        public async Task<IReadOnlyList<MessageResponse>> GetConversationAsync(
            Guid userA,
            Guid userB,
            int take = 50,
            DateTimeOffset? before = null,
            CancellationToken ct = default)
        {
            var key = ChatKey(userA, userB);
            var json = await _redis.GetStringAsync(key, ct);

            var list = string.IsNullOrEmpty(json)
                ? new List<MessageDto>()
                : (JsonSerializer.Deserialize<List<MessageDto>>(json, _json) ?? new());

            var query = list.Where(m => !m.DeletedForUserIds.Contains(userA));

            var ordered = query.OrderByDescending(m => m.SentAt);
            if (before != null)
                ordered = ordered.Where(m => new DateTimeOffset(m.SentAt, TimeSpan.Zero) < before.Value)
                                 .OrderByDescending(m => m.SentAt);

            var pageList = ordered.Take(Math.Max(1, take)).OrderBy(m => m.SentAt).ToList();

           
            var userIds = pageList.Select(m => m.SenderId).Distinct().ToList();
            var userMap = new Dictionary<Guid, (string? name, string? avatar)>();
            foreach (var id in userIds)
            {
                var u = await _userManager.FindByIdAsync(id.ToString());
                var name = u == null ? null : (string.IsNullOrWhiteSpace(u.FullName) ? u.UserName : u.FullName);
                var avatar = u == null ? null : (!string.IsNullOrWhiteSpace(u.ProfilePicture) ? u.ProfilePicture : "/images/avatars/default.png");
                userMap[id] = (name, avatar);
            }

            var page = pageList
                .Select(m => new MessageResponse
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    ReceiverId = m.ReceiverId,
                    Content = m.Content,
                    SentAt = new DateTimeOffset(m.SentAt, TimeSpan.Zero),
                    IsRead = m.IsRead,
                    SenderName = userMap.TryGetValue(m.SenderId, out var meta) ? meta.name : null,
                    SenderAvatarUrl = userMap.TryGetValue(m.SenderId, out meta) ? meta.avatar : null
                })
                .ToList();

            return page;
        }

        public async Task MarkMessagesAsReadAsync(Guid senderId, Guid receiverId)
        {
            var key = ChatKey(senderId, receiverId);
            var json = await _redis.GetStringAsync(key);
            if (string.IsNullOrEmpty(json)) return;

            var list = JsonSerializer.Deserialize<List<MessageDto>>(json, _json);
            if (list == null) return;

            foreach (var m in list)
            {
                if (m.ReceiverId == senderId)
                    m.IsRead = true;
            }

            var updatedJson = JsonSerializer.Serialize(list, _json);
            await _redis.SetStringAsync(key, updatedJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.MessageTtlMinutes)
            });
        }

        public async Task DeleteMessageForMeAsync(Guid userId, Guid otherUserId, Guid messageId)
        {
            var key = ChatKey(userId, otherUserId);
            var json = await _redis.GetStringAsync(key);
            if (string.IsNullOrEmpty(json)) return;

            var list = JsonSerializer.Deserialize<List<MessageDto>>(json, _json);
            if (list == null) return;

            var target = list.FirstOrDefault(m => m.Id == messageId);
            if (target == null) return;

            if (!target.DeletedForUserIds.Contains(userId))
                target.DeletedForUserIds.Add(userId);

            var updatedJson = JsonSerializer.Serialize(list, _json);
            await _redis.SetStringAsync(key, updatedJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.MessageTtlMinutes)
            });
        }

        public async Task DeleteMessageForBothAsync(Guid user1Id, Guid user2Id, Guid messageId)
        {
            var key = ChatKey(user1Id, user2Id);
            var json = await _redis.GetStringAsync(key);
            if (string.IsNullOrEmpty(json)) return;

            var list = JsonSerializer.Deserialize<List<MessageDto>>(json, _json);
            if (list == null) return;

            list = list.Where(m => m.Id != messageId).ToList();

            var updatedJson = JsonSerializer.Serialize(list, _json);
            await _redis.SetStringAsync(key, updatedJson, new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(_redisSettings.MessageTtlMinutes)
            });
        }

        [Obsolete("Use SendAsync(senderId, SendMessageRequest) instead.")]
        public async Task SendMessageAsync(MessageDto message)
        {
            var req = new SendMessageRequest { ReceiverId = message.ReceiverId, Content = message.Content };
            await SendAsync(message.SenderId, req);
        }

        [Obsolete("Use GetConversationAsync(userA, userB, take, before) instead.")]
        public async Task<List<MessageDto>> GetConversationAsync(Guid user1Id, Guid user2Id)
        {
            var list = await GetConversationAsync(user1Id, user2Id, 200);
            return list.Select(m => new MessageDto
            {
                Id = m.Id,
                SenderId = m.SenderId,
                ReceiverId = m.ReceiverId,
                Content = m.Content,
                SentAt = m.SentAt.UtcDateTime,
                IsRead = m.IsRead,
                DeletedForUserIds = new List<Guid>()
            }).ToList();
        }
    }
}
