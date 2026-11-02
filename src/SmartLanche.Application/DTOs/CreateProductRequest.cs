namespace SmartLanche.Application.DTOs;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public bool IsCombo { get; set; }

    public string? Description { get; set; }

    public CreateInventoryItemRequest? InventoryRequest { get; set; }
}