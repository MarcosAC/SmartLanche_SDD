namespace SmartLanche.Application.DTOs;

public class CreateCustomerRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public decimal CreditLimit { get; set; }
}