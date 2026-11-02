using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.DTOs;

public class UpdateProductRequest : IEntityId
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int CategoryId { get; set; }

    public decimal Price { get; set; }

    public bool IsCombo { get; set; }

    public string? Description { get; set; }
}