using Microsoft.EntityFrameworkCore.Query;
using OnlineShppingSystem.Domain.Entities;
using System.Linq.Expressions;

namespace OnlineShppingSystem.Application.Abstracts.Repositories;

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetAsync(Expression<Func<Order, bool>> predicate, Func<IQueryable<Order>, IIncludableQueryable<Order, object>>? include = null);
}
