namespace SmartLanche.Application.DTOs;

public class CreateOrderRequest
{
    public int? CustomerId { get; set; }
    public string? Observacoes { get; set; }
}