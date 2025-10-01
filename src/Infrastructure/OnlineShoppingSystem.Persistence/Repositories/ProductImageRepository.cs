using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Domain.Entities;
using System;

namespace OnlineShoppingSystem.Persistence.Repositories;

public class ProductImageRepository : Repository<ProductImage>, IProductImageRepository
{

    public ProductImageRepository(OnlineShoppingSystemDbContext context) : base(context)
    {
    }

    public async Task<List<ProductImage>> GetByProductIdAsync(Guid productId)
    {
        return await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync();
    }

    public async Task DeleteImagesAsync(List<Guid> imageIds)
    {
        var images = await _context.ProductImages
            .Where(i => imageIds.Contains(i.Id))
            .ToListAsync();

        _context.ProductImages.RemoveRange(images);
        await _context.SaveChangesAsync();
    }
}
