using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("InventoryMovements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .ValueGeneratedOnAdd();

        builder.Property(m => m.InventoryItemId)
            .IsRequired();

        builder.Property(m => m.Tipo)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Motivo)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(m => m.Quantidade)
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasPrecision(18, 4);

        builder.Property(m => m.ValorUnitario)
            .IsRequired()
            .HasColumnType("decimal(18,4)")
            .HasPrecision(18, 4);

        builder.Property(m => m.DataHora)
            .IsRequired()
            .HasDefaultValueSql("SYSDATETIME()");

        builder.Property(m => m.OrderId)
            .IsRequired(false);

        builder.Property(m => m.ComboId)
            .IsRequired(false);

        builder.Property(m => m.Fornecedor)
            .HasMaxLength(150)
            .IsRequired(false);

        builder.Property(m => m.Observacoes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.HasOne(m => m.InventoryItem)
            .WithMany(i => i.Movements)
            .HasForeignKey(m => m.InventoryItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(m => m.InventoryItemId);
        builder.HasIndex(m => m.DataHora);
        builder.HasIndex(m => m.OrderId);
        builder.HasIndex(m => m.ComboId);
        builder.HasIndex(m => m.Tipo);
        builder.HasIndex(m => m.Motivo);
    }
}