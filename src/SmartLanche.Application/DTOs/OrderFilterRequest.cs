namespace SmartLanche.Application.DTOs;

public class OrderFilterRequest
{
    public int? Status { get; set; }

    public int? CustomerId { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public string? SearchTerm { get; set; }

    public string? SortBy { get; set; }

    public bool SortDescending { get; set; } = true;

    public bool ExcludeCancelled { get; set; } = true;
}