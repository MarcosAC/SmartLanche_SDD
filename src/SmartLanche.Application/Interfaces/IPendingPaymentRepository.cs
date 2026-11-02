using SmartLanche.Application.DTOs;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Interfaces;

public interface IPendingPaymentRepository
{
    Task<PendingPayment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PendingPayment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<PendingPayment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPayment>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPayment>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPayment>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default);
    Task<PendingPayment> AddAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default);
    Task UpdateAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default);

    // Pending-orders specific
    Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default);
}