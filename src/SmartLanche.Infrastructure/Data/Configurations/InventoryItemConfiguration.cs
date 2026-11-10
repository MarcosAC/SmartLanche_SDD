using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .ValueGeneratedOnAdd();

        builder.Property(i => i.ProductId)
            .IsRequired(false);

        builder.HasIndex(i => i.ProductId)
            .IsUnique()
            .HasFilter("[ProductId] IS NOT NULL");

        builder.Property(i => i.Nome)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(i => i.QuantidadeAtual)
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        builder.Property(i => i.QuantidadeMinima)
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        builder.Property(i => i.ValorUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasPrecision(18, 4)
            .HasDefaultValue(0);

        builder.Property(i => i.UnidadeMedida)
            .IsRequired()
            .HasMaxLength(20)
            .HasDefaultValue("UN");

        builder.Property(i => i.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne(i => i.Produto)
            .WithOne()
            .HasForeignKey<InventoryItem>(i => i.ProductId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.Movements)
            .WithOne(m => m.InventoryItem)
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(i => i.EstoqueBaixo);
        builder.Ignore(i => i.EstoqueZerado);
        builder.Ignore(i => i.ValorTotalEstoque);
    }
}