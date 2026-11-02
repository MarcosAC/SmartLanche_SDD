using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakePromotionRepository : FakeRepositoryBase<Promotion>, IPromotionRepository
{
    public Task<IReadOnlyList<Promotion>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Promotion> query = _entities.Values;
        if (isActive.HasValue)
            query = query.Where(p => p.IsActive == isActive.Value);
        return Task.FromResult<IReadOnlyList<Promotion>>(query.OrderBy(p => p.Name).ToList());
    }

    public Task<IReadOnlyList<Promotion>> GetActiveAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var promotions = _entities.Values
            .Where(p => p.IsValidOn(date))
            .OrderBy(p => p.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Promotion>>(promotions);
    }

    public Task<IReadOnlyList<Promotion>> GetApplicableToProductAsync(int productId, DateTime date, CancellationToken cancellationToken = default)
    {
        var promotions = _entities.Values
            .Where(p => p.IsValidOn(date) && (p.IsGlobal || p.ApplicableProducts.Any(ap => ap.Id == productId)))
            .OrderBy(p => p.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Promotion>>(promotions);
    }

    public Task<IReadOnlyList<Promotion>> GetApplicableToCategoryAsync(int categoryId, DateTime date, CancellationToken cancellationToken = default)
    {
        var promotions = _entities.Values
            .Where(p => p.IsValidOn(date) && (p.IsGlobal || p.ApplicableCategories.Any(ac => ac.Id == categoryId)))
            .OrderBy(p => p.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Promotion>>(promotions);
    }

    public Task<IReadOnlyList<Promotion>> GetGlobalActiveAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        var promotions = _entities.Values
            .Where(p => p.IsValidOn(date) && p.IsGlobal)
            .OrderBy(p => p.Name)
            .ToList();
        return Task.FromResult<IReadOnlyList<Promotion>>(promotions);
    }

    public Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return base.GetByIdAsync(id, cancellationToken);
    }

    public Task<Promotion?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return base.GetByIdAsync(id, cancellationToken);
    }

    public Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        return base.AddAsync(promotion, cancellationToken);
    }

    public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        return base.UpdateAsync(promotion, cancellationToken);
    }

    public Task DeleteAsync(Promotion promotion, CancellationToken cancellationToken = default)
    {
        return base.DeleteAsync(promotion, cancellationToken);
    }
}