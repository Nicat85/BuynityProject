using OnlineShppingSystem.Domain.Entities;
using OnlineSohppingSystem.Domain.Entities;

namespace OnlineSohppingSystem.Application.Abstracts.Repositories;

public interface IOrderItemRepository
{
    Task AddAsync(OrderItem entity);
    Task AddRangeAsync(IEnumerable<OrderItem> entities);
}
