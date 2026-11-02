using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class InventoryMovementRepository : IInventoryMovementRepository
{
    private readonly ApplicationDbContext _context;

    public InventoryMovementRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<InventoryMovement>> GetByItemAsync(int itemId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken = default)
    {
        var query = _context.InventoryMovements
            .AsNoTracking()
            .Where(m => m.InventoryItemId == itemId);

        if (dataInicio.HasValue)
        {
            query = query.Where(m => m.DataHora >= dataInicio.Value);
        }

        if (dataFim.HasValue)
        {
            query = query.Where(m => m.DataHora <= dataFim.Value);
        }

        return await query
            .OrderByDescending(m => m.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryMovement>> GetByOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryMovements
            .AsNoTracking()
            .Where(m => m.OrderId == orderId)
            .OrderByDescending(m => m.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken = default)
    {
        await _context.InventoryMovements.AddAsync(movement, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}