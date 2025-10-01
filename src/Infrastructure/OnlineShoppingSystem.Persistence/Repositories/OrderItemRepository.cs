using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Application.Abstracts.Repositories;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineShoppingSystem.Persistence.Repositories;

public class OrderItemRepository : IOrderItemRepository
{
    private readonly OnlineShoppingSystemDbContext _context;
    public OrderItemRepository(OnlineShoppingSystemDbContext context) => _context = context;

    public Task AddAsync(OrderItem entity)
        => _context.Set<OrderItem>().AddAsync(entity).AsTask();

    public Task AddRangeAsync(IEnumerable<OrderItem> entities)
        => _context.Set<OrderItem>().AddRangeAsync(entities);
}