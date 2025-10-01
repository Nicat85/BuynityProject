using OnlineShppingSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OnlineShppingSystem.Application.Abstracts.Repositories;

public interface ICategoryRepository : IRepository<Category>
{
    Task<List<Category>> GetAllWithChildrenAsync(bool isTracking = false);
    Task HardDeleteAndSaveChangesAsync(Guid id);
    Task RestoreWithChildrenAsync(Category category);
    Task<Category?> GetByIdIncludingDeletedAsync(Guid id);
}
