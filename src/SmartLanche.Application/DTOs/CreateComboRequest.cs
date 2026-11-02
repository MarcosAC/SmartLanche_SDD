namespace SmartLanche.Application.DTOs;

public class CreateComboRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public IReadOnlyList<CreateComboItemRequest> Items { get; set; } = [];
}