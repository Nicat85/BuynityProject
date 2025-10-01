using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Domain.Entities;
using System;

namespace OnlineShoppingSystem.Persistence.Repositories;

public sealed class ReviewRepository : Repository<Review>, IReviewRepository
{
    private readonly OnlineShoppingSystemDbContext _ctx;

    public ReviewRepository(OnlineShoppingSystemDbContext ctx) : base(ctx)
    {
        _ctx = ctx;
    }

    public Task<bool> ExistsByUserAndProductAsync(Guid userId, Guid productId, CancellationToken ct = default)
        => _ctx.Reviews.AnyAsync(r => r.UserId == userId && r.ProductId == productId, ct);

    public async Task<List<Review>> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken ct = default)
    {
        return await _ctx.Reviews
            .AsNoTracking()
            .Where(r => r.ProductId == productId)
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(r => r.User)
            .ToListAsync(ct);
    }

    public Task<int> CountByProductAsync(Guid productId, CancellationToken ct = default)
        => _ctx.Reviews.CountAsync(r => r.ProductId == productId, ct);

    public async Task<Dictionary<int, int>> GetDistributionAsync(Guid productId, CancellationToken ct = default)
    {
        var groups = await _ctx.Reviews
            .Where(r => r.ProductId == productId)
            .GroupBy(r => r.Rating)
            .Select(g => new { Rating = g.Key, Count = g.Count() })
            .ToListAsync(ct);

        var dict = new Dictionary<int, int> { [1] = 0, [2] = 0, [3] = 0, [4] = 0, [5] = 0 };
        foreach (var g in groups) if (dict.ContainsKey(g.Rating)) dict[g.Rating] = g.Count;
        return dict;
    }

    public async Task<double> GetAverageAsync(Guid productId, CancellationToken ct = default)
    {
        var q = _ctx.Reviews.Where(r => r.ProductId == productId);
        if (!await q.AnyAsync(ct)) return 0d;
        return await q.AverageAsync(r => r.Rating, ct);
    }

    public Task<List<Review>> GetByUserAsync(Guid userId, CancellationToken ct = default)
        => _ctx.Reviews.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt)
            .Include(r => r.Product)
            .ToListAsync(ct);

    public Task<Review?> GetByIdWithUserAsync(Guid id, CancellationToken ct = default)
        => _ctx.Reviews.Include(r => r.User).FirstOrDefaultAsync(r => r.Id == id, ct);
}
