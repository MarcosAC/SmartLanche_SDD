using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.DTOs;

public class UpdateCategoryRequest : IEntityId
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public bool IsActive { get; set; }
}