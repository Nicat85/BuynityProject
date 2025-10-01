using OnlineShoppingSystem.Application.Abstracts.Repositories;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Repositories;
public class ProductRepository : Repository<Product>, IProductRepository
{
    public ProductRepository(OnlineShoppingSystemDbContext context) : base(context)
    {
    }
}

