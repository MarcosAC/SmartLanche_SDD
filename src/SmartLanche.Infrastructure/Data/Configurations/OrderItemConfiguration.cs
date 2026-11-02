using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity)
            .IsRequired();

        builder.Property(item => item.UnitPrice)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(item => item.Subtotal)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2)
            .HasComputedColumnSql("[Quantity] * [UnitPrice]");

        builder.Property(item => item.OrderId)
            .IsRequired();

        builder.Property(item => item.ProductId)
            .IsRequired();

        builder.HasOne(item => item.Order)
            .WithMany(order => order.OrderItems)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(item => item.Product)
            .WithMany()
            .HasForeignKey(item => item.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}