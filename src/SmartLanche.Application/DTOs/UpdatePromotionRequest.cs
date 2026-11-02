using SmartLanche.Domain.Enums;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.DTOs;

public class UpdatePromotionRequest : IEntityId
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; }

    public int Priority { get; set; }

    public IReadOnlyList<int> ApplicableProductIds { get; set; } = [];

    public IReadOnlyList<int> ApplicableCategoryIds { get; set; } = [];
}