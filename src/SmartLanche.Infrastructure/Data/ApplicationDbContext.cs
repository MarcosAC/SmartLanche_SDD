using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Order> Orders => Set<Order>();

    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<Combo> Combos => Set<Combo>();

    public DbSet<ComboItem> ComboItems => Set<ComboItem>();

    public DbSet<Payment> Payments => Set<Payment>();

    public DbSet<PendingPayment> PendingPayments => Set<PendingPayment>();

    public DbSet<PendingPaymentHistory> PendingPaymentHistory => Set<PendingPaymentHistory>();

    public DbSet<InventoryItem> InventoryItems => Set<InventoryItem>();

    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Global query filters for soft delete (IsActive)
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IEntity).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = Expression.Parameter(entityType.ClrType, "e");
                var property = Expression.Property(parameter, nameof(IEntity.IsActive));
                var lambda = Expression.Lambda(property, parameter);
                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }
    }
}