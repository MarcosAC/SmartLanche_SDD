using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakePendingPaymentRepository : FakeRepositoryBase<PendingPayment>, IPendingPaymentRepository
{
    private readonly FakeCustomerRepository _customerRepository;

    public FakePendingPaymentRepository(FakeCustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public Task<PendingPayment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        => GetByIdAsync(id, cancellationToken);

    public Task<PendingPayment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var payment = _entities.Values.FirstOrDefault(p => p.OrderId == orderId);
        return Task.FromResult<PendingPayment?>(payment);
    }

    public Task<IReadOnlyList<PendingPayment>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var payments = _entities.Values.Where(p => p.CustomerId == customerId).ToList();
        return Task.FromResult<IReadOnlyList<PendingPayment>>(payments);
    }

    public Task<IReadOnlyList<PendingPayment>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<PendingPayment>>(_entities.Values.ToList());
    }

    public Task<IReadOnlyList<PendingPayment>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default)
    {
        var payments = _entities.Values.Where(p => p.StatusPagamento == status).ToList();
        return Task.FromResult<IReadOnlyList<PendingPayment>>(payments);
    }

    public Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var sum = _entities.Values
            .Where(p => p.CustomerId == customerId && p.StatusPagamento == StatusPagamentoPendente.NaoPago)
            .Sum(p => p.SaldoPendente);
        return Task.FromResult(sum);
    }

    public Task<IReadOnlyList<SmartLanche.Application.DTOs.DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<SmartLanche.Application.DTOs.DevedorDto>>([]);
    }

    public Task<PendingPayment> AddAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
    {
        var idProperty = typeof(PendingPayment).GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            var currentId = (int?)idProperty.GetValue(pendingPayment);
            if (currentId == null || currentId == 0)
            {
                idProperty.SetValue(pendingPayment, _nextId++);
            }
        }
        _entities.TryAdd(GetEntityId(pendingPayment), pendingPayment);
        return Task.FromResult(pendingPayment);
    }

    public Task UpdateAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
    {
        return base.UpdateAsync(pendingPayment, cancellationToken);
    }
}