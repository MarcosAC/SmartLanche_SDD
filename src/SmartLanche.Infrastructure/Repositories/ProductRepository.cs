using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _context;

    public ProductRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = _context.Products.AsNoTracking().Include(product => product.Category);

        if (isActive.HasValue)
        {
            query = query.IgnoreQueryFilters().Where(product => product.IsActive == isActive.Value);
        }
        else
        {
            query = query.IgnoreQueryFilters(); // Show all (active + inactive)
        }

        return await query.OrderBy(product => product.Name).ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.IgnoreQueryFilters().Include(product => product.Category).FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        _context.Entry(product).State = EntityState.Modified;

        await _context.SaveChangesAsync(cancellationToken);
    }
}