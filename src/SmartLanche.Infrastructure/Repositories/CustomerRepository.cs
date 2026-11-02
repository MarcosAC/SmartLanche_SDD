using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Customer> query = _context.Customers.AsNoTracking();

        if (isActive.HasValue)
        {
            query = query.Where(customer => customer.IsActive == isActive.Value);
        }

        return await query.OrderBy(customer => customer.Name).ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.AsNoTracking()
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(customer => customer.Id == id, cancellationToken);
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _context.Entry(customer).State = EntityState.Modified;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Customers.AnyAsync(c => c.Id == id, cancellationToken);
    }
}