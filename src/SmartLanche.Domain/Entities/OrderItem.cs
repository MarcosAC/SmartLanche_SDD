namespace SmartLanche.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order Order { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public int? ComboId { get; set; }

    public Combo? Combo { get; set; }

    private decimal _subtotal;

    public decimal Subtotal
    {
        get => Quantity * UnitPrice;
        private set => _subtotal = value;
    }

    public void Validate()
    {
        if (Quantity <= 0)
        {
            throw new ArgumentException("Quantidade deve ser maior que zero");
        }

        if (UnitPrice < 0)
        {
            throw new ArgumentException("Preço unitário não pode ser negativo");
        }
    }
}