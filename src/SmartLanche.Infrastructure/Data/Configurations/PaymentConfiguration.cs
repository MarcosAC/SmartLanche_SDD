using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(payment => payment.Id);

        builder.Property(payment => payment.FormaPagamento)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(payment => payment.ValorPago)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(payment => payment.Troco)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(payment => payment.Parcelas)
            .IsRequired();

        builder.Property(payment => payment.DataHora)
            .IsRequired();

        builder.Property(payment => payment.Observacoes)
            .HasMaxLength(500);

        builder.Property(payment => payment.Estornado)
            .IsRequired();

        builder.Property(payment => payment.EstornoDataHora)
            .IsRequired(false);

        builder.Property(payment => payment.EstornoMotivo)
            .HasMaxLength(500);

        builder.HasOne(payment => payment.Order)
            .WithMany(order => order.Payments)
            .HasForeignKey(payment => payment.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}