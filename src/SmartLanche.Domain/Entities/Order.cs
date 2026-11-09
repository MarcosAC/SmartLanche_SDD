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

    public string? Observacoes { get; set; }

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
            [OrderStatus.Aberto] = [OrderStatus.EmPreparacao, OrderStatus.Cancelado],
            [OrderStatus.EmPreparacao] = [OrderStatus.Pronto, OrderStatus.Finalizado, OrderStatus.Cancelado],
            [OrderStatus.Pronto] = [OrderStatus.Finalizado, OrderStatus.Cancelado],
            [OrderStatus.Finalizado] = [],
            [OrderStatus.Cancelado] = [],
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

    public void MarkAsReady()
    {
        ValidateStatusTransition(OrderStatus.Pronto);
        Status = OrderStatus.Pronto;
    }

    public void Finalize()
    {
        ValidateStatusTransition(OrderStatus.Finalizado);
        if (!OrderItems.Any())
        {
            throw new InvalidOperationException("Pedido não pode ser finalizado sem itens");
        }
        Status = OrderStatus.Finalizado;
    }

    public void Cancel()
    {
        ValidateStatusTransition(OrderStatus.Cancelado);
        Status = OrderStatus.Cancelado;
        CancelledAt = DateTime.Now;
    }
}