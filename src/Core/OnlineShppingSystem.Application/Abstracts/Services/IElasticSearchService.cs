using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Abstracts.Services;

public interface IElasticSearchService
{
    Task IndexProductAsync(Product product);
    Task DeleteProductAsync(Guid productId);
    Task<List<Product>> SearchProductsAsync(string query);
}

