using SmartLanche.Application.DTOs;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Services;

public abstract class BaseService
{
    protected static OrderDto MapToDto(Order order)
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
            Items = order.OrderItems.Select(i => new OrderItemDto
            {
                Id = i.Id,
                OrderId = i.OrderId,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice,
                Subtotal = i.Subtotal,
                ComboId = i.ComboId
            }).ToList(),
            Payments = order.Payments.Select(p => new PaymentDto(
                p.Id,
                p.OrderId,
                p.FormaPagamento,
                p.ValorPago,
                p.Troco,
                p.Parcelas,
                p.ValorParcela,
                p.DataHora,
                p.Notes,
                p.Estornado,
                p.EstornoDataHora,
                p.EstornoMotivo
            )).ToList()
        };
    }
}