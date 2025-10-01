using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Domain.Entities;
using System.Linq.Expressions;

namespace OnlineShoppingSystem.Persistence.Repositories;

public class OrderRepository : Repository<Order>, IOrderRepository
{
    public OrderRepository(OnlineShoppingSystemDbContext context) : base(context)
    {
    }

    public async Task<Order?> GetAsync(
        Expression<Func<Order, bool>> predicate,
        Func<IQueryable<Order>, IIncludableQueryable<Order, object>>? include = null)
    {
        IQueryable<Order> query = base._context.Orders;

        if (include != null)
        {
            query = include(query);
        }

        return await query.FirstOrDefaultAsync(predicate);
    }
}
