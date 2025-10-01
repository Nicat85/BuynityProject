using OnlineShppingSystem.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineShoppingSystem.Persistence.Contexts;
using OnlineShppingSystem.Application.Abstracts.Repositories;

namespace OnlineShoppingSystem.Persistence.Repositories
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {

        public CategoryRepository(OnlineShoppingSystemDbContext context) : base(context)
        {
        }

        public async Task<List<Category>> GetAllWithChildrenAsync(bool isTracking = false)
        {
            var query = _context.Categories
                .Where(c => !c.IsDeleted)
                .Include(c => c.Children.Where(child => !child.IsDeleted))
                .AsQueryable();

            if (!isTracking)
                query = query.AsNoTracking();

            return await query.ToListAsync();
        }
        public async Task HardDeleteAndSaveChangesAsync(Guid id)
        {
            var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id);
            if (category is not null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }
        public async Task RestoreWithChildrenAsync(Category category)
        {
            category.IsDeleted = false;
            category.DeletedAt = null;

            
            await RestoreChildrenRecursiveAsync(category);

            _context.Categories.Update(category);
        }

        public async Task<Category?> GetByIdIncludingDeletedAsync(Guid id)
        {
            return await _context.Categories
                .Include(c => c.Children)
                .FirstOrDefaultAsync(c => c.Id == id); 
        }

        private async Task RestoreChildrenRecursiveAsync(Category category)
        {
            var children = await _context.Categories
                .Where(c => c.ParentId == category.Id && c.IsDeleted)
                .ToListAsync();

            foreach (var child in children)
            {
                child.IsDeleted = false;
                child.DeletedAt = null;

                _context.Categories.Update(child);

                await RestoreChildrenRecursiveAsync(child);
            }
        }
    }
}
