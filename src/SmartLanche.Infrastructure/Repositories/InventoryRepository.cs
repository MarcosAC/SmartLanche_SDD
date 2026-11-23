using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Produto)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
    }

    public async Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Produto)
                .ThenInclude(p => p.Category)
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(bool? apenasBaixo, string? searchTerm, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryItems
            .AsNoTracking()
            .Include(i => i.Produto)
                .ThenInclude(p => p.Category)
            .AsQueryable();

        if (apenasBaixo == true)
        {
            query = query.Where(i => i.QuantidadeAtual <= i.QuantidadeMinima && i.QuantidadeAtual > 0);
        }

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(i =>
                i.Nome.ToLower().Contains(term) ||
                (i.Produto != null && i.Produto.Name.ToLower().Contains(term)) ||
                (i.ProductId.HasValue && i.ProductId.Value.ToString().Contains(term)));
        }

        return await query
            .OrderBy(i => i.Nome)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        await _context.InventoryItems.AddAsync(item, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _context.Entry(item).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<decimal> GetValorTotalEstoqueAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .AsNoTracking()
            .SumAsync(i => i.QuantidadeAtual * i.ValorUnitario, cancellationToken);
    }
}