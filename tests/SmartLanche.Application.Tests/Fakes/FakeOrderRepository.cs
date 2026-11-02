using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakeOrderRepository : FakeRepositoryBase<Order>, IOrderRepository
{
    private readonly FakeCustomerRepository _customerRepository;
    private readonly FakeProductRepository _productRepository;

    public FakeOrderRepository(FakeCustomerRepository customerRepository, FakeProductRepository productRepository)
    {
        _customerRepository = customerRepository;
        _productRepository = productRepository;
    }

    public Task<IReadOnlyList<Order>> GetAllAsync(int? status = null, int? customerId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? searchTerm = null, string? sortBy = null, bool sortDescending = true, CancellationToken cancellationToken = default)
    {
        IEnumerable<Order> query = _entities.Values;

        if (status.HasValue)
            query = query.Where(o => o.Status == (OrderStatus)status.Value);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        if (dateFrom.HasValue)
            query = query.Where(o => o.OrderDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(o => o.OrderDate <= dateTo.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(o =>
                o.Id.ToString().Contains(term) ||
                (o.Customer != null && o.Customer.Name.ToLower().Contains(term)) ||
                o.Status.ToString().ToLower().Contains(term));
        }

        // Apply sorting
        query = (sortBy?.ToLowerInvariant()) switch
        {
            "orderdate" or "datahora" => sortDescending
                ? query.OrderByDescending(o => o.OrderDate)
                : query.OrderBy(o => o.OrderDate),
            "status" => sortDescending
                ? query.OrderByDescending(o => o.Status)
                : query.OrderBy(o => o.Status),
            "customer" or "cliente" => sortDescending
                ? query.OrderByDescending(o => o.Customer != null ? o.Customer.Name : "")
                : query.OrderBy(o => o.Customer != null ? o.Customer.Name : ""),
            "total" or "totalamount" => sortDescending
                ? query.OrderByDescending(o => o.TotalAmount)
                : query.OrderBy(o => o.TotalAmount),
            _ => query.OrderByDescending(o => o.OrderDate)
        };

        var orders = query.ToList();
        foreach (var order in orders)
        {
            LoadNavigationProperties(order);
        }

        return Task.FromResult((IReadOnlyList<Order>)orders.AsReadOnly());
    }

    public Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
    {
        var query = _entities.Values.Where(o => o.Status == OrderStatus.Aberto);

        if (customerId.HasValue)
            query = query.Where(o => o.CustomerId == customerId.Value);

        var order = query.OrderByDescending(o => o.OrderDate).FirstOrDefault();
        if (order != null)
        {
            LoadNavigationProperties(order);
        }
        return Task.FromResult(order);
    }

    public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entities.Values.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.Aberto));
    }

    public Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = _entities.Values.FirstOrDefault(o => o.Id == orderId);
        if (order == null) return Task.FromResult((IReadOnlyList<OrderItem>)Enumerable.Empty<OrderItem>().ToList().AsReadOnly());

        foreach (var item in order.OrderItems)
        {
            item.Product = _productRepository.GetByIdAsync(item.ProductId, cancellationToken).Result;
        }

        return Task.FromResult((IReadOnlyList<OrderItem>)order.OrderItems.ToList().AsReadOnly());
    }

    private void LoadNavigationProperties(Order order)
    {
        if (order.CustomerId.HasValue)
        {
            order.Customer = _customerRepository.GetByIdAsync(order.CustomerId.Value).Result;
        }

        foreach (var item in order.OrderItems)
        {
            item.Product = _productRepository.GetByIdAsync(item.ProductId).Result;
        }
    }
}