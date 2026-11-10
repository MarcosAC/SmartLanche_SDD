using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Entities;

public class PendingPayment
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    public decimal ValorOriginal { get; set; }

    public decimal ValorPago { get; set; }

    public decimal SaldoPendente => ValorOriginal - ValorPago;

    public DateTime DataHora { get; set; } = DateTime.Now;

    public StatusPagamentoPendente StatusPagamento { get; set; } = StatusPagamentoPendente.NaoPago;

    public DateTime? DataPagamento { get; set; }

    public string? Notes { get; set; }

    public ICollection<PendingPaymentHistory> History { get; set; } = new List<PendingPaymentHistory>();

    public void RegistrarPagamento(decimal valor, FormaPagamento formaPagamento, string? observacoes = null)
    {
        if (StatusPagamento == StatusPagamentoPendente.Pago)
        {
            throw new InvalidOperationException("Pendência já está quitada.");
        }

        if (valor <= 0)
        {
            throw new ArgumentException("Valor do pagamento deve ser maior que zero.", nameof(valor));
        }

        if (valor > SaldoPendente)
        {
            throw new ArgumentException($"Valor (R$ {valor:N2}) excede saldo pendente (R$ {SaldoPendente:N2}).", nameof(valor));
        }

        ValorPago += valor;

        var history = new PendingPaymentHistory
        {
            PendingPaymentId = Id,
            ValorPago = valor,
            DataHora = DateTime.Now,
            FormaPagamento = formaPagamento,
            Notes = observacoes
        };

        History.Add(history);

        if (SaldoPendente <= 0)
        {
            StatusPagamento = StatusPagamentoPendente.Pago;
            DataPagamento = DateTime.Now;
        }
    }

    public void Cancelar(string motivo)
    {
        if (StatusPagamento == StatusPagamentoPendente.Pago)
        {
            throw new InvalidOperationException("Não é possível cancelar pendência já paga.");
        }

        StatusPagamento = StatusPagamentoPendente.NaoPago;
        Notes = $"CANCELADA: {motivo}. {Notes}".Trim();
    }
}

public class PendingPaymentHistory
{
    public int Id { get; set; }

    public int PendingPaymentId { get; set; }

    public PendingPayment? PendingPayment { get; set; }

    public decimal ValorPago { get; set; }

    public DateTime DataHora { get; set; }

    public FormaPagamento FormaPagamento { get; set; }

    public string? Notes { get; set; }
}