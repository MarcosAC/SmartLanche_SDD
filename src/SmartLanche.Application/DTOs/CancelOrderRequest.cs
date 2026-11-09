namespace SmartLanche.Application.DTOs;

public class CancelOrderRequest
{
    public int OrderId { get; set; }

    /// <summary>
    /// If true, Pendente orders will be deleted from database (used by OrderFormViewModel).
    /// If false, orders will be marked as Cancelado but kept in database (used by OrderListViewModel).
    /// </summary>
    public bool DeleteIfPendente { get; set; } = false;
}