using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.DTOs;

public class UpdateCustomerRequest : IEntityId
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; }
}