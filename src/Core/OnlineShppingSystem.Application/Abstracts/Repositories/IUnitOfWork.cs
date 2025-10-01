using Microsoft.EntityFrameworkCore.Storage;
using OnlineShppingSystem.Domain.Entities;

namespace OnlineShppingSystem.Application.Abstracts.Repositories;

public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : BaseEntity;
    Task<int> SaveChangesAsync();
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
