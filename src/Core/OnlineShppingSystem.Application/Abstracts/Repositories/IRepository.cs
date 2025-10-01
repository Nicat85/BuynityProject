using OnlineShppingSystem.Domain.Entities;
using System.Linq.Expressions;

namespace OnlineShppingSystem.Application.Abstracts.Repositories;

public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(Guid id, bool isTracking = true);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, bool isTracking = false);
    IQueryable<T> GetAll(bool isTracking = false);
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    void Update(T entity);
    void SoftDelete(T entity);
    void HardDelete(T entity);
    Task<bool> SaveChangesAsync();
    Task<T?> GetByExpressionAsync(Expression<Func<T, bool>> expression);
    IQueryable<T> GetQueryable();
}
