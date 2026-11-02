using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItem>> GetAllAsync(bool? apenasBaixo, string? searchTerm, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);

    Task<decimal> GetValorTotalEstoqueAsync(CancellationToken cancellationToken = default);
}