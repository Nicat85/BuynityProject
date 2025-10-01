using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Application.Abstracts.Repositories;

public interface IReviewRepository : IRepository<Review>
{
    Task<bool> ExistsByUserAndProductAsync(Guid userId, Guid productId, CancellationToken ct = default);
    Task<List<Review>> GetByProductAsync(Guid productId, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountByProductAsync(Guid productId, CancellationToken ct = default);
    Task<Dictionary<int, int>> GetDistributionAsync(Guid productId, CancellationToken ct = default); 
    Task<double> GetAverageAsync(Guid productId, CancellationToken ct = default);
    Task<List<Review>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Review?> GetByIdWithUserAsync(Guid id, CancellationToken ct = default);
}
