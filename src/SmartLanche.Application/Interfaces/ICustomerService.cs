using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface ICustomerService : IEntityService<CustomerDto>
{
    Task<CustomerDto> CreateAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    Task<CustomerDto> UpdateAsync(UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    // Pending-orders specific
    Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default);
}