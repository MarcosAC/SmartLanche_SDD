using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Tests;

public class EntityMappingTests
{
    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(@"Server=(localdb)\MSSQLLocalDB;Database=SmartLancheMappingTests;Trusted_Connection=True;TrustServerCertificate=True").Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public void Category_ShouldHavePrimaryKey()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Category));

        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Category.Id), primaryKey.Properties.Single().Name);
    }

    [Fact]
    public void Product_ShouldHavePrimaryKey()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Product));

        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Product.Id), primaryKey.Properties.Single().Name);
    }

    [Fact]
    public void Customer_ShouldHavePrimaryKey()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Customer));

        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Customer.Id), primaryKey.Properties.Single().Name);
    }

    [Fact]
    public void Order_ShouldHavePrimaryKey()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Order));

        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(Order.Id), primaryKey.Properties.Single().Name);
    }

    [Fact]
    public void OrderItem_ShouldHavePrimaryKey()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(OrderItem));

        Assert.NotNull(entity);

        var primaryKey = entity.FindPrimaryKey();

        Assert.NotNull(primaryKey);
        Assert.Equal(nameof(OrderItem.Id), primaryKey.Properties.Single().Name);
    }

    [Fact]
    public void Product_ShouldHaveCategoryForeignKey()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Product));

        Assert.NotNull(entity);

        var foreignKey = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(property => property.Name == nameof(Product.CategoryId)));

        Assert.NotNull(foreignKey);
        Assert.Equal(typeof(Category), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void Order_ShouldHaveCustomerForeignKey_WithRestrict()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Order));

        Assert.NotNull(entity);

        var foreignKey = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(property => property.Name == nameof(Order.CustomerId)));

        Assert.NotNull(foreignKey);
        Assert.Equal(typeof(Customer), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void OrderItem_ShouldHaveOrderForeignKey_WithCascade()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(OrderItem));

        Assert.NotNull(entity);

        var foreignKey = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(property => property.Name == nameof(OrderItem.OrderId)));

        Assert.NotNull(foreignKey);
        Assert.Equal(typeof(Order), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void OrderItem_ShouldHaveProductForeignKey_WithRestrict()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(OrderItem));

        Assert.NotNull(entity);

        var foreignKey = entity.GetForeignKeys().SingleOrDefault(fk => fk.Properties.Any(property => property.Name == nameof(OrderItem.ProductId)));

        Assert.NotNull(foreignKey);
        Assert.Equal(typeof(Product), foreignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior);
    }

    [Fact]
    public void ProductPrice_ShouldUsePrecision18Scale2()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Product));

        Assert.NotNull(entity);

        var price = entity.FindProperty(nameof(Product.Price));

        Assert.NotNull(price);
        Assert.Equal(18, price.GetPrecision());
        Assert.Equal(2, price.GetScale());
    }

    [Fact]
    public void CustomerCreditLimit_ShouldUsePrecision18Scale2()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Customer));

        Assert.NotNull(entity);

        var creditLimit = entity.FindProperty(nameof(Customer.CreditLimit));

        Assert.NotNull(creditLimit);
        Assert.Equal(18, creditLimit.GetPrecision());
        Assert.Equal(2, creditLimit.GetScale());
    }

    [Fact]
    public void OrderTotalAmount_ShouldUsePrecision18Scale2()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Order));

        Assert.NotNull(entity);

        var totalAmount = entity.FindProperty(nameof(Order.TotalAmount));

        Assert.NotNull(totalAmount);
        Assert.Equal(18, totalAmount.GetPrecision());
        Assert.Equal(2, totalAmount.GetScale());
    }

    [Fact]
    public void OrderItemUnitPrice_ShouldUsePrecision18Scale2()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(OrderItem));

        Assert.NotNull(entity);

        var unitPrice = entity.FindProperty(nameof(OrderItem.UnitPrice));

        Assert.NotNull(unitPrice);
        Assert.Equal(18, unitPrice.GetPrecision());
        Assert.Equal(2, unitPrice.GetScale());
    }

    [Fact]
    public void OrderItemSubtotal_ShouldUsePrecision18Scale2()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(OrderItem));

        Assert.NotNull(entity);

        var subtotal = entity.FindProperty(nameof(OrderItem.Subtotal));

        Assert.NotNull(subtotal);
        Assert.Equal(18, subtotal.GetPrecision());
        Assert.Equal(2, subtotal.GetScale());
    }

    [Fact]
    public void ProductName_ShouldHaveMaximumLength150()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Product));

        Assert.NotNull(entity);

        var name = entity.FindProperty(nameof(Product.Name));

        Assert.NotNull(name);
        Assert.Equal(150, name.GetMaxLength());
    }

    [Fact]
    public void CategoryName_ShouldHaveMaximumLength100()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Category));

        Assert.NotNull(entity);

        var name = entity.FindProperty(nameof(Category.Name));

        Assert.NotNull(name);
        Assert.Equal(100, name.GetMaxLength());
    }

    [Fact]
    public void CustomerName_ShouldHaveMaximumLength150()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Customer));

        Assert.NotNull(entity);

        var name = entity.FindProperty(nameof(Customer.Name));

        Assert.NotNull(name);
        Assert.Equal(150, name.GetMaxLength());
    }

    [Fact]
    public void CustomerPhone_ShouldHaveMaximumLength20()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Customer));

        Assert.NotNull(entity);

        var phone = entity.FindProperty(nameof(Customer.Phone));

        Assert.NotNull(phone);
        Assert.Equal(20, phone.GetMaxLength());
    }

    [Fact]
    public void CustomerNotes_ShouldHaveMaximumLength500()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Customer));

        Assert.NotNull(entity);

        var notes = entity.FindProperty(nameof(Customer.Notes));

        Assert.NotNull(notes);
        Assert.Equal(500, notes.GetMaxLength());
    }

    [Fact]
    public void OrderObservacoes_ShouldHaveMaximumLength500()
    {
        using var context = CreateContext();

        var entity = context.Model.FindEntityType(typeof(Order));

        Assert.NotNull(entity);

        var notes = entity.FindProperty(nameof(Order.Observacoes));

        Assert.NotNull(notes);
        Assert.Equal(500, notes.GetMaxLength());
    }
}
