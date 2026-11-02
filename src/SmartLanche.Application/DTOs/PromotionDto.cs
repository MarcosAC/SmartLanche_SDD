using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public class PromotionDto : IEntityDto
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

    public bool IsGlobal => !ApplicableProductIds.Any() && !ApplicableCategoryIds.Any();

    public bool IsCurrentlyValid => IsActive && StartDate.Date <= DateTime.Today && (!EndDate.HasValue || EndDate.Value.Date >= DateTime.Today);

    public decimal CalculateDiscount(decimal amount)
    {
        if (DiscountType == DiscountType.Percentage)
        {
            var discount = amount * DiscountValue / 100m;
            return Math.Round(Math.Min(discount, amount), 2, MidpointRounding.AwayFromZero);
        }
        else // FixedAmount
        {
            return Math.Min(DiscountValue, amount);
        }
    }
}