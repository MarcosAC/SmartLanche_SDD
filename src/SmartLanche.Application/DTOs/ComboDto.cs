using SmartLanche.Application.Interfaces;

namespace SmartLanche.Application.DTOs;

public class ComboDto : IEntityDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; }

    public bool IsAvailable { get; set; }

    public IReadOnlyList<ComboItemDto> Items { get; set; } = [];
}