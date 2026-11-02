using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakeComboRepository : FakeRepositoryBase<Combo>, IComboRepository
{
    private readonly FakeProductRepository _productRepository;

    public FakeComboRepository(FakeProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public Task<IReadOnlyList<Combo>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Combo> query = _entities.Values;
        if (isActive.HasValue)
            query = query.Where(c => c.IsActive == isActive.Value);
        return Task.FromResult<IReadOnlyList<Combo>>(query.OrderBy(c => c.Name).ToList());
    }

    public Task<Combo?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
    {
        var combo = _entities.Values.FirstOrDefault(c => c.Id == id);
        if (combo != null)
        {
            foreach (var item in combo.ComboItems)
            {
                var product = _productRepository.GetByIdAsync(item.ProductId, cancellationToken).Result;
                if (product != null) item.Product = product;
            }
        }
        return Task.FromResult<Combo?>(combo);
    }

    public Task<Combo?> GetWithItemsTrackedAsync(int id, CancellationToken cancellationToken = default)
        => GetWithItemsAsync(id, cancellationToken);

    public Task<Combo?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Combo>> GetAvailableAsync(CancellationToken cancellationToken = default)
    {
        var available = _entities.Values.Where(c => c.IsAvailable).OrderBy(c => c.Name).ToList();
        return Task.FromResult<IReadOnlyList<Combo>>(available);
    }
}