using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IProductService : IEntityService<ProductDto>
{
    Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default);

    Task<ProductDto> UpdateAsync(UpdateProductRequest request, CancellationToken cancellationToken = default);
}