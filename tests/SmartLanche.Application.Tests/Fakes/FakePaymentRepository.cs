using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakePaymentRepository : FakeRepositoryBase<Payment>, IPaymentRepository
{
    public Task<Payment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<IReadOnlyList<Payment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var payments = _entities.Values.Where(p => p.OrderId == orderId).ToList();
        return Task.FromResult<IReadOnlyList<Payment>>(payments);
    }

    public Task<IReadOnlyList<Payment>> GetAllAsync(SmartLanche.Application.DTOs.PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<Payment>>(_entities.Values.ToList());
    }

    public Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        var idProperty = typeof(Payment).GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            var currentId = (int?)idProperty.GetValue(payment);
            if (currentId == null || currentId == 0)
            {
                idProperty.SetValue(payment, _nextId++);
            }
        }
        _entities.TryAdd(GetEntityId(payment), payment);
        return Task.FromResult(payment);
    }

    public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
    {
        return base.UpdateAsync(payment, cancellationToken);
    }
}