using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IPaymentService
{
    Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentDto>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default);
    Task<ProcessPaymentResult> EstornarPaymentAsync(int paymentId, string motivo, CancellationToken cancellationToken = default);
}