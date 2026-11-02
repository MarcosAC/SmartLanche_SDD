using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IInventoryMovementService
{
    Task<IReadOnlyList<InventoryMovementDto>> GetByItemAsync(int itemId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovementDto>> GetByOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryMovementDto>> GetFilteredAsync(
        int? itemId,
        DateTime? dataInicio,
        DateTime? dataFim,
        SmartLanche.Domain.Enums.InventoryMovementType? tipo,
        SmartLanche.Domain.Enums.InventoryMovementReason? motivo,
        int? orderId,
        int? comboId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
}