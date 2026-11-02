using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakeProductRepository : FakeRepositoryBase<Product>, IProductRepository
{
    public Task<IReadOnlyList<Product>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Product> query = _entities.Values;

        if (isActive.HasValue)
        {
            query = query.Where(p => p.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyList<Product>>(query.OrderBy(p => p.Name).ToList());
    }
}