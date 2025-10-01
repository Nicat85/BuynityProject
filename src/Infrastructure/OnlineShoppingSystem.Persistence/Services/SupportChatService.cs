using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineSohppingSystem.Application.Abstracts.Services;
using OnlineSohppingSystem.Application.DTOs.Supports;
using OnlineSohppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Enums;

namespace OnlineShoppingSystem.Persistence.Services
{
    public sealed class SupportChatService : ISupportChatService
    {
        private readonly OnlineShoppingSystemDbContext _db;

        public SupportChatService(OnlineShoppingSystemDbContext db) => _db = db;

        public async Task<Guid> CreateThreadAsync(Guid customerId, string subject)
        {
            var t = new SupportChatThread
            {
                CustomerId = customerId,
                Subject = subject,
                Status = SupportThreadStatus.Open,
                LastMessageAt = DateTime.UtcNow
            };
            await _db.AddAsync(t);
            await _db.SaveChangesAsync();
            return t.Id;
        }

        public async Task<SupportMessageDto> SendAsync(
            Guid threadId,
            Guid senderId,
            string text,
            bool isInternalNote = false,
            string? attachmentUrl = null)
        {
            var thread = await _db.Set<SupportChatThread>()
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(x => x.Id == threadId)
                         ?? throw new InvalidOperationException("Thread not found");

            
            var isOwner = thread.CustomerId == senderId;
            var isAssignee = thread.AssignedToId == senderId; 

            if (!isOwner && !isAssignee)
                throw new UnauthorizedAccessException("You are not allowed to post to this thread.");

            var msg = new SupportChatMessage
            {
                ThreadId = threadId,
                SenderId = senderId,
                Text = text,
                IsInternalNote = isInternalNote,
                AttachmentUrl = attachmentUrl
            };
            await _db.AddAsync(msg);

            
            var tracked = await _db.Set<SupportChatThread>().FirstAsync(x => x.Id == threadId);
            tracked.LastMessageAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return new SupportMessageDto
            {
                Id = msg.Id,
                ThreadId = msg.ThreadId,
                SenderId = msg.SenderId ?? Guid.Empty, 
                Text = msg.Text,
                AttachmentUrl = msg.AttachmentUrl,
                IsInternalNote = msg.IsInternalNote,
                CreatedAt = msg.CreatedAt
            };

        }

        public async Task AssignAsync(Guid threadId, Guid agentId)
        {
            var t = await _db.Set<SupportChatThread>().FirstOrDefaultAsync(x => x.Id == threadId)
                    ?? throw new InvalidOperationException("Thread not found");

            t.AssignedToId = agentId;
            await _db.SaveChangesAsync();
        }

        public async Task SetStatusAsync(Guid threadId, SupportThreadStatus status)
        {
            var t = await _db.Set<SupportChatThread>().FirstOrDefaultAsync(x => x.Id == threadId)
                    ?? throw new InvalidOperationException("Thread not found");

            t.Status = status;
            await _db.SaveChangesAsync();
        }

        public async Task<(IEnumerable<object> messages, int total)> GetMessagesAsync(
            Guid threadId,
            Guid requesterId,
            int page = 1,
            int pageSize = 50)
        {
            var thread = await _db.Set<SupportChatThread>()
                                  .AsNoTracking()
                                  .FirstOrDefaultAsync(t => t.Id == threadId)
                         ?? throw new InvalidOperationException("Thread not found");

            var isOwner = thread.CustomerId == requesterId;
            var isAssignee = thread.AssignedToId == requesterId; 

            if (!isOwner && !isAssignee)
                throw new UnauthorizedAccessException("You are not allowed to view this thread.");

            var q = _db.Set<SupportChatMessage>()
                       .AsNoTracking()
                       .Where(m => m.ThreadId == threadId)
                       .OrderBy(m => m.CreatedAt);

            var total = await q.CountAsync();

            var items = await q.Skip((page - 1) * pageSize)
                               .Take(pageSize)
                               .Select(m => new
                               {
                                   m.Id,
                                   m.ThreadId,
                                   m.SenderId,
                                   m.Text,
                                   m.AttachmentUrl,
                                   m.IsInternalNote,
                                   m.CreatedAt
                               })
                               .ToListAsync();

            return (items, total);
        }

        public async Task<IEnumerable<object>> GetMyThreadsAsync(Guid userId)
        {
            return await _db.Set<SupportChatThread>()
                .AsNoTracking()
                .Where(t => t.CustomerId == userId || t.AssignedToId == userId) 
                .OrderByDescending(t => t.LastMessageAt)
                .Select(t => new
                {
                    t.Id,
                    t.Subject,
                    t.Status,
                    t.CustomerId,
                    t.AssignedToId,
                    t.LastMessageAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetOpenThreadsAsync()
        {
            return await _db.Set<SupportChatThread>()
                .AsNoTracking()
                .Where(t => t.Status == SupportThreadStatus.Open || t.Status == SupportThreadStatus.Pending)
                .OrderByDescending(t => t.LastMessageAt)
                .Select(t => new
                {
                    t.Id,
                    t.Subject,
                    t.Status,
                    t.CustomerId,
                    t.AssignedToId,
                    t.LastMessageAt
                })
                .ToListAsync();
        }
    }
}
