using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IOrderRepository
{
    Task<IReadOnlyList<Order>> GetAllAsync(
        int? status = null,
        int? customerId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    Task UpdateAsync(Order order, CancellationToken cancellationToken = default);

    Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default);

    Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default);
}