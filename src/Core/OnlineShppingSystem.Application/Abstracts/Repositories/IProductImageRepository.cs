using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Abstracts.Repositories;

public interface IProductImageRepository : IRepository<ProductImage>
{
    Task<List<ProductImage>> GetByProductIdAsync(Guid productId);
    Task DeleteImagesAsync(List<Guid> imageIds);
}