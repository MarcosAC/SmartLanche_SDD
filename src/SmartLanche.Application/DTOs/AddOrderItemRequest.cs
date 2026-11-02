namespace SmartLanche.Application.DTOs;

public class AddOrderItemRequest
{
    public int OrderId { get; set; }

    public int ProductId { get; set; }

    public int Quantity { get; set; }

    public int? ComboId { get; set; }
}