using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IOrderPreparationService
{
    event EventHandler? OrderChanged;

    Task<IReadOnlyList<OrderPreparationDto>> GetPreparingOrdersAsync(CancellationToken cancellationToken = default);

    Task<OrderDto?> MarkAsReadyAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto?> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto?> StartPreparationAsync(int orderId, CancellationToken cancellationToken = default);
}