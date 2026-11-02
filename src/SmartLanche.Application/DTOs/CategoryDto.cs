using SmartLanche.Application.Interfaces;

namespace SmartLanche.Application.DTOs;

public class CategoryDto : IEntityDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}