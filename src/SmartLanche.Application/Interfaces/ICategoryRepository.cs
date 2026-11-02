using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface ICategoryRepository
{
    Task<IReadOnlyList<Category>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);

    Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Category category, CancellationToken cancellationToken = default);

    Task UpdateAsync(Category category, CancellationToken cancellationToken = default);
}