using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface ICustomerRepository
{
    Task<IReadOnlyList<Customer>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);

    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
}