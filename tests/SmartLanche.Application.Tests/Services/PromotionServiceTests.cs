using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class PromotionServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakePromotionRepository _promotionRepository;
    private readonly FakeProductRepository _productRepository;
    private readonly FakeCategoryRepository _categoryRepository;
    private readonly FakeOrderRepository _orderRepository;
    private readonly PromotionService _service;

    public PromotionServiceTests()
    {
        var services = new ServiceCollection();
        _promotionRepository = new FakePromotionRepository();
        _productRepository = new FakeProductRepository();
        _categoryRepository = new FakeCategoryRepository();
        _orderRepository = new FakeOrderRepository();
        services.AddSingleton<IPromotionRepository>(_promotionRepository);
        services.AddSingleton<IProductRepository>(_productRepository);
        services.AddSingleton<ICategoryRepository>(_categoryRepository);
        services.AddSingleton<IOrderRepository>(_orderRepository);
        _serviceProvider = services.BuildServiceProvider();

        _service = new PromotionService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            _productRepository,
            _categoryRepository,
            _orderRepository,
            NullLogger<PromotionService>.Instance);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesPromotion()
    {
        var category = await CreateCategoryAsync();
        var product = await CreateProductAsync(category.Id);

        var result = await _service.CreateAsync(
            new CreatePromotionRequest
            {
                Name = "Desconto 10%",
                Description = "Desconto de 10% em todos os lanches",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                Priority = 1,
                ApplicableProductIds = [product.Id],
                ApplicableCategoryIds = []
            });

        Assert.True(result.Id > 0);
        Assert.Equal("Desconto 10%", result.Name);
        Assert.Equal(DiscountType.Percentage, result.DiscountType);
        Assert.Equal(10m, result.DiscountValue);
        Assert.True(result.IsActive);
        Assert.Equal(1, result.Priority);
        Assert.Contains(product.Id, result.ApplicableProductIds);
    }

    [Fact]
    public async Task CreateAsync_WithGlobalPromotion_CreatesPromotion()
    {
        var result = await _service.CreateAsync(
            new CreatePromotionRequest
            {
                Name = "Promoção Global",
                Description = "Desconto global de 5%",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 5,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(30),
                Priority = 0,
                ApplicableProductIds = [],
                ApplicableCategoryIds = []
            });

        Assert.True(result.IsGlobal);
        Assert.Empty(result.ApplicableProductIds);
        Assert.Empty(result.ApplicableCategoryIds);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreatePromotionRequest
                {
                    Name = name,
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 10,
                    StartDate = DateTime.Today,
                    Priority = 1
                }));
    }

    [Fact]
    public async Task CreateAsync_WithZeroDiscountValue_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreatePromotionRequest
                {
                    Name = "Promoção",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 0,
                    StartDate = DateTime.Today,
                    Priority = 1
                }));
    }

    [Fact]
    public async Task CreateAsync_WithPercentageOver100_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreatePromotionRequest
                {
                    Name = "Promoção",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 150,
                    StartDate = DateTime.Today,
                    Priority = 1
                }));
    }

    [Fact]
    public async Task CreateAsync_WithEndDateBeforeStartDate_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreatePromotionRequest
                {
                    Name = "Promoção",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 10,
                    StartDate = DateTime.Today.AddDays(10),
                    EndDate = DateTime.Today,
                    Priority = 1
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNegativePriority_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreatePromotionRequest
                {
                    Name = "Promoção",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 10,
                    StartDate = DateTime.Today,
                    Priority = -1
                }));
    }

    [Fact]
    public async Task CreateAsync_WithFixedAmountDiscount_CreatesPromotion()
    {
        var result = await _service.CreateAsync(
            new CreatePromotionRequest
            {
                Name = "Desconto R$ 5",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 5,
                StartDate = DateTime.Today,
                Priority = 1
            });

        Assert.Equal(DiscountType.FixedAmount, result.DiscountType);
        Assert.Equal(5m, result.DiscountValue);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesPromotion()
    {
        var promotion = await CreatePromotionAsync();

        var result = await _service.UpdateAsync(
            new UpdatePromotionRequest
            {
                Id = promotion.Id,
                Name = "Promoção Atualizada",
                Description = "Nova descrição",
                DiscountType = DiscountType.FixedAmount,
                DiscountValue = 15,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(60),
                IsActive = true,
                Priority = 5,
                ApplicableProductIds = [],
                ApplicableCategoryIds = []
            });

        Assert.Equal("Promoção Atualizada", result.Name);
        Assert.Equal("Nova descrição", result.Description);
        Assert.Equal(DiscountType.FixedAmount, result.DiscountType);
        Assert.Equal(15m, result.DiscountValue);
        Assert.Equal(5, result.Priority);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentPromotion_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(
                new UpdatePromotionRequest
                {
                    Id = 999,
                    Name = "Promoção",
                    DiscountType = DiscountType.Percentage,
                    DiscountValue = 10,
                    StartDate = DateTime.Today,
                    Priority = 1
                }));
    }

    [Fact]
    public async Task DeactivateAsync_ExistingPromotion_SetsInactive()
    {
        var promotion = await CreatePromotionAsync();

        await _service.DeactivateAsync(promotion.Id);

        var result = await _service.GetAllAsync(isActive: false);

        Assert.Single(result);
        Assert.False(result[0].IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_InactivePromotion_SetsActive()
    {
        var promotion = await CreatePromotionAsync();

        await _service.DeactivateAsync(promotion.Id);
        await _service.ReactivateAsync(promotion.Id);

        var result = await _service.GetAllAsync(isActive: true);

        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingPromotions()
    {
        var activePromo = await CreatePromotionAsync();
        var inactivePromo = await CreatePromotionAsync();
        await _service.DeactivateAsync(inactivePromo.Id);

        var activePromotions = await _service.GetAllAsync(isActive: true);
        var inactivePromotions = await _service.GetAllAsync(isActive: false);

        Assert.Single(activePromotions);
        Assert.Equal(activePromo.Id, activePromotions[0].Id);

        Assert.Single(inactivePromotions);
        Assert.Equal(inactivePromo.Id, inactivePromotions[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingPromotion_ReturnsPromotion()
    {
        var promotion = await CreatePromotionAsync();

        var result = await _service.GetByIdAsync(promotion.Id);

        Assert.NotNull(result);
        Assert.Equal(promotion.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentPromotion_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
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

    private async Task<Product> CreateProductAsync(int categoryId)
    {
        var product = new Product
        {
            Name = "X-Burguer",
            CategoryId = categoryId,
            Price = 20m,
            IsActive = true
        };

        await _productRepository.AddAsync(product);

        return product;
    }

    private async Task<PromotionDto> CreatePromotionAsync()
    {
        return await _service.CreateAsync(
            new CreatePromotionRequest
            {
                Name = "Promoção Teste",
                DiscountType = DiscountType.Percentage,
                DiscountValue = 10,
                StartDate = DateTime.Today,
                Priority = 1
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

    private sealed class FakePromotionRepository : IPromotionRepository
    {
        private readonly List<Promotion> _promotions = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Promotion>> GetAllAsync(
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Promotion> query = _promotions;

            if (isActive.HasValue)
            {
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            IReadOnlyList<Promotion> result = query
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToList();

            return Task.FromResult(result);
        }

        public Task<IReadOnlyList<Promotion>> GetActiveAsync(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var result = _promotions
                .Where(p => p.IsActive
                    && p.StartDate <= date
                    && (p.EndDate == null || p.EndDate >= date))
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToList();

            return Task.FromResult<IReadOnlyList<Promotion>>(result);
        }

        public Task<IReadOnlyList<Promotion>> GetApplicableToProductAsync(
            int productId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var result = _promotions
                .Where(p => p.IsActive
                    && p.StartDate <= date
                    && (p.EndDate == null || p.EndDate >= date)
                    && (p.ApplicableProducts.Any(ap => ap.Id == productId)
                        || p.ApplicableCategories.Any(ac => ac.Products.Any(p => p.Id == productId))))
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToList();

            return Task.FromResult<IReadOnlyList<Promotion>>(result);
        }

        public Task<IReadOnlyList<Promotion>> GetApplicableToCategoryAsync(
            int categoryId,
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var result = _promotions
                .Where(p => p.IsActive
                    && p.StartDate <= date
                    && (p.EndDate == null || p.EndDate >= date)
                    && p.ApplicableCategories.Any(ac => ac.Id == categoryId))
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToList();

            return Task.FromResult<IReadOnlyList<Promotion>>(result);
        }

        public Task<IReadOnlyList<Promotion>> GetGlobalActiveAsync(
            DateTime date,
            CancellationToken cancellationToken = default)
        {
            var result = _promotions
                .Where(p => p.IsActive
                    && p.StartDate <= date
                    && (p.EndDate == null || p.EndDate >= date)
                    && !p.ApplicableProducts.Any()
                    && !p.ApplicableCategories.Any())
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Name)
                .ToList();

            return Task.FromResult<IReadOnlyList<Promotion>>(result);
        }

        public Task<Promotion?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_promotions.FirstOrDefault(p => p.Id == id));
        }

        public Task<Promotion?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_promotions.FirstOrDefault(p => p.Id == id));
        }

        public Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
        {
            promotion.Id = _nextId++;
            _promotions.Add(promotion);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Promotion promotion, CancellationToken cancellationToken = default)
        {
            var index = _promotions.FindIndex(p => p.Id == promotion.Id);

            if (index >= 0)
            {
                _promotions[index] = promotion;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Promotion promotion, CancellationToken cancellationToken = default)
        {
            _promotions.Remove(promotion);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Order>> GetAllAsync(
            int? status = null,
            int? customerId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = true,
            bool excludeCancelled = true,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Order> query = _orders;

            if (status.HasValue)
                query = query.Where(o => (int)o.Status == status.Value);
            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);
            if (dateFrom.HasValue)
                query = query.Where(o => o.OrderDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(o => o.OrderDate <= dateTo.Value);
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.Trim().ToLower();
                query = query.Where(o => o.Id.ToString().Contains(term)
                    || (o.Customer != null && o.Customer.Name.ToLower().Contains(term)));
            }

            if (excludeCancelled)
                query = query.Where(o => o.Status != OrderStatus.Cancelado);

            query = sortDescending ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate);

            return Task.FromResult<IReadOnlyList<Order>>(query.ToList());
        }

        public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(_orders.FirstOrDefault(o => o.Id == id));

        public Task<Order?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(_orders.FirstOrDefault(o => o.Id == id));

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            order.Id = _nextId++;
            _orders.Add(order);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            var index = _orders.FindIndex(o => o.Id == order.Id);
            if (index >= 0)
                _orders[index] = order;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.Id == id));

        public Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(_orders.Where(o => o.Status == OrderStatus.EmPreparacao && (!customerId.HasValue || o.CustomerId == customerId)).OrderByDescending(o => o.OrderDate).FirstOrDefault());

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.EmPreparacao));

        public Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return Task.FromResult<IReadOnlyList<OrderItem>>([]);
            return Task.FromResult<IReadOnlyList<OrderItem>>(order.OrderItems.ToList());
        }

        public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
        {
            _orders.RemoveAll(o => o.Id == order.Id);
            return Task.CompletedTask;
        }
    }
}