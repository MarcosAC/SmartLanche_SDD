using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");

        builder.HasKey(customer => customer.Id);

        builder.Property(customer => customer.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(customer => customer.Phone)
            .HasMaxLength(20);

        builder.Property(customer => customer.Notes)
            .HasMaxLength(500);

        builder.Property(customer => customer.CreditLimit)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2)
            .HasDefaultValue(0m);

        builder.Property(customer => customer.IsActive)
            .IsRequired();

        builder.HasMany(customer => customer.Orders)
            .WithOne(order => order.Customer)
            .HasForeignKey(order => order.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}