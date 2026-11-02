namespace SmartLanche.Domain.Entities;

public class ComboItem
{
    public int Id { get; set; }

    public int ComboId { get; set; }

    public Combo Combo { get; set; } = null!;

    public int ProductId { get; set; }

    public Product Product { get; set; } = null!;

    public int Quantity { get; set; }
}