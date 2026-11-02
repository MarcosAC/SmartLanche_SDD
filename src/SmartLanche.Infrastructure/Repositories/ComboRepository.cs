using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class ComboRepository : IComboRepository
{
    private readonly ApplicationDbContext _context;

    public ComboRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Combo>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Combo> query = _context.Combos
            .AsNoTracking()
            .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product);

        if (isActive.HasValue)
        {
            query = query.IgnoreQueryFilters().Where(c => c.IsActive == isActive.Value);
        }
        else
        {
            query = query.IgnoreQueryFilters(); // Show all (active + inactive)
        }

        return await query
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Combo>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Combos
            .AsNoTracking()
            .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
            .Where(c => c.IsActive
                && c.ComboItems.All(ci => ci.Product.IsActive))
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Combo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Combos.IgnoreQueryFilters().AsNoTracking()
            .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Combo?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Combos.AsNoTracking()
            .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Combo?> GetWithItemsTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Combos
            .Include(c => c.ComboItems)
                .ThenInclude(ci => ci.Product)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Combo?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Combos.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task AddAsync(Combo combo, CancellationToken cancellationToken = default)
    {
        await _context.Combos.AddAsync(combo, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Combo combo, CancellationToken cancellationToken = default)
    {
        _context.Entry(combo).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Combo combo, CancellationToken cancellationToken = default)
    {
        _context.Combos.Remove(combo);
        await _context.SaveChangesAsync(cancellationToken);
    }
}