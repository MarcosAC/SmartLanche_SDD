using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("Promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.Property(p => p.DiscountType)
            .IsRequired();

        builder.Property(p => p.DiscountValue)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(p => p.StartDate)
            .IsRequired()
            .HasColumnType("date");

        builder.Property(p => p.EndDate)
            .HasColumnType("date");

        builder.Property(p => p.IsActive)
            .IsRequired();

        builder.Property(p => p.Priority)
            .IsRequired();

        builder.HasIndex(p => new { p.IsActive, p.StartDate, p.EndDate });

        builder.HasMany(p => p.ApplicableProducts)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PromotionProducts",
                j => j.HasOne<Product>().WithMany().HasForeignKey("ProductId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Promotion>().WithMany().HasForeignKey("PromotionId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("PromotionId", "ProductId");
                    j.ToTable("PromotionProducts");
                });

        builder.HasMany(p => p.ApplicableCategories)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PromotionCategories",
                j => j.HasOne<Category>().WithMany().HasForeignKey("CategoryId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Promotion>().WithMany().HasForeignKey("PromotionId").OnDelete(DeleteBehavior.Cascade),
                j =>
                {
                    j.HasKey("PromotionId", "CategoryId");
                    j.ToTable("PromotionCategories");
                });
    }
}