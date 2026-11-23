using Microsoft.EntityFrameworkCore;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Infrastructure.Data;

namespace SmartLanche.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly ApplicationDbContext _context;

    public PaymentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Payment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrderId == orderId)
            .OrderByDescending(p => p.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Payment>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Payments.AsNoTracking();

        if (filter is not null)
        {
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var term = filter.SearchTerm.Trim();
                query = query.Where(p => p.Notes != null && p.Notes.Contains(term) || p.OrderId.ToString().Contains(term));
            }

            if (filter.FormaPagamento.HasValue)
            {
                query = query.Where(p => p.FormaPagamento == filter.FormaPagamento.Value);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(p => p.DataHora >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(p => p.DataHora <= filter.EndDate.Value);
            }
        }

        return await query
            .OrderByDescending(p => p.DataHora)
            .ToListAsync(cancellationToken);
    }

    public async Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        await _context.Payments.AddAsync(payment, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return payment;
    }

    public async Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        _context.Entry(payment).State = EntityState.Modified;
        await _context.SaveChangesAsync(cancellationToken);
    }
}