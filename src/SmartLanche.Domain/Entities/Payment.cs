using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Entities;

public class Payment
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public FormaPagamento FormaPagamento { get; set; }

    public decimal ValorPago { get; set; }

    public decimal Troco { get; set; }

    public int Parcelas { get; set; } = 1;

    public decimal ValorParcela => Parcelas > 0 ? Math.Round(ValorPago / Parcelas, 2, MidpointRounding.AwayFromZero) : 0;

    public DateTime DataHora { get; set; } = DateTime.Now;

    public string? Notes { get; set; }

    public bool Estornado { get; set; }

    public DateTime? EstornoDataHora { get; set; }

    public string? EstornoMotivo { get; set; }

    public void Estornar(string motivo)
    {
        if (Estornado)
        {
            throw new InvalidOperationException("Pagamento já foi estornado.");
        }

        Estornado = true;
        EstornoDataHora = DateTime.Now;
        EstornoMotivo = motivo;
    }

    public void Validate(decimal orderTotal)
    {
        if (ValorPago <= 0)
        {
            throw new ArgumentException("Valor pago deve ser maior que zero.", nameof(ValorPago));
        }

        if (Parcelas < 1)
        {
            throw new ArgumentException("Parcelas deve ser maior ou igual a 1.", nameof(Parcelas));
        }

        if (Parcelas > 12)
        {
            throw new ArgumentException("Parcelas não pode exceder 12.", nameof(Parcelas));
        }

        switch (FormaPagamento)
        {
            case FormaPagamento.Dinheiro:
                if (ValorPago < orderTotal)
                {
                    throw new ArgumentException($"Valor pago (R$ {ValorPago:N2}) insuficiente. Total do pedido: R$ {orderTotal:N2}.", nameof(ValorPago));
                }
                Troco = ValorPago - orderTotal;
                break;

            case FormaPagamento.Cartao:
            case FormaPagamento.Pix:
                if (ValorPago != orderTotal)
                {
                    throw new ArgumentException($"Para {FormaPagamento}, valor pago deve ser igual ao total do pedido (R$ {orderTotal:N2}).", nameof(ValorPago));
                }
                Troco = 0;
                break;

            case FormaPagamento.Fiado:
                // Fiado usa o total do pedido para criar a pendência
                // ValorPago não é usado no momento do fechamento
                Troco = 0;
                break;
        }
    }
}