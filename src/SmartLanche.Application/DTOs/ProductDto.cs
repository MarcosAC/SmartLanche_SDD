using SmartLanche.Application.Interfaces;

namespace SmartLanche.Application.DTOs;

public class ProductDto : IEntityDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public decimal Price { get; set; }

    public bool IsCombo { get; set; }

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}