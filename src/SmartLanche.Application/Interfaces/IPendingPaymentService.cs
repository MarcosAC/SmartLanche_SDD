using SmartLanche.Application.DTOs;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Interfaces;

public interface IPendingPaymentService
{
    Task<RegisterPendingPaymentResult> RegisterPaymentAsync(RegisterPendingPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PendingPaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<PendingPaymentDto?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPaymentDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPaymentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPaymentDto>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PendingPaymentHistoryDto>> GetHistoryAsync(int pendingPaymentId, CancellationToken cancellationToken = default);
    Task<PendingPaymentDto> CreateForOrderAsync(int orderId, int customerId, decimal valor, string? observacoes, CancellationToken cancellationToken = default);

    // Pending-orders specific
    Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default);
}