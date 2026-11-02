using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly ApplicationDbContext _context;

    public PromotionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Promotion>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Promotion> query = _context.Promotions
            .AsNoTracking()
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories);

        if (isActive.HasValue)
        {
            query = query.IgnoreQueryFilters().Where(p => p.IsActive == isActive.Value);
        }
        else
        {
            query = query.IgnoreQueryFilters(); // Show all (active + inactive)
        }

        return await query
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Promotions
            .AsNoTracking()
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories)
            .Where(p => p.IsActive
                && p.StartDate <= date
                && (p.EndDate == null || p.EndDate >= date))
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetApplicableToProductAsync(
        int productId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Promotions
            .AsNoTracking()
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories)
            .Where(p => p.IsActive
                && p.StartDate <= date
                && (p.EndDate == null || p.EndDate >= date)
                && (p.ApplicableProducts.Any(ap => ap.Id == productId)
                    || p.ApplicableCategories.Any(ac => ac.Products.Any(p => p.Id == productId))))
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetApplicableToCategoryAsync(
        int categoryId,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Promotions
            .AsNoTracking()
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories)
            .Where(p => p.IsActive
                && p.StartDate <= date
                && (p.EndDate == null || p.EndDate >= date)
                && p.ApplicableCategories.Any(ac => ac.Id == categoryId))
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Promotion>> GetGlobalActiveAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        return await _context.Promotions
            .AsNoTracking()
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories)
            .Where(p => p.IsActive
                && p.StartDate <= date
                && (p.EndDate == null || p.EndDate >= date)
                && !p.ApplicableProducts.Any()
                && !p.ApplicableCategories.Any())
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Promotions.IgnoreQueryFilters().AsNoTracking()
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Promotion?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Promotions
            .Include(p => p.ApplicableProducts)
            .Include(p => p.ApplicableCategories)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        await _context.Promotions.AddAsync(promotion, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        _context.Entry(promotion).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        _context.Promotions.Remove(promotion);
        await _context.SaveChangesAsync(cancellationToken);
    }
}