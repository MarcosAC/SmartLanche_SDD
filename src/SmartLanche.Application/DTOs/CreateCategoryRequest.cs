namespace SmartLanche.Application.DTOs;

public class CreateCategoryRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
}