using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.DTOs;

public class UpdateComboRequest : IEntityId
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public IReadOnlyList<CreateComboItemRequest> Items { get; set; } = [];
}