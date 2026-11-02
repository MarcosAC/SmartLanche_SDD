using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class InventoryMovementService : IInventoryMovementService
{
    private readonly IInventoryMovementRepository _movementRepository;
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryMovementService(
        IInventoryMovementRepository movementRepository,
        IInventoryRepository inventoryRepository)
    {
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetByItemAsync(int itemId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken = default)
    {
        var movements = await _movementRepository.GetByItemAsync(itemId, dataInicio, dataFim, cancellationToken);
        return await MapMovementsToDto(movements, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var movements = await _movementRepository.GetByOrderAsync(orderId, cancellationToken);
        return await MapMovementsToDto(movements, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovementDto>> GetFilteredAsync(
        int? itemId,
        DateTime? dataInicio,
        DateTime? dataFim,
        InventoryMovementType? tipo,
        InventoryMovementReason? motivo,
        int? orderId,
        int? comboId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (itemId.HasValue)
        {
            var movements = await _movementRepository.GetByItemAsync(itemId.Value, dataInicio, dataFim, cancellationToken);
            return await ApplyFiltersAndMap(movements, tipo, motivo, orderId, comboId, searchTerm, page, pageSize, cancellationToken);
        }

        if (orderId.HasValue)
        {
            var movements = await _movementRepository.GetByOrderAsync(orderId.Value, cancellationToken);
            return await ApplyFiltersAndMap(movements, tipo, motivo, orderId, comboId, searchTerm, page, pageSize, cancellationToken);
        }

        var allItems = await _inventoryRepository.GetAllAsync(null, null, cancellationToken);
        var allMovements = new List<InventoryMovement>();

        foreach (var item in allItems)
        {
            var itemMovements = await _movementRepository.GetByItemAsync(item.Id, dataInicio, dataFim, cancellationToken);
            allMovements.AddRange(itemMovements);
        }

        return await ApplyFiltersAndMap(allMovements, tipo, motivo, orderId, comboId, searchTerm, page, pageSize, cancellationToken);
    }

    private async Task<IReadOnlyList<InventoryMovementDto>> MapMovementsToDto(IReadOnlyList<InventoryMovement> movements, CancellationToken cancellationToken)
    {
        var result = new List<InventoryMovementDto>();

        foreach (var movement in movements.OrderByDescending(m => m.DataHora))
        {
            var item = await GetItemNameAsync(movement.InventoryItemId, cancellationToken);
            result.Add(MapToDto(movement, item));
        }

        return result;
    }

    private async Task<string> GetItemNameAsync(int itemId, CancellationToken cancellationToken)
    {
        var item = await _inventoryRepository.GetByIdAsync(itemId, cancellationToken);
        return item?.Nome ?? $"Item #{itemId}";
    }

    private async Task<IReadOnlyList<InventoryMovementDto>> ApplyFiltersAndMap(
        IReadOnlyList<InventoryMovement> movements,
        InventoryMovementType? tipo,
        InventoryMovementReason? motivo,
        int? orderId,
        int? comboId,
        string? searchTerm,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        IEnumerable<InventoryMovement> query = movements;

        if (tipo.HasValue)
            query = query.Where(m => m.Tipo == tipo.Value);

        if (motivo.HasValue)
            query = query.Where(m => m.Motivo == motivo.Value);

        if (orderId.HasValue)
            query = query.Where(m => m.OrderId == orderId.Value);

        if (comboId.HasValue)
            query = query.Where(m => m.ComboId == comboId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(m =>
                (m.Observacoes != null && m.Observacoes.ToLower().Contains(term)) ||
                m.OrderId.ToString().Contains(term) ||
                (m.ComboId.HasValue && m.ComboId.Value.ToString().Contains(term)));
        }

        var paged = query
            .OrderByDescending(m => m.DataHora)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var result = new List<InventoryMovementDto>();

        foreach (var movement in paged)
        {
            var itemName = await GetItemNameAsync(movement.InventoryItemId, cancellationToken);
            result.Add(MapToDto(movement, itemName));
        }

        return result;
    }

    private static InventoryMovementDto MapToDto(InventoryMovement movement, string itemNome)
    {
        return new InventoryMovementDto(
            movement.Id,
            movement.InventoryItemId,
            itemNome,
            movement.Tipo,
            movement.Motivo,
            movement.Quantidade,
            movement.ValorUnitario,
            movement.ValorTotal,
            movement.DataHora,
            movement.OrderId,
            movement.ComboId,
            movement.Fornecedor,
            movement.Observacoes);
    }
}