using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakeCategoryRepository : FakeRepositoryBase<Category>, ICategoryRepository
{
    public Task<IReadOnlyList<Category>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Category> query = _entities.Values;

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyList<Category>>(query.OrderBy(c => c.Name).ToList());
    }

    public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        var exists = _entities.Values.Any(c =>
            c.Id != excludeId &&
            c.Name.Trim().ToUpperInvariant() == normalizedName);
        return Task.FromResult(exists);
    }
}