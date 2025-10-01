using Microsoft.EntityFrameworkCore.Storage;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShoppingSystem.Persistence.Repositories;
using OnlineShppingSystem.Application.Abstracts.Repositories;
using OnlineShppingSystem.Domain.Entities;

public class UnitOfWork : IUnitOfWork
{
    private readonly OnlineShoppingSystemDbContext _context;

    public UnitOfWork(OnlineShoppingSystemDbContext context) => _context = context;

    public IRepository<T> Repository<T>() where T : BaseEntity => new Repository<T>(_context);

    public Task<int> SaveChangesAsync() => _context.SaveChangesAsync();

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        => _context.Database.BeginTransactionAsync(ct);
}
