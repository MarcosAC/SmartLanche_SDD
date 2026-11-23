using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public class CloseOrderRequest
{
    public int OrderId { get; set; }
    public FormaPagamento? FormaPagamento { get; set; }
    public decimal? ValorPago { get; set; }
    public int? Parcelas { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
}