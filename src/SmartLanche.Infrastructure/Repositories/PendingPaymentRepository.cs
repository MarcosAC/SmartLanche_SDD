using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class PendingPaymentRepository : IPendingPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PendingPaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PendingPayment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(pp => pp.Customer)
            .Include(pp => pp.Order)
            .Include(pp => pp.History)
            .FirstOrDefaultAsync(pp => pp.Id == id, cancellationToken);
    }

    public async Task<PendingPayment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .IgnoreQueryFilters()
            .Include(pp => pp.Customer)
            .Include(pp => pp.Order)
            .Include(pp => pp.History)
            .FirstOrDefaultAsync(pp => pp.Id == id, cancellationToken);
    }

    public async Task<PendingPayment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(pp => pp.Customer)
            .Include(pp => pp.Order)
            .Include(pp => pp.History)
            .FirstOrDefaultAsync(pp => pp.OrderId == orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<PendingPayment>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(pp => pp.Customer)
            .Include(pp => pp.Order)
            .Include(pp => pp.History)
            .Where(pp => pp.CustomerId == customerId)
            .OrderByDescending(pp => pp.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingPayment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .AsNoTracking()
            .IgnoreQueryFilters() // Include inactive customers' pending payments
            .Include(pp => pp.Customer)
            .Include(pp => pp.Order)
            .Include(pp => pp.History)
            .OrderByDescending(pp => pp.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PendingPayment>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Include(pp => pp.Customer)
            .Include(pp => pp.Order)
            .Include(pp => pp.History)
            .Where(pp => pp.StatusPagamento == status)
            .OrderByDescending(pp => pp.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<PendingPayment> AddAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
    {
        await _context.PendingPayments.AddAsync(pendingPayment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return pendingPayment;
    }

    public async Task UpdateAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
    {
        _context.Entry(pendingPayment).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }

    // Pending-orders specific
    public async Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _context.PendingPayments
            .AsNoTracking()
            .Where(pp => pp.CustomerId == customerId && pp.StatusPagamento == StatusPagamentoPendente.NaoPago)
            .SumAsync(pp => pp.ValorOriginal - pp.ValorPago, cancellationToken);
    }

    public async Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
    {
        // Load filtered data first, then group in memory to avoid EF Core translation issues
        var pendingPayments = await _context.PendingPayments
            .AsNoTracking()
            .Include(pp => pp.Customer)
            .Where(pp => pp.StatusPagamento == StatusPagamentoPendente.NaoPago
                && pp.Customer != null
                && pp.Customer.IsActive)
            .ToListAsync(cancellationToken);

        var devedores = pendingPayments
            .GroupBy(pp => new { pp.CustomerId, pp.Customer!.Name, pp.Customer!.Phone })
            .Select(g => new DevedorDto(
                g.Key.CustomerId,
                g.Key.Name,
                g.Key.Phone,
                g.Sum(pp => pp.ValorOriginal - pp.ValorPago),
                g.Count(),
                g.Max(pp => pp.DataHora)))
            .OrderByDescending(d => d.SaldoDevedor)
            .ToList();

        return devedores;
    }
}