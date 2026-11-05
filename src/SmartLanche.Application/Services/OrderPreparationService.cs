using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class OrderPreparationService : IOrderPreparationService
{
    private readonly IOrderRepository _orderRepository;

    public event EventHandler? OrderChanged;

    public OrderPreparationService(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    private void OnOrderChanged()
    {
        OrderChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<IReadOnlyList<OrderPreparationDto>> GetPreparingOrdersAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(
            status: (int)OrderStatus.EmPreparacao,
            cancellationToken: cancellationToken);

        return orders.Select(MapToPreparationDto).ToList();
    }

    public async Task<OrderDto?> StartPreparationAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.StartPreparation();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        OnOrderChanged();

        return MapToDto(order);
    }

    public async Task<OrderDto?> MarkAsReadyAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.MarkAsReady();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        OnOrderChanged();

        return MapToDto(order);
    }

    public async Task<OrderDto?> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        order.Cancel();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        OnOrderChanged();

        return MapToDto(order);
    }

    private static OrderPreparationDto MapToPreparationDto(Order order)
    {
        var elapsedTime = order.PreparationStartedAt.HasValue
            ? DateTime.Now - order.PreparationStartedAt.Value
            : (TimeSpan?)null;

        var itemsSummary = string.Join(", ",
            order.OrderItems
                .GroupBy(i => i.Product?.Name ?? "Produto")
                .Select(g => $"{g.Sum(i => i.Quantity)}x {g.Key}"));

        return new OrderPreparationDto(
            order.Id,
            order.OrderDate,
            order.PreparationStartedAt,
            elapsedTime,
            order.Customer?.Name,
            order.TotalAmount,
            itemsSummary,
            order.Status);
    }

    private static OrderDto MapToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name,
            OrderDate = order.OrderDate,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            Notes = order.Notes,
            PreparationStartedAt = order.PreparationStartedAt,
            DeliveredAt = order.DeliveredAt,
            CancelledAt = order.CancelledAt,
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal
            }).ToList()
        };
    }
}