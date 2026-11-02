namespace SmartLanche.Domain.Entities;

public class Product : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public Category Category { get; set; } = null!;

    public decimal Price { get; set; }

    public bool IsCombo { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}