using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Application.Tests.Fakes;

namespace SmartLanche.Application.Tests.Fakes;

public sealed class FakeCustomerRepository : FakeRepositoryBase<Customer>, ICustomerRepository
{
    public Task<IReadOnlyList<Customer>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<Customer> query = _entities.Values;

        if (isActive.HasValue)
        {
            query = query.Where(c => c.IsActive == isActive.Value);
        }

        return Task.FromResult<IReadOnlyList<Customer>>(query.OrderBy(c => c.Name).ToList());
    }

    public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_entities.ContainsKey(id));
    }
}