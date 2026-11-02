using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Tests.Entities;

public class PromotionTests
{
    [Fact]
    public void IsValidOn_ActivePromotionWithinDateRange_ReturnsTrue()
    {
        var promotion = new Promotion
        {
            IsActive = true,
            StartDate = DateTime.Today.AddDays(-5),
            EndDate = DateTime.Today.AddDays(5)
        };

        Assert.True(promotion.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void IsValidOn_InactivePromotion_ReturnsFalse()
    {
        var promotion = new Promotion
        {
            IsActive = false,
            StartDate = DateTime.Today.AddDays(-5),
            EndDate = DateTime.Today.AddDays(5)
        };

        Assert.False(promotion.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void IsValidOn_BeforeStartDate_ReturnsFalse()
    {
        var promotion = new Promotion
        {
            IsActive = true,
            StartDate = DateTime.Today.AddDays(5),
            EndDate = DateTime.Today.AddDays(10)
        };

        Assert.False(promotion.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void IsValidOn_AfterEndDate_ReturnsFalse()
    {
        var promotion = new Promotion
        {
            IsActive = true,
            StartDate = DateTime.Today.AddDays(-10),
            EndDate = DateTime.Today.AddDays(-5)
        };

        Assert.False(promotion.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void IsValidOn_OnStartDate_ReturnsTrue()
    {
        var promotion = new Promotion
        {
            IsActive = true,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(5)
        };

        Assert.True(promotion.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void IsValidOn_OnEndDate_ReturnsTrue()
    {
        var promotion = new Promotion
        {
            IsActive = true,
            StartDate = DateTime.Today.AddDays(-5),
            EndDate = DateTime.Today
        };

        Assert.True(promotion.IsValidOn(DateTime.Today));
    }

    [Fact]
    public void IsValidOn_NoEndDate_RemainsValidIndefinitely()
    {
        var promotion = new Promotion
        {
            IsActive = true,
            StartDate = DateTime.Today.AddDays(-5),
            EndDate = null
        };

        Assert.True(promotion.IsValidOn(DateTime.Today));
        Assert.True(promotion.IsValidOn(DateTime.Today.AddYears(1)));
    }

    [Fact]
    public void CalculateDiscount_PercentageType_ReturnsCorrectDiscount()
    {
        var promotion = new Promotion
        {
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m
        };

        var discount = promotion.CalculateDiscount(20m);

        Assert.Equal(2m, discount);
    }

    [Fact]
    public void CalculateDiscount_FixedAmountType_ReturnsCorrectDiscount()
    {
        var promotion = new Promotion
        {
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5m
        };

        var discount = promotion.CalculateDiscount(12m);

        Assert.Equal(5m, discount);
    }

    [Fact]
    public void CalculateDiscount_FixedAmountCappedAtSubtotal_ReturnsSubtotal()
    {
        var promotion = new Promotion
        {
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5m
        };

        var discount = promotion.CalculateDiscount(3m);

        Assert.Equal(3m, discount);
    }

    [Fact]
    public void CalculateDiscount_PercentageOnOrderTotal_ReturnsCorrectDiscount()
    {
        var promotion = new Promotion
        {
            DiscountType = DiscountType.Percentage,
            DiscountValue = 15m
        };

        var discount = promotion.CalculateDiscount(50m);

        Assert.Equal(7.50m, discount);
    }

    [Fact]
    public void IsGlobal_NoProductsNoCategories_ReturnsTrue()
    {
        var promotion = new Promotion
        {
            ApplicableProducts = new List<SmartLanche.Domain.Entities.Product>(),
            ApplicableCategories = new List<SmartLanche.Domain.Entities.Category>()
        };

        Assert.True(promotion.IsGlobal);
    }

    [Fact]
    public void IsGlobal_WithApplicableProducts_ReturnsFalse()
    {
        var promotion = new Promotion
        {
            ApplicableProducts = new List<SmartLanche.Domain.Entities.Product> { new SmartLanche.Domain.Entities.Product() },
            ApplicableCategories = new List<SmartLanche.Domain.Entities.Category>()
        };

        Assert.False(promotion.IsGlobal);
    }

    [Fact]
    public void IsGlobal_WithApplicableCategories_ReturnsFalse()
    {
        var promotion = new Promotion
        {
            ApplicableProducts = new List<SmartLanche.Domain.Entities.Product>(),
            ApplicableCategories = new List<SmartLanche.Domain.Entities.Category> { new SmartLanche.Domain.Entities.Category() }
        };

        Assert.False(promotion.IsGlobal);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ThrowsArgumentException(string name)
    {
        var promotion = new Promotion { Name = name, DiscountValue = 10m, StartDate = DateTime.Today };

        Assert.Throws<ArgumentException>(() => promotion.Validate());
    }

    [Fact]
    public void Validate_WithZeroDiscountValue_ThrowsArgumentException()
    {
        var promotion = new Promotion { Name = "Test", DiscountValue = 0m, StartDate = DateTime.Today };

        Assert.Throws<ArgumentException>(() => promotion.Validate());
    }

    [Fact]
    public void Validate_WithNegativeDiscountValue_ThrowsArgumentException()
    {
        var promotion = new Promotion { Name = "Test", DiscountValue = -1m, StartDate = DateTime.Today };

        Assert.Throws<ArgumentException>(() => promotion.Validate());
    }

    [Fact]
    public void Validate_PercentageDiscountOver100_ThrowsArgumentException()
    {
        var promotion = new Promotion { Name = "Test", DiscountType = DiscountType.Percentage, DiscountValue = 101m, StartDate = DateTime.Today };

        Assert.Throws<ArgumentException>(() => promotion.Validate());
    }

    [Fact]
    public void Validate_EndDateBeforeStartDate_ThrowsArgumentException()
    {
        var promotion = new Promotion
        {
            Name = "Test",
            DiscountValue = 10m,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(-1)
        };

        Assert.Throws<ArgumentException>(() => promotion.Validate());
    }

    [Fact]
    public void Validate_NegativePriority_ThrowsArgumentException()
    {
        var promotion = new Promotion { Name = "Test", DiscountValue = 10m, StartDate = DateTime.Today, Priority = -1 };

        Assert.Throws<ArgumentException>(() => promotion.Validate());
    }

    [Fact]
    public void Validate_ValidPromotion_DoesNotThrow()
    {
        var promotion = new Promotion
        {
            Name = "Desconto 10%",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10m,
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(30),
            Priority = 5
        };

        var exception = Record.Exception(() => promotion.Validate());

        Assert.Null(exception);
    }
}