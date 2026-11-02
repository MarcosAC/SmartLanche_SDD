using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using Xunit;

namespace SmartLanche.Domain.Tests.Entities;

public class PaymentTests
{
    [Fact]
    public void Payment_Dinheiro_WithSufficientAmount_CalculatesTroco()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Dinheiro,
            ValorPago = 50m,
            Parcelas = 1
        };

        payment.Validate(35m);

        Assert.Equal(15m, payment.Troco);
        Assert.False(payment.Estornado);
    }

    [Fact]
    public void Payment_Dinheiro_WithInsufficientAmount_ThrowsException()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Dinheiro,
            ValorPago = 20m,
            Parcelas = 1
        };

        var ex = Assert.Throws<ArgumentException>(() => payment.Validate(35m));
        Assert.Contains("insuficiente", ex.Message);
    }

    [Fact]
    public void Payment_Cartao_WithExactAmount_Succeeds()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 35m,
            Parcelas = 1
        };

        payment.Validate(35m);

        Assert.Equal(0, payment.Troco);
    }

    [Fact]
    public void Payment_Cartao_WithDifferentAmount_ThrowsException()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 40m,
            Parcelas = 1
        };

        var ex = Assert.Throws<ArgumentException>(() => payment.Validate(35m));
        Assert.Contains("igual ao total", ex.Message);
    }

    [Fact]
    public void Payment_Pix_WithExactAmount_Succeeds()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Pix,
            ValorPago = 35m,
            Parcelas = 1
        };

        payment.Validate(35m);

        Assert.Equal(0, payment.Troco);
    }

    [Fact]
    public void Payment_Fiado_SetsTrocoToZero()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Fiado,
            ValorPago = 35m,
            Parcelas = 1
        };

        payment.Validate(35m);

        Assert.Equal(0, payment.Troco);
    }

    [Fact]
    public void Payment_Parcelas_CalculatesValorParcelaCorrectly()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 120m,
            Parcelas = 3
        };

        payment.Validate(120m);

        Assert.Equal(40m, payment.ValorParcela);
    }

    [Fact]
    public void Payment_Parcelas_TooMany_ThrowsException()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 120m,
            Parcelas = 13
        };

        var ex = Assert.Throws<ArgumentException>(() => payment.Validate(120m));
        Assert.Contains("exceder 12", ex.Message);
    }

    [Fact]
    public void Payment_Parcelas_ZeroOrNegative_ThrowsException()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 120m,
            Parcelas = 0
        };

        var ex = Assert.Throws<ArgumentException>(() => payment.Validate(120m));
        Assert.Contains("maior ou igual a 1", ex.Message);
    }

    [Fact]
    public void Payment_ValorPago_ZeroOrNegative_ThrowsException()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 0,
            Parcelas = 1
        };

        var ex = Assert.Throws<ArgumentException>(() => payment.Validate(35m));
        Assert.Contains("maior que zero", ex.Message);
    }

    [Fact]
    public void Payment_Estornar_Success()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 35m,
            Parcelas = 1
        };

        payment.Validate(35m);
        payment.Estornar("Cancelado pelo cliente");

        Assert.True(payment.Estornado);
        Assert.NotNull(payment.EstornoDataHora);
        Assert.Equal("Cancelado pelo cliente", payment.EstornoMotivo);
    }

    [Fact]
    public void Payment_Estornar_AlreadyEstornado_ThrowsException()
    {
        var payment = new Payment
        {
            FormaPagamento = FormaPagamento.Cartao,
            ValorPago = 35m,
            Parcelas = 1
        };

        payment.Validate(35m);
        payment.Estornar("Primeiro estorno");

        var ex = Assert.Throws<InvalidOperationException>(() => payment.Estornar("Segundo estorno"));
        Assert.Contains("já foi estornado", ex.Message);
    }
}