using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderFilterRequest? filter = null, CancellationToken cancellationToken = default);

    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> AddItemAsync(AddOrderItemRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> RemoveItemAsync(int orderId, int orderItemId, CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateItemQuantityAsync(UpdateOrderItemRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> CloseAsync(CloseOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> StartPreparationAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto> MarkAsReadyAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto> FinalizeAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderDto> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken = default);

    Task<OrderDto> RecalculateWithPromotionsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionDto>> GetAppliedPromotionsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductSalesDto>> GetTopProductsAsync(int topCount = 5, int days = 7, CancellationToken cancellationToken = default);
}