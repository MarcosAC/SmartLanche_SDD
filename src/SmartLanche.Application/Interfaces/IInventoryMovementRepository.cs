using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IInventoryMovementRepository
{
    Task<IReadOnlyList<InventoryMovement>> GetByItemAsync(int itemId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovement>> GetByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken = default);
}