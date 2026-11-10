using System.Transactions;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class InventoryService : IInventoryService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IInventoryMovementRepository _movementRepository;

    public InventoryService(
        IInventoryRepository inventoryRepository,
        IInventoryMovementRepository movementRepository)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _movementRepository = movementRepository ?? throw new ArgumentNullException(nameof(movementRepository));
    }

    public async Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductId.HasValue)
        {
            var existing = await _inventoryRepository.GetByProductIdAsync(request.ProductId.Value, cancellationToken);
            if (existing is not null)
            {
                throw new InvalidOperationException("Já existe item de estoque para este produto.");
            }
        }

        var item = new InventoryItem
        {
            ProductId = request.ProductId,
            Nome = request.Nome,
            QuantidadeAtual = request.QuantidadeAtual,
            QuantidadeMinima = request.QuantidadeMinima,
            ValorUnitario = request.ValorUnitario,
            UnidadeMedida = request.UnidadeMedida,
            Notes = request.Notes
        };

        item.ValidateForCreate();

        if (item.QuantidadeAtual > 0)
        {
            var initialMovement = new InventoryMovement
            {
                InventoryItemId = item.Id,
                Tipo = InventoryMovementType.Entrada,
                Motivo = InventoryMovementReason.Compra,
                Quantidade = item.QuantidadeAtual,
                ValorUnitario = item.ValorUnitario,
                DataHora = DateTime.UtcNow,
                Fornecedor = "Estoque inicial",
                Notes = "Criação do item de estoque"
            };
            item.Movements.Add(initialMovement);
        }

        await _inventoryRepository.AddAsync(item, cancellationToken);

        return MapToDto(item);
    }

    public async Task<InventoryItemDto> UpdateAsync(int id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryRepository.GetByIdAsync(id, cancellationToken);

        if (item is null)
        {
            throw new ArgumentException($"Item de estoque {id} não encontrado.", nameof(id));
        }

        item.QuantidadeMinima = request.QuantidadeMinima;
        item.ValorUnitario = request.ValorUnitario;
        item.UnidadeMedida = request.UnidadeMedida;
        item.Notes = request.Notes;

        item.ValidateForUpdate();

        await _inventoryRepository.UpdateAsync(item, cancellationToken);

        return MapToDto(item);
    }

    public async Task<InventoryMovementDto> RegistrarEntradaAsync(RegistrarEntradaRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryRepository.GetByIdAsync(request.InventoryItemId, cancellationToken);

        if (item is null)
        {
            throw new ArgumentException($"Item de estoque {request.InventoryItemId} não encontrado.", nameof(request.InventoryItemId));
        }

        item.ApplyEntrada(request.Quantidade, request.ValorUnitario, request.Fornecedor, request.Notes);

        await _inventoryRepository.UpdateAsync(item, cancellationToken);

        var movement = item.Movements.Last();

        await _movementRepository.AddAsync(movement, cancellationToken);

        return MapToMovementDto(movement, item.Nome);
    }

    public async Task<InventoryMovementDto> RegistrarSaidaAsync(RegistrarSaidaRequest request, CancellationToken cancellationToken = default)
    {
        var item = await _inventoryRepository.GetByIdAsync(request.InventoryItemId, cancellationToken);

        if (item is null)
        {
            throw new ArgumentException($"Item de estoque {request.InventoryItemId} não encontrado.", nameof(request.InventoryItemId));
        }

        item.ApplySaida(request.Quantidade, request.Motivo, request.Notes);

        await _inventoryRepository.UpdateAsync(item, cancellationToken);

        var movement = item.Movements.Last();

        await _movementRepository.AddAsync(movement, cancellationToken);

        return MapToMovementDto(movement, item.Nome);
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetItensBaixosAsync(CancellationToken cancellationToken = default)
    {
        var items = await _inventoryRepository.GetAllAsync(true, null, cancellationToken);
        return items.Where(i => i.EstoqueBaixo).Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<InventoryItemDto>> GetItensZeradosAsync(CancellationToken cancellationToken = default)
    {
        var items = await _inventoryRepository.GetAllAsync(true, null, cancellationToken);
        return items.Where(i => i.EstoqueZerado).Select(MapToDto).ToList();
    }

    public async Task<decimal> GetValorTotalEstoqueAsync(CancellationToken cancellationToken = default)
    {
        return await _inventoryRepository.GetValorTotalEstoqueAsync(cancellationToken);
    }

    public async Task BaixaAutomaticaPedidoAsync(int orderId, IReadOnlyList<OrderItem> orderItems, CancellationToken cancellationToken = default)
    {
        // Use TransactionScope to ensure all inventory deductions are atomic
        // Requires: services.AddDbContext<...>(ServiceLifetime.Scoped) and compatible SQL Server
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        foreach (var orderItem in orderItems)
        {
            await ProcessOrderItemAsync(orderItem, orderId, cancellationToken);
        }

        scope.Complete();
    }

    public async Task EstornarBaixaPedidoAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var movements = await _movementRepository.GetByOrderAsync(orderId, cancellationToken);

        var vendaMovements = movements.Where(m => m.Motivo == InventoryMovementReason.Venda).ToList();

        foreach (var movement in vendaMovements)
        {
            var item = await _inventoryRepository.GetByIdAsync(movement.InventoryItemId, cancellationToken);

            if (item is not null)
            {
                item.QuantidadeAtual += movement.Quantidade;

                var estornoMovement = new InventoryMovement
                {
                    InventoryItemId = item.Id,
                    Tipo = InventoryMovementType.Entrada,
                    Motivo = InventoryMovementReason.EstornoVenda,
                    Quantidade = movement.Quantidade,
                    ValorUnitario = movement.ValorUnitario,
                    DataHora = DateTime.UtcNow,
                    OrderId = orderId,
                    ComboId = movement.ComboId,
                    Notes = $"Estorno de venda - Pedido #{orderId}"
                };

                item.Movements.Add(estornoMovement);

                await _inventoryRepository.UpdateAsync(item, cancellationToken);
                await _movementRepository.AddAsync(estornoMovement, cancellationToken);
            }
        }
    }

    private async Task ProcessOrderItemAsync(OrderItem orderItem, int orderId, CancellationToken cancellationToken)
    {
        if (orderItem.ProductId > 0)
        {
            var inventoryItem = await _inventoryRepository.GetByProductIdAsync(orderItem.ProductId, cancellationToken);

            if (inventoryItem is not null)
            {
                var quantidade = orderItem.Quantity;

                inventoryItem.QuantidadeAtual -= quantidade;

                var movement = new InventoryMovement
                {
                    InventoryItemId = inventoryItem.Id,
                    Tipo = InventoryMovementType.Saida,
                    Motivo = InventoryMovementReason.Venda,
                    Quantidade = quantidade,
                    ValorUnitario = inventoryItem.ValorUnitario,
                    DataHora = DateTime.UtcNow,
                    OrderId = orderId,
                    ComboId = orderItem.ComboId,
                    Notes = $"Baixa automática - Pedido #{orderId}"
                };

                inventoryItem.Movements.Add(movement);

                await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);
                await _movementRepository.AddAsync(movement, cancellationToken);
            }
            // If no inventory item exists for this product, skip creating movement
            // to avoid FK violation (InventoryItemId would be 0)
            // Consider making InventoryItemId nullable in InventoryMovement if logging is needed
        }
    }

    private static InventoryItemDto MapToDto(InventoryItem item)
    {
        return new InventoryItemDto(
            item.Id,
            item.ProductId,
            item.Produto?.Name,
            item.Nome,
            item.QuantidadeAtual,
            item.QuantidadeMinima,
            item.ValorUnitario,
            item.UnidadeMedida,
            item.Notes,
            item.EstoqueBaixo,
            item.EstoqueZerado,
            item.ValorTotalEstoque);
    }

    private static InventoryMovementDto MapToMovementDto(InventoryMovement movement, string itemNome)
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
            movement.Notes);
    }
}