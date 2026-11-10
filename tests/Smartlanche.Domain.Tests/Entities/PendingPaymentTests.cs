using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using Xunit;

namespace SmartLanche.Domain.Tests.Entities;

public class PendingPaymentTests
{
    [Fact]
    public void PendingPayment_SaldoPendente_CalculatesCorrectly()
    {
        var pending = new PendingPayment
        {
            ValorOriginal = 100m,
            ValorPago = 30m
        };

        Assert.Equal(70m, pending.SaldoPendente);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_PartialPayment_UpdatesSaldoAndStatus()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 0m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        pending.RegistrarPagamento(30m, FormaPagamento.Dinheiro, "Pagamento parcial");

        Assert.Equal(30m, pending.ValorPago);
        Assert.Equal(70m, pending.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.NaoPago, pending.StatusPagamento);
        Assert.Null(pending.DataPagamento);
        Assert.Single(pending.History);
        Assert.Equal(30m, pending.History.First().ValorPago);
        Assert.Equal(FormaPagamento.Dinheiro, pending.History.First().FormaPagamento);
        Assert.Equal("Pagamento parcial", pending.History.First().Notes);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_FullPayment_SetsStatusToPago()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 0m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        pending.RegistrarPagamento(30m, FormaPagamento.Dinheiro, "Parte 1");
        pending.RegistrarPagamento(70m, FormaPagamento.Pix, "Quitacao total");

        Assert.Equal(100m, pending.ValorPago);
        Assert.Equal(0m, pending.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.Pago, pending.StatusPagamento);
        Assert.NotNull(pending.DataPagamento);
        Assert.Equal(2, pending.History.Count);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_MultiplePayments_AccumulatesCorrectly()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 0m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        pending.RegistrarPagamento(25m, FormaPagamento.Dinheiro, "Parte 1");
        pending.RegistrarPagamento(25m, FormaPagamento.Cartao, "Parte 2");
        pending.RegistrarPagamento(50m, FormaPagamento.Pix, "Parte 3");

        Assert.Equal(100m, pending.ValorPago);
        Assert.Equal(0m, pending.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.Pago, pending.StatusPagamento);
        Assert.Equal(3, pending.History.Count);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_ExceedsSaldo_ThrowsException()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 80m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        var ex = Assert.Throws<ArgumentException>(() => pending.RegistrarPagamento(30m, FormaPagamento.Dinheiro));
        Assert.Contains("excede saldo pendente", ex.Message);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_ZeroValue_ThrowsException()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 0m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        var ex = Assert.Throws<ArgumentException>(() => pending.RegistrarPagamento(0m, FormaPagamento.Dinheiro));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_NegativeValue_ThrowsException()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 0m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        var ex = Assert.Throws<ArgumentException>(() => pending.RegistrarPagamento(-10m, FormaPagamento.Dinheiro));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void PendingPayment_RegistrarPagamento_AlreadyPaid_ThrowsException()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 100m,
            StatusPagamento = StatusPagamentoPendente.Pago,
            DataPagamento = DateTime.Now
        };

        var ex = Assert.Throws<InvalidOperationException>(() => pending.RegistrarPagamento(10m, FormaPagamento.Dinheiro));
        Assert.Contains("já está quitada", ex.Message);
    }

    [Fact]
    public void PendingPayment_Cancelar_NotPaid_SetsStatusToNaoPago()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 30m,
            StatusPagamento = StatusPagamentoPendente.NaoPago,
            Notes = "Original"
        };

        pending.Cancelar("Cliente desistiu");

        Assert.Equal(StatusPagamentoPendente.NaoPago, pending.StatusPagamento);
        Assert.Contains("CANCELADA: Cliente desistiu", pending.Notes);
        Assert.Contains("Original", pending.Notes);
    }

    [Fact]
    public void PendingPayment_Cancelar_AlreadyPaid_ThrowsException()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 100m,
            StatusPagamento = StatusPagamentoPendente.Pago,
            DataPagamento = DateTime.Now
        };

        var ex = Assert.Throws<InvalidOperationException>(() => pending.Cancelar("Tentativa de cancelar"));
        Assert.Contains("Não é possível cancelar", ex.Message);
    }

    [Fact]
    public void PendingPayment_Cancelar_WhenPartiallyPaid_ResetsToNotPaid()
    {
        var pending = new PendingPayment
        {
            Id = 1,
            ValorOriginal = 100m,
            ValorPago = 50m,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        };

        pending.Cancelar("Acordo cancelado");

        Assert.Equal(StatusPagamentoPendente.NaoPago, pending.StatusPagamento);
        Assert.Contains("Acordo cancelado", pending.Notes);
    }
}