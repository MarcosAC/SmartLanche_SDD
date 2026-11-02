using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.OrderDate)
            .IsRequired();

        builder.Property(order => order.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(order => order.TotalAmount)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(order => order.Notes)
            .HasMaxLength(500);

        builder.Property(order => order.PreparationStartedAt)
            .IsRequired(false);

        builder.Property(order => order.DeliveredAt)
            .IsRequired(false);

        builder.Property(order => order.CancelledAt)
            .IsRequired(false);

        builder.Property(order => order.CustomerId)
            .IsRequired(false);

        builder.HasOne(order => order.Customer)
            .WithMany(customer => customer.Orders)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(order => order.OrderItems)
            .WithOne(item => item.Order)
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(order => order.Status);
        builder.HasIndex(order => order.PreparationStartedAt);
    }
}