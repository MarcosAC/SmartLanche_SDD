using SmartLanche.Application.Interfaces;

namespace SmartLanche.Application.DTOs;

public class CustomerDto : IEntityDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Notes { get; set; }

    public decimal CreditLimit { get; set; }

    public bool IsActive { get; set; }
}