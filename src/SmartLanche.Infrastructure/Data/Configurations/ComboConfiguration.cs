using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class ComboConfiguration : IEntityTypeConfiguration<Combo>
{
    public void Configure(EntityTypeBuilder<Combo> builder)
    {
        builder.ToTable("Combos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(c => c.Description)
            .HasMaxLength(500);

        builder.Property(c => c.Price)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(c => c.IsActive)
            .IsRequired();

        builder.HasMany(c => c.ComboItems)
            .WithOne(ci => ci.Combo)
            .HasForeignKey(ci => ci.ComboId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}