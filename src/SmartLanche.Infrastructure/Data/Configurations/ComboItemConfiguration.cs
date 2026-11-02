using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class ComboItemConfiguration : IEntityTypeConfiguration<ComboItem>
{
    public void Configure(EntityTypeBuilder<ComboItem> builder)
    {
        builder.ToTable("ComboItems");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Quantity)
            .IsRequired();

        builder.HasOne(ci => ci.Combo)
            .WithMany(c => c.ComboItems)
            .HasForeignKey(ci => ci.ComboId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.Product)
            .WithMany()
            .HasForeignKey(ci => ci.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ci => new { ci.ComboId, ci.ProductId }).IsUnique();
    }
}