
using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class CategoryRepository : ICategoryRepository
{
    private readonly ApplicationDbContext _context;

    public CategoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Category>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Category> query = _context.Categories.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.IgnoreQueryFilters().Where(category => category.IsActive == isActive.Value);
        }
        else
        {
            query = query.IgnoreQueryFilters(); // Show all (active + inactive)
        }

        return await query.OrderBy(category => category.Name).ToListAsync(cancellationToken);
    }

    public async Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Categories.IgnoreQueryFilters().FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    public async Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        // SQL Server usa collation case-insensitive por padrão (Latin1_General_CI_AS)
        // normalizedName já vem em UPPER, então comparação direta funciona
        IQueryable<Category> query = _context.Categories
            .AsNoTracking()
            .Where(category => category.Name.Trim() == normalizedName);

        if (excludeId.HasValue)
        {
            query = query.Where(category => category.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken = default)
    {
        await _context.Categories.AddAsync(category, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
    {
        _context.Entry(category).State = EntityState.Modified;

        await _context.SaveChangesAsync(cancellationToken);
    }
}