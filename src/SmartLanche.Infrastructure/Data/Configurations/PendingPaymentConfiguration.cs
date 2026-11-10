using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Infrastructure.Data.Configurations;

public class PendingPaymentConfiguration : IEntityTypeConfiguration<PendingPayment>
{
    public void Configure(EntityTypeBuilder<PendingPayment> builder)
    {
        builder.ToTable("PendingPayments");

        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.ValorOriginal)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(pp => pp.ValorPago)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(pp => pp.DataHora)
            .IsRequired();

        builder.Property(pp => pp.StatusPagamento)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(pp => pp.DataPagamento)
            .IsRequired(false);

        builder.Property(pp => pp.Notes)
            .HasMaxLength(1000);

        builder.HasOne(pp => pp.Customer)
            .WithMany(c => c.PendingPayments)
            .HasForeignKey(pp => pp.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pp => pp.Order)
            .WithOne(o => o.PendingPayment)
            .HasForeignKey<PendingPayment>(pp => pp.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(pp => pp.History)
            .WithOne(h => h.PendingPayment)
            .HasForeignKey(h => h.PendingPaymentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pp => pp.CustomerId);
        builder.HasIndex(pp => pp.OrderId).IsUnique();
        builder.HasIndex(pp => pp.StatusPagamento);
    }
}

public class PendingPaymentHistoryConfiguration : IEntityTypeConfiguration<PendingPaymentHistory>
{
    public void Configure(EntityTypeBuilder<PendingPaymentHistory> builder)
    {
        builder.ToTable("PendingPaymentHistory");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.ValorPago)
            .IsRequired()
            .HasColumnType("decimal(18,2)")
            .HasPrecision(18, 2);

        builder.Property(h => h.DataHora)
            .IsRequired();

        builder.Property(h => h.FormaPagamento)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(h => h.Notes)
            .HasMaxLength(500);

        builder.HasOne(h => h.PendingPayment)
            .WithMany(pp => pp.History)
            .HasForeignKey(h => h.PendingPaymentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}