using SmartLanche.Application.DTOs;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Payment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Payment>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default);
    Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default);
    Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default);
}