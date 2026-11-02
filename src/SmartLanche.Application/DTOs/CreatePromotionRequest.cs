using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public class CreatePromotionRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int Priority { get; set; }

    public IReadOnlyList<int> ApplicableProductIds { get; set; } = [];

    public IReadOnlyList<int> ApplicableCategoryIds { get; set; } = [];
}