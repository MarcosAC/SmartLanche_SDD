using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Entities;

public class Order
{
    public int Id { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public DateTime OrderDate { get; set; }

    public OrderStatus Status { get; set; }

    public decimal TotalAmount { get; set; }

    public string? Notes { get; set; }

    public DateTime? PreparationStartedAt { get; set; }

    public DateTime? DeliveredAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public PendingPayment? PendingPayment { get; set; }

    public void RecalculateTotal()
    {
        TotalAmount = OrderItems.Sum(i => i.Subtotal);
    }

    public void ValidateStatusTransition(OrderStatus newStatus)
    {
        if (Status == OrderStatus.Cancelado)
        {
            throw new InvalidOperationException("Pedidos cancelados não podem ter seu status alterado");
        }

        var validTransitions = new Dictionary<OrderStatus, OrderStatus[]>
        {
            [OrderStatus.Aberto] = [OrderStatus.EmPreparacao, OrderStatus.Fechado, OrderStatus.Cancelado],
            [OrderStatus.EmPreparacao] = [OrderStatus.Entregue, OrderStatus.Cancelado],
            [OrderStatus.Fechado] = [OrderStatus.Cancelado],
            [OrderStatus.Entregue] = [OrderStatus.Fechado, OrderStatus.Cancelado],
        };

        if (!validTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new InvalidOperationException($"Transição de status inválida: {Status} → {newStatus}");
        }
    }

    public void StartPreparation()
    {
        ValidateStatusTransition(OrderStatus.EmPreparacao);
        Status = OrderStatus.EmPreparacao;
        PreparationStartedAt = DateTime.Now;
    }

    public void MarkAsDelivered()
    {
        ValidateStatusTransition(OrderStatus.Entregue);
        Status = OrderStatus.Entregue;
        DeliveredAt = DateTime.Now;
    }

    public void Close()
    {
        ValidateStatusTransition(OrderStatus.Fechado);
        if (!OrderItems.Any())
        {
            throw new InvalidOperationException("Pedido não pode ser fechado sem itens");
        }
        Status = OrderStatus.Fechado;
    }

    public void Cancel()
    {
        ValidateStatusTransition(OrderStatus.Cancelado);
        Status = OrderStatus.Cancelado;
        CancelledAt = DateTime.Now;
    }
}