using SmartLanche.Domain.Entities;

namespace SmartLanche.Domain.Tests.Entities;

public class ComboTests
{
    [Fact]
    public void IsAvailable_AllComponentsActive_ReturnsTrue()
    {
        var product1 = new Product { IsActive = true };
        var product2 = new Product { IsActive = true };

        var combo = new Combo
        {
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = product1, Quantity = 1 },
                new ComboItem { Product = product2, Quantity = 1 }
            }
        };

        Assert.True(combo.IsAvailable);
    }

    [Fact]
    public void IsAvailable_OneComponentInactive_ReturnsFalse()
    {
        var product1 = new Product { IsActive = true };
        var product2 = new Product { IsActive = false };

        var combo = new Combo
        {
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = product1, Quantity = 1 },
                new ComboItem { Product = product2, Quantity = 1 }
            }
        };

        Assert.False(combo.IsAvailable);
    }

    [Fact]
    public void IsAvailable_AllComponentsInactive_ReturnsFalse()
    {
        var product1 = new Product { IsActive = false };
        var product2 = new Product { IsActive = false };

        var combo = new Combo
        {
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = product1, Quantity = 1 },
                new ComboItem { Product = product2, Quantity = 1 }
            }
        };

        Assert.False(combo.IsAvailable);
    }

    [Fact]
    public void GetComponents_ReturnsProductAndQuantity()
    {
        var product1 = new Product { Name = "X-Burguer", IsActive = true };
        var product2 = new Product { Name = "Batata Frita", IsActive = true };

        var combo = new Combo
        {
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = product1, Quantity = 1 },
                new ComboItem { Product = product2, Quantity = 2 }
            }
        };

        var components = combo.GetComponents().ToList();

        Assert.Equal(2, components.Count);
        Assert.Equal("X-Burguer", components[0].Product.Name);
        Assert.Equal(1, components[0].Quantity);
        Assert.Equal("Batata Frita", components[1].Product.Name);
        Assert.Equal(2, components[1].Quantity);
    }

    [Fact]
    public void GetComponents_SkipsNullProducts()
    {
        var product1 = new Product { Name = "X-Burguer", IsActive = true };

        var combo = new Combo
        {
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = product1, Quantity = 1 },
                new ComboItem { Product = null!, Quantity = 1 }
            }
        };

        var components = combo.GetComponents().ToList();

        Assert.Single(components);
        Assert.Equal("X-Burguer", components[0].Product.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ThrowsArgumentException(string name)
    {
        var combo = new Combo
        {
            Name = name,
            Price = 25m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 },
                new ComboItem { Product = new Product(), Quantity = 1 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithZeroPrice_ThrowsArgumentException()
    {
        var combo = new Combo
        {
            Name = "Test Combo",
            Price = 0m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 },
                new ComboItem { Product = new Product(), Quantity = 1 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithNegativePrice_ThrowsArgumentException()
    {
        var combo = new Combo
        {
            Name = "Test Combo",
            Price = -1m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 },
                new ComboItem { Product = new Product(), Quantity = 1 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithSingleItem_ThrowsArgumentException()
    {
        var combo = new Combo
        {
            Name = "Test Combo",
            Price = 25m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithNullProductInItem_ThrowsArgumentException()
    {
        var combo = new Combo
        {
            Name = "Test Combo",
            Price = 25m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 },
                new ComboItem { Product = null!, Quantity = 1 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithZeroQuantityInItem_ThrowsArgumentException()
    {
        var combo = new Combo
        {
            Name = "Test Combo",
            Price = 25m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 },
                new ComboItem { Product = new Product(), Quantity = 0 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithNegativeQuantityInItem_ThrowsArgumentException()
    {
        var combo = new Combo
        {
            Name = "Test Combo",
            Price = 25m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product(), Quantity = 1 },
                new ComboItem { Product = new Product(), Quantity = -1 }
            }
        };

        Assert.Throws<ArgumentException>(() => combo.Validate());
    }

    [Fact]
    public void Validate_WithValidCombo_DoesNotThrow()
    {
        var combo = new Combo
        {
            Name = "X-Burguer + Batata + Refrigerante",
            Price = 25m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = new Product { IsActive = true }, Quantity = 1 },
                new ComboItem { Product = new Product { IsActive = true }, Quantity = 1 },
                new ComboItem { Product = new Product { IsActive = true }, Quantity = 1 }
            }
        };

        var exception = Record.Exception(() => combo.Validate());

        Assert.Null(exception);
    }

    [Fact]
    public void Validate_AllowsDuplicateProductsInSameCombo()
    {
        var product = new Product { IsActive = true };

        var combo = new Combo
        {
            Name = "Duplo X-Burguer",
            Price = 30m,
            ComboItems = new List<ComboItem>
            {
                new ComboItem { Product = product, Quantity = 2 },
                new ComboItem { Product = new Product { IsActive = true }, Quantity = 1 }
            }
        };

        var exception = Record.Exception(() => combo.Validate());

        Assert.Null(exception);
    }
}