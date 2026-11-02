namespace SmartLanche.Application.DTOs;

public class UpdateOrderItemRequest
{
    public int OrderId { get; set; }

    public int OrderItemId { get; set; }

    public int Quantity { get; set; }
}