using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Services;

public class CategoryService : CrudServiceBase<Category, CategoryDto, CreateCategoryRequest, UpdateCategoryRequest, ICategoryRepository>, ICategoryService
{
    public CategoryService(IServiceScopeFactory scopeFactory)
        : base(scopeFactory)
    {
    }

    protected override string GetEntityName() => "Categoria";

    protected override async Task<IReadOnlyList<Category>> GetAllEntitiesAsync(ICategoryRepository repository, bool? isActive, CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(isActive, cancellationToken);
    }

    protected override async Task<Category?> GetEntityByIdAsync(ICategoryRepository repository, int id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    protected override async Task AddEntityAsync(ICategoryRepository repository, Category entity, CancellationToken cancellationToken)
    {
        await repository.AddAsync(entity, cancellationToken);
    }

    protected override async Task UpdateEntityAsync(ICategoryRepository repository, Category entity, CancellationToken cancellationToken)
    {
        await repository.UpdateAsync(entity, cancellationToken);
    }

    protected override CategoryDto MapToDto(Category entity)
    {
        return new CategoryDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    protected override async Task<Category> CreateEntityAsync(ICategoryRepository repository, CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(request.Name);

        var exists = await repository.ExistsByNormalizedNameAsync(normalizedName, cancellationToken: cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Já existe uma categoria com esse nome.");
        }

        return new Category
        {
            Name = request.Name.Trim(),
            Description = NormalizeDescription(request.Description),
            IsActive = true
        };
    }

    protected override async Task UpdateEntityAsync(ICategoryRepository repository, Category entity, UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeName(request.Name);

        var exists = await repository.ExistsByNormalizedNameAsync(normalizedName, entity.Id, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Já existe uma categoria com esse nome.");
        }

        entity.Name = request.Name.Trim();
        entity.Description = NormalizeDescription(request.Description);
        entity.IsActive = request.IsActive;
    }

    protected override async Task ValidateCreateRequestAsync(CreateCategoryRequest request, CancellationToken cancellationToken)
    {
        await ValidateCategoryRequestAsync(request.Name, request.Description, cancellationToken);
    }

    protected override async Task ValidateUpdateRequestAsync(UpdateCategoryRequest request, CancellationToken cancellationToken)
    {
        await ValidateCategoryRequestAsync(request.Name, request.Description, cancellationToken);
    }

    private static Task ValidateCategoryRequestAsync(string name, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome da categoria é obrigatório.", nameof(name));
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException("O nome da categoria deve possuir no máximo 100 caracteres.", nameof(name));
        }

        if (description is not null && description.Length > 500)
        {
            throw new ArgumentException("A descrição da categoria deve possuir no máximo 500 caracteres.", nameof(description));
        }

        return Task.CompletedTask;
    }

    private static string NormalizeName(string name)
    {
        return name.Trim().ToUpperInvariant();
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}