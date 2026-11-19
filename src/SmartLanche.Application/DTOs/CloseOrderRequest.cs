using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public class CloseOrderRequest
{
    public int OrderId { get; set; }
    public FormaPagamento? PaymentMethod { get; set; }
    public decimal? AmountPaid { get; set; }
    public int? Installments { get; set; }
    public string? Notes { get; set; }
    public int? CustomerId { get; set; }
}