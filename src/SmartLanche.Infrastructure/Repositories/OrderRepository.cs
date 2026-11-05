using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly ApplicationDbContext _context;

    public OrderRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Order>> GetAllAsync(
        int? status = null,
        int? customerId = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        string? searchTerm = null,
        string? sortBy = null,
        bool sortDescending = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = _context.Orders
            .AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Product);

        if (status.HasValue)
        {
            query = query.Where(order => (int)order.Status == status.Value);
        }

        if (customerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        if (dateFrom.HasValue)
        {
            query = query.Where(order => order.OrderDate >= dateFrom.Value);
        }

        if (dateTo.HasValue)
        {
            query = query.Where(order => order.OrderDate <= dateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.ToLower();
            query = query.Where(order =>
                order.Id.ToString().Contains(term) ||
                (order.Customer != null && order.Customer.Name.ToLower().Contains(term)) ||
                order.Status.ToString().ToLower().Contains(term));
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

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.AsNoTracking()
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task<Order?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(order => order.Customer)
            .Include(order => order.OrderItems)
                .ThenInclude(item => item.Product)
            .FirstOrDefaultAsync(order => order.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
    {
        // Use Entry().State = Modified to only mark the Order entity as modified
        // Avoids marking navigation properties (Customer, OrderItems) as modified
        _context.Entry(order).State = EntityState.Modified;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Order> query = _context.Orders
            .Where(order => order.Status == OrderStatus.Pendente);

        if (customerId.HasValue)
        {
            query = query.Where(order => order.CustomerId == customerId.Value);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AnyAsync(order => order.CustomerId == customerId && order.Status == OrderStatus.Pendente, cancellationToken);
    }

    public async Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.OrderItems
            .AsNoTracking()
            .Include(item => item.Product)
            .Where(item => item.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}