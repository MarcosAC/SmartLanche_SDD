using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IComboRepository
{
    Task<IReadOnlyList<Combo>> GetAllAsync(
        bool? isActive = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Combo>> GetAvailableAsync(CancellationToken cancellationToken = default);

    Task<Combo?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Combo?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default);

    Task<Combo?> GetWithItemsTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task<Combo?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Combo combo, CancellationToken cancellationToken = default);

    Task UpdateAsync(Combo combo, CancellationToken cancellationToken = default);

    Task DeleteAsync(Combo combo, CancellationToken cancellationToken = default);
}