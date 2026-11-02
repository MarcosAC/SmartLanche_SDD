using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Services;

public class ProductService : CrudServiceBase<Product, ProductDto, CreateProductRequest, UpdateProductRequest, IProductRepository>, IProductService
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IInventoryService _inventoryService;

    public ProductService(
        IServiceScopeFactory scopeFactory,
        ICategoryRepository categoryRepository,
        IInventoryService inventoryService)
        : base(scopeFactory)
    {
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
    }

    protected override string GetEntityName() => "Produto";

    protected override async Task<IReadOnlyList<Product>> GetAllEntitiesAsync(IProductRepository repository, bool? isActive, CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(isActive, cancellationToken);
    }

    protected override async Task<Product?> GetEntityByIdAsync(IProductRepository repository, int id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    protected override async Task AddEntityAsync(IProductRepository repository, Product entity, CancellationToken cancellationToken)
    {
        await repository.AddAsync(entity, cancellationToken);
    }

    protected override async Task UpdateEntityAsync(IProductRepository repository, Product entity, CancellationToken cancellationToken)
    {
        await repository.UpdateAsync(entity, cancellationToken);
    }

    protected override ProductDto MapToDto(Product entity)
    {
        return new ProductDto
        {
            Id = entity.Id,
            Name = entity.Name,
            CategoryId = entity.CategoryId,
            CategoryName = entity.Category?.Name ?? string.Empty,
            Price = entity.Price,
            IsCombo = entity.IsCombo,
            Description = entity.Description,
            IsActive = entity.IsActive
        };
    }

    protected override async Task<Product> CreateEntityAsync(IProductRepository repository, CreateProductRequest request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException("A categoria informada não existe.");
        }

        if (!category.IsActive)
        {
            throw new InvalidOperationException("Não é permitido cadastrar um produto em uma categoria inativa.");
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            CategoryId = request.CategoryId,
            Category = category,
            Price = request.Price,
            IsCombo = request.IsCombo,
            Description = NormalizeDescription(request.Description),
            IsActive = true
        };

        return product;
    }

    public override async Task<ProductDto> CreateAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateCreateRequestAsync(request, cancellationToken);

        return await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entity = await CreateEntityAsync(repository, request, ct);

            await AddEntityAsync(repository, entity, ct);

            // Create InventoryItem if requested (now entity.Id is set)
            if (request.InventoryRequest is not null)
            {
                var inventoryRequest = request.InventoryRequest with { ProductId = entity.Id };
                await _inventoryService.CreateAsync(inventoryRequest, ct);
            }

            return MapToDto(entity);
        }, cancellationToken);
    }

    protected override async Task UpdateEntityAsync(IProductRepository repository, Product entity, UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var category = await _categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException("A categoria informada não existe.");
        }

        if (!category.IsActive)
        {
            throw new InvalidOperationException("Não é permitido mover o produto para uma categoria inativa.");
        }

        entity.Name = request.Name.Trim();
        entity.CategoryId = request.CategoryId;
        entity.Category = category;
        entity.Price = request.Price;
        entity.IsCombo = request.IsCombo;
        entity.Description = NormalizeDescription(request.Description);
        // Note: IsActive is not updated here - use Deactivate/Reactivate methods
    }

    protected override async Task ValidateCreateRequestAsync(CreateProductRequest request, CancellationToken cancellationToken)
    {
        await ValidateProductRequestAsync(request.Name, request.Price, request.Description, cancellationToken);

        if (request.InventoryRequest is not null)
        {
            await ValidateInventoryRequestAsync(request.InventoryRequest, cancellationToken);
        }
    }

    protected override async Task ValidateUpdateRequestAsync(UpdateProductRequest request, CancellationToken cancellationToken)
    {
        await ValidateProductRequestAsync(request.Name, request.Price, request.Description, cancellationToken);
    }

    private static Task ValidateProductRequestAsync(string name, decimal price, string? description, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do produto é obrigatório.", nameof(name));
        }

        if (name.Trim().Length > 150)
        {
            throw new ArgumentException("O nome do produto deve possuir no máximo 150 caracteres.", nameof(name));
        }

        if (price < 0)
        {
            throw new ArgumentException("O preço do produto não pode ser negativo.", nameof(price));
        }

        if (description is not null && description.Length > 500)
        {
            throw new ArgumentException("A descrição do produto deve possuir no máximo 500 caracteres.", nameof(description));
        }

        return Task.CompletedTask;
    }

    private static Task ValidateInventoryRequestAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken)
    {
        if (request.ValorUnitario < 0)
        {
            throw new ArgumentException("O valor unitário do item de estoque não pode ser negativo.", nameof(request.ValorUnitario));
        }

        if (request.QuantidadeMinima < 0)
        {
            throw new ArgumentException("A quantidade mínima do item de estoque não pode ser negativa.", nameof(request.QuantidadeMinima));
        }

        return Task.CompletedTask;
    }

    private static string? NormalizeDescription(string? description)
    {
        return string.IsNullOrWhiteSpace(description)
            ? null
            : description.Trim();
    }
}