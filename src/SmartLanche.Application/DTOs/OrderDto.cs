using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public class OrderDto
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public string? CustomerName { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime? PreparationStartedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public IReadOnlyList<OrderItemDto> Items { get; set; } = new List<OrderItemDto>();
}