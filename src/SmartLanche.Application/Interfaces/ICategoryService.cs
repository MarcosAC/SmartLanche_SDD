using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface ICategoryService : IEntityService<CategoryDto>
{
    Task<CategoryDto> CreateAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);

    Task<CategoryDto> UpdateAsync(UpdateCategoryRequest request, CancellationToken cancellationToken = default);
}