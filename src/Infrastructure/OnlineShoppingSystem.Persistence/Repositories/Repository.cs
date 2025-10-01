using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Domain.Entities;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace OnlineShoppingSystem.Persistence.Repositories
{
    public class Repository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly OnlineShoppingSystemDbContext _context;
        protected readonly DbSet<T> _table;

        public Repository(OnlineShoppingSystemDbContext context)
        {
            _context = context;
            _table = _context.Set<T>();
        }

        public async Task<T?> GetByIdAsync(Guid id, bool isTracking = true)
        {
            var query = _table.AsQueryable();
            if (!isTracking) query = query.AsNoTracking();
            return await query.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, bool isTracking = false)
        {
            var query = _table.Where(x => !x.IsDeleted).AsQueryable();
            if (predicate != null) query = query.Where(predicate);
            if (!isTracking) query = query.AsNoTracking();
            return await query.ToListAsync();
        }

        public IQueryable<T> GetAll(bool isTracking = false)
            => isTracking ? _table.Where(x => !x.IsDeleted)
                          : _table.Where(x => !x.IsDeleted).AsNoTracking();

        public IQueryable<T> GetQueryable() => _table.AsQueryable();

        public async Task<T?> GetByExpressionAsync(Expression<Func<T, bool>> expression)
            => await _table.Where(x => !x.IsDeleted).FirstOrDefaultAsync(expression);

        public async Task AddAsync(T entity)
        {
            
            if (entity.Id == Guid.Empty)
                entity.Id = Guid.NewGuid();

            entity.CreatedAt = DateTime.UtcNow;
            entity.UpdatedAt = DateTime.UtcNow;
            entity.IsDeleted = false;

            await _table.AddAsync(entity);
            
        }

        public void Update(T entity)
        {
            entity.UpdatedAt = DateTime.UtcNow;
            _table.Update(entity);
        }

        public void SoftDelete(T entity)
        {
            entity.IsDeleted = true;
            entity.DeletedAt = DateTime.UtcNow;
            _table.Update(entity);
        }

        public void HardDelete(T entity) => _table.Remove(entity);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
            => await _table.AnyAsync(predicate);

        public async Task<bool> SaveChangesAsync()
            => await _context.SaveChangesAsync() > 0;
    }
}