using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(category => category.Id);

        builder.Property(category => category.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(category => category.Description)
            .HasMaxLength(500);

        builder.Property(category => category.IsActive)
            .IsRequired();
    }
}