using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task UpdateAsync(Product product, CancellationToken cancellationToken = default);
}