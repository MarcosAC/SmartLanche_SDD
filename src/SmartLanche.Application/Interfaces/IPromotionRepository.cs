using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IPromotionRepository
{
    Task<IReadOnlyList<Promotion>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Promotion>> GetActiveAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Promotion>> GetApplicableToProductAsync(
        int productId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Promotion>> GetApplicableToCategoryAsync(
        int categoryId,
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Promotion>> GetGlobalActiveAsync(
        DateTime date,
        CancellationToken cancellationToken = default);

    Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Promotion?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);

    Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default);

    Task DeleteAsync(Promotion promotion, CancellationToken cancellationToken = default);
}