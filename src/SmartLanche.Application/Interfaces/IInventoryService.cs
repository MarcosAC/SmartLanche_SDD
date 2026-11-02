using SmartLanche.Application.DTOs;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IInventoryService
{
    Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default);

    Task<InventoryItemDto> UpdateAsync(int id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default);

    Task<InventoryMovementDto> RegistrarEntradaAsync(RegistrarEntradaRequest request, CancellationToken cancellationToken = default);

    Task<InventoryMovementDto> RegistrarSaidaAsync(RegistrarSaidaRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItemDto>> GetItensBaixosAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventoryItemDto>> GetItensZeradosAsync(CancellationToken cancellationToken = default);

    Task<decimal> GetValorTotalEstoqueAsync(CancellationToken cancellationToken = default);

    Task BaixaAutomaticaPedidoAsync(int orderId, IReadOnlyList<OrderItem> orderItems, CancellationToken cancellationToken = default);

    Task EstornarBaixaPedidoAsync(int orderId, CancellationToken cancellationToken = default);
}