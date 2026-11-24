using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Tests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeCategoryRepository _categoryRepository;
    private readonly FakeProductRepository _productRepository;
    private readonly FakeInventoryService _inventoryService;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var services = new ServiceCollection();
        _categoryRepository = new FakeCategoryRepository();
        _productRepository = new FakeProductRepository();
        _inventoryService = new FakeInventoryService();
        services.AddSingleton<ICategoryRepository>(_categoryRepository);
        services.AddSingleton<IProductRepository>(_productRepository);
        services.AddSingleton<IInventoryService>(_inventoryService);
        _serviceProvider = services.BuildServiceProvider();

        _service = new ProductService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _inventoryService);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesProduct()
    {
        var category = await CreateCategoryAsync();

        var result = await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "X-Burguer",
                CategoryId = category.Id,
                Price = 25.50m,
                IsCombo = false,
                Description = "Hambúrguer tradicional"
            });

        Assert.True(result.Id > 0);
        Assert.Equal("X-Burguer", result.Name);
        Assert.Equal(category.Id, result.CategoryId);
        Assert.Equal("Lanches", result.CategoryName);
        Assert.Equal(25.50m, result.Price);
        Assert.False(result.IsCombo);
        Assert.True(result.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException(string name)
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = name,
                    CategoryId = category.Id,
                    Price = 10m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNameLongerThan150Characters_ThrowsArgumentException()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = new string('A', 151),
                    CategoryId = category.Id,
                    Price = 10m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentCategory_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = "X-Burguer",
                    CategoryId = 999,
                    Price = 20m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveCategory_ThrowsInvalidOperationException()
    {
        var category = await CreateCategoryAsync(isActive: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = "X-Burguer",
                    CategoryId = category.Id,
                    Price = 20m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithZeroPrice_CreatesProduct()
    {
        var category = await CreateCategoryAsync();

        var result = await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "Amostra",
                CategoryId = category.Id,
                Price = 0m
            });

        Assert.Equal(0m, result.Price);
    }

    [Fact]
    public async Task CreateAsync_WithNegativePrice_ThrowsArgumentException()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = "X-Burguer",
                    CategoryId = category.Id,
                    Price = -1m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithDescriptionLongerThan500Characters_ThrowsArgumentException()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = "X-Burguer",
                    CategoryId = category.Id,
                    Price = 20m,
                    Description = new string('A', 501)
                }));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyDescription_StoresNullDescription()
    {
        var category = await CreateCategoryAsync();

        var result = await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "X-Burguer",
                CategoryId = category.Id,
                Price = 20m,
                Description = "   "
            });

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task CreateAsync_WithCombo_SetsIsComboTrue()
    {
        var category = await CreateCategoryAsync();

        var result = await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "X-Burguer + Suco",
                CategoryId = category.Id,
                Price = 30m,
                IsCombo = true
            });

        Assert.True(result.IsCombo);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_AllowsBothProducts()
    {
        var category = await CreateCategoryAsync();

        var request = new CreateProductRequest
        {
            Name = "X-Burguer",
            CategoryId = category.Id,
            Price = 20m
        };

        var first = await _service.CreateAsync(request);
        var second = await _service.CreateAsync(request);

        Assert.NotEqual(first.Id, second.Id);

        var products = await _service.GetAllAsync();

        Assert.Equal(2, products.Count);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesProduct()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var result = await _service.UpdateAsync(
            new UpdateProductRequest
            {
                Id = product.Id,
                Name = "X-Burguer Especial",
                CategoryId = category.Id,
                Price = 32m,
                IsCombo = true,
                Description = "Versão especial"
            });

        Assert.Equal("X-Burguer Especial", result.Name);
        Assert.Equal(32m, result.Price);
        Assert.True(result.IsCombo);
        Assert.Equal("Versão especial", result.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentProduct_ThrowsInvalidOperationException()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(
                new UpdateProductRequest
                {
                    Id = 999,
                    Name = "X-Burguer",
                    CategoryId = category.Id,
                    Price = 20m
                }));
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentCategory_ThrowsInvalidOperationException()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(
                new UpdateProductRequest
                {
                    Id = product.Id,
                    Name = "X-Burguer",
                    CategoryId = 999,
                    Price = 20m
                }));
    }

    [Fact]
    public async Task DeactivateAsync_ExistingProduct_SetsInactive()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        await _service.DeactivateAsync(product.Id);

        var result = await _service.GetAllAsync(isActive: false);

        Assert.Single(result);
        Assert.False(result[0].IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_InactiveProduct_SetsActive()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        await _service.DeactivateAsync(product.Id);
        await _service.ReactivateAsync(product.Id);

        var result = await _service.GetAllAsync(isActive: true);

        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingProducts()
    {
        var category = await CreateCategoryAsync();
        var activeProduct = await CreateProductAsync(category.Id);
        var inactiveProduct = await CreateProductAsync(category.Id);

        await _service.DeactivateAsync(inactiveProduct.Id);

        var activeProducts = await _service.GetAllAsync(isActive: true);
        var inactiveProducts = await _service.GetAllAsync(isActive: false);

        Assert.Single(activeProducts);
        Assert.Equal(activeProduct.Id, activeProducts[0].Id);

        Assert.Single(inactiveProducts);
        Assert.Equal(inactiveProduct.Id, inactiveProducts[0].Id);
    }

    [Fact]
    public async Task CreateAsync_WithInventoryRequest_CreatesLinkedInventoryItem()
    {
        var category = await CreateCategoryAsync();

        var result = await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "X-Burguer",
                CategoryId = category.Id,
                Price = 25.50m,
                InventoryRequest = new CreateInventoryItemRequest(
                    ProductId: null,
                    Nome: "Hambúrguer 150g",
                    QuantidadeAtual: 50,
                    QuantidadeMinima: 10,
                    ValorUnitario: 8.50m,
                    UnidadeMedida: "UN",
                    Notes: "Estoque inicial"
                )
            });

        Assert.True(result.Id > 0);
        Assert.Single(_inventoryService.CreatedItems);
        var createdItem = _inventoryService.CreatedItems[0];
        Assert.Equal(result.Id, createdItem.ProductId);
        Assert.Equal("Hambúrguer 150g", createdItem.Nome);
        Assert.Equal(50, createdItem.QuantidadeAtual);
        Assert.Equal(10, createdItem.QuantidadeMinima);
        Assert.Equal(8.50m, createdItem.ValorUnitario);
    }

    [Fact]
    public async Task CreateAsync_WithoutInventoryRequest_DoesNotCreateInventoryItem()
    {
        var category = await CreateCategoryAsync();

        var result = await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "X-Burguer",
                CategoryId = category.Id,
                Price = 25.50m
            });

        Assert.True(result.Id > 0);
        Assert.Empty(_inventoryService.CreatedItems);
    }

    [Fact]
    public async Task CreateAsync_WithNegativeValorUnitarioInInventoryRequest_ThrowsArgumentException()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = "X-Burguer",
                    CategoryId = category.Id,
                    Price = 25.50m,
                    InventoryRequest = new CreateInventoryItemRequest(
                        ProductId: null,
                        Nome: "Hambúrguer 150g",
                        QuantidadeAtual: 50,
                        QuantidadeMinima: 10,
                        ValorUnitario: -5.00m,
                        UnidadeMedida: "UN",
                        Notes: null
                    )
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeQuantidadeMinimaInInventoryRequest_ThrowsArgumentException()
    {
        var category = await CreateCategoryAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateProductRequest
                {
                    Name = "X-Burguer",
                    CategoryId = category.Id,
                    Price = 25.50m,
                    InventoryRequest = new CreateInventoryItemRequest(
                        ProductId: null,
                        Nome: "Hambúrguer 150g",
                        QuantidadeAtual: 50,
                        QuantidadeMinima: -5,
                        ValorUnitario: 8.50m,
                        UnidadeMedida: "UN",
                        Notes: null
                    )
                }));
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private async Task<Category> CreateCategoryAsync(bool isActive = true)
    {
        var category = new Category
        {
            Name = "Lanches",
            IsActive = isActive
        };

        await _categoryRepository.AddAsync(category);

        return category;
    }

    private async Task<ProductDto> CreateProductAsync(int categoryId)
    {
        return await _service.CreateAsync(
            new CreateProductRequest
            {
                Name = "X-Burguer",
                CategoryId = categoryId,
                Price = 20m
            });
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Category>> GetAllAsync(
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Category> query = _categories;

            if (isActive.HasValue)
            {
                query = query.Where(category => category.IsActive == isActive.Value);
            }

            IReadOnlyList<Category> result = query.OrderBy(category => category.Name).ToList();

            return Task.FromResult(result);
        }

        public Task<Category?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_categories.FirstOrDefault(category => category.Id == id));
        }

        public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = _categories.Any(category => category.Id != excludeId && category.Name.Trim().ToUpperInvariant() == normalizedName);

            return Task.FromResult(exists);
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            category.Id = _nextId++;
            _categories.Add(category);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            var index = _categories.FindIndex(item => item.Id == category.Id);

            if (index >= 0)
            {
                _categories[index] = category;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly List<Product> _products = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Product>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Product> query = _products;

            if (isActive.HasValue)
            {
                query = query.Where(product => product.IsActive == isActive.Value);
            }

            IReadOnlyList<Product> result = query.OrderBy(product => product.Name).ToList();

            return Task.FromResult(result);
        }

        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.FirstOrDefault(product => product.Id == id));
        }

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            product.Id = _nextId++;
            _products.Add(product);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            var index = _products.FindIndex(item => item.Id == product.Id);

            if (index >= 0)
            {
                _products[index] = product;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class FakeInventoryService : IInventoryService
    {
        private readonly List<InventoryItemDto> _items = [];
        private readonly List<InventoryMovementDto> _movements = [];
        private int _nextItemId = 1;
        private int _nextMovementId = 1;

        public IReadOnlyList<InventoryItemDto> CreatedItems => _items;

        public Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
        {
            var item = new InventoryItemDto(
                _nextItemId++,
                request.ProductId,
                null,
                request.Nome,
                request.QuantidadeAtual,
                request.QuantidadeMinima,
                request.ValorUnitario,
                request.UnidadeMedida,
                request.Notes,
                request.QuantidadeAtual <= request.QuantidadeMinima && request.QuantidadeAtual > 0,
                request.QuantidadeAtual <= 0,
                request.QuantidadeAtual * request.ValorUnitario
            );
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<InventoryItemDto> UpdateAsync(int id, UpdateInventoryItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InventoryMovementDto> RegistrarEntradaAsync(RegistrarEntradaRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<InventoryMovementDto> RegistrarSaidaAsync(RegistrarSaidaRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<InventoryItemDto>> GetItensBaixosAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<InventoryItemDto>> GetItensZeradosAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetValorTotalEstoqueAsync(CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task BaixaAutomaticaPedidoAsync(int orderId, IReadOnlyList<OrderItem> orderItems, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task EstornarBaixaPedidoAsync(int orderId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}