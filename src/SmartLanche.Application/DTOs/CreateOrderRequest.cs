namespace SmartLanche.Application.DTOs;

public class CreateOrderRequest
{
    public int? CustomerId { get; set; }
    public string? Notes { get; set; }
}