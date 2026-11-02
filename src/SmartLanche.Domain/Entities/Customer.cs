namespace SmartLanche.Domain.Entities;

public class Customer : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();

    public ICollection<PendingPayment> PendingPayments { get; set; } = new List<PendingPayment>();
}