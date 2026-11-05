using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class ComboServiceTests
{
    private static (ComboService Service, FakeComboRepository ComboRepo, FakeProductRepository ProductRepo, FakeOrderRepository OrderRepo, FakeOrderService OrderService, ServiceProvider Provider) CreateContext()
    {
        var services = new ServiceCollection();
        var productRepository = new FakeProductRepository();
        var orderService = new FakeOrderService(null!, productRepository);
        var orderRepository = new FakeOrderRepository(orderService, productRepository);
        var comboRepository = new FakeComboRepository(productRepository);

        // Fix circular reference
        orderService = new FakeOrderService(orderRepository, productRepository);

        services.AddSingleton<IComboRepository>(comboRepository);
        services.AddSingleton<IProductRepository>(productRepository);
        services.AddSingleton<IOrderRepository>(orderRepository);
        services.AddSingleton<IOrderService>(orderService);
        var serviceProvider = services.BuildServiceProvider();

        var service = new ComboService(
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            productRepository,
            orderRepository,
            orderService,
            NullLogger<ComboService>.Instance);

        return (service, comboRepository, productRepository, orderRepository, orderService, serviceProvider);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCombo()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var product3 = await CreateProductAsync(ctx.ProductRepo, "Refrigerante", 6m);

            var result = await ctx.Service.CreateAsync(
                new CreateComboRequest
                {
                    Name = "Combo Clássico",
                    Description = "Burguer + Batata + Refrigerante",
                    Price = 30m,
                    Items =
                    [
                        new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                        new CreateComboItemRequest { ProductId = product2.Id, Quantity = 1 },
                        new CreateComboItemRequest { ProductId = product3.Id, Quantity = 1 }
                    ]
                });

            Assert.True(result.Id > 0);
            Assert.Equal("Combo Clássico", result.Name);
            Assert.Equal(30m, result.Price);
            Assert.True(result.IsActive);
            Assert.Equal(3, result.Items.Count);
            Assert.Contains(result.Items, i => i.ProductId == product1.Id && i.Quantity == 1);
            Assert.Contains(result.Items, i => i.ProductId == product2.Id && i.Quantity == 1);
            Assert.Contains(result.Items, i => i.ProductId == product3.Id && i.Quantity == 1);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithTwoItems_CreatesCombo()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            var result = await ctx.Service.CreateAsync(
                new CreateComboRequest
                {
                    Name = "Combo Simples",
                    Price = 25m,
                    Items =
                    [
                        new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                        new CreateComboItemRequest { ProductId = product2.Id, Quantity = 1 }
                    ]
                });

            Assert.Equal(2, result.Items.Count);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException(string name)
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = name,
                        Price = 25m,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = product2.Id, Quantity = 1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithZeroPrice_ThrowsArgumentException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = "Combo",
                        Price = 0,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = product2.Id, Quantity = 1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithNegativePrice_ThrowsArgumentException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = "Combo",
                        Price = -10,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = product2.Id, Quantity = 1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithOneItem_ThrowsArgumentException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = "Combo",
                        Price = 20m,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithZeroQuantity_ThrowsArgumentException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = "Combo",
                        Price = 25m,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = product2.Id, Quantity = 0 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithNegativeQuantity_ThrowsArgumentException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            await Assert.ThrowsAsync<ArgumentException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = "Combo",
                        Price = 25m,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = product2.Id, Quantity = -1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentProduct_ThrowsInvalidOperationException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ctx.Service.CreateAsync(
                    new CreateComboRequest
                    {
                        Name = "Combo",
                        Price = 25m,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = 999, Quantity = 1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCombo()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var product3 = await CreateProductAsync(ctx.ProductRepo, "Refrigerante", 6m);

            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);

            var result = await ctx.Service.UpdateAsync(
                new UpdateComboRequest
                {
                    Id = combo.Id,
                    Name = "Combo Atualizado",
                    Description = "Nova descrição",
                    Price = 35m,
                    IsActive = true,
                    Items =
                    [
                        new CreateComboItemRequest { ProductId = product1.Id, Quantity = 2 },
                        new CreateComboItemRequest { ProductId = product3.Id, Quantity = 1 }
                    ]
                });

            Assert.Equal("Combo Atualizado", result.Name);
            Assert.Equal("Nova descrição", result.Description);
            Assert.Equal(35m, result.Price);
            Assert.True(result.IsActive);
            Assert.Equal(2, result.Items.Count);
            Assert.Contains(result.Items, i => i.ProductId == product1.Id && i.Quantity == 2);
            Assert.Contains(result.Items, i => i.ProductId == product3.Id && i.Quantity == 1);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentCombo_ThrowsInvalidOperationException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ctx.Service.UpdateAsync(
                    new UpdateComboRequest
                    {
                        Id = 999,
                        Name = "Combo",
                        Price = 25m,
                        Items =
                        [
                            new CreateComboItemRequest { ProductId = product1.Id, Quantity = 1 },
                            new CreateComboItemRequest { ProductId = product2.Id, Quantity = 1 }
                        ]
                    }));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task DeactivateAsync_ExistingCombo_SetsInactive()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);

            await ctx.Service.DeactivateAsync(combo.Id);

            var result = await ctx.Service.GetAllAsync(isActive: false);

            Assert.Single(result);
            Assert.False(result[0].IsActive);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task ReactivateAsync_InactiveCombo_SetsActive()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);

            await ctx.Service.DeactivateAsync(combo.Id);
            await ctx.Service.ReactivateAsync(combo.Id);

            var result = await ctx.Service.GetAllAsync(isActive: true);

            Assert.Single(result);
            Assert.True(result[0].IsActive);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingCombos()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var activeCombo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            var inactiveCombo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            await ctx.Service.DeactivateAsync(inactiveCombo.Id);

            var activeCombos = await ctx.Service.GetAllAsync(isActive: true);
            var inactiveCombos = await ctx.Service.GetAllAsync(isActive: false);

            Assert.Single(activeCombos);
            Assert.Equal(activeCombo.Id, activeCombos[0].Id);

            Assert.Single(inactiveCombos);
            Assert.Equal(inactiveCombo.Id, inactiveCombos[0].Id);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task GetByIdAsync_ExistingCombo_ReturnsCombo()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);

            var result = await ctx.Service.GetByIdAsync(combo.Id);

            Assert.NotNull(result);
            Assert.Equal(combo.Id, result!.Id);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentCombo_ReturnsNull()
    {
        var ctx = CreateContext();
        try
        {
            var result = await ctx.Service.GetByIdAsync(999);

            Assert.Null(result);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task GetAvailableCombosAsync_WithInactiveProduct_ExcludesCombo()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var product3 = await CreateProductAsync(ctx.ProductRepo, "Inativo", 5m, isActive: false);

            var availableCombo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            var unavailableCombo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product3.Id);

            var result = await ctx.Service.GetAvailableCombosAsync();

            Assert.Single(result);
            Assert.Equal(availableCombo.Id, result[0].Id);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task GetAvailableCombosAsync_WithInactiveCombo_ExcludesCombo()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var activeCombo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            var inactiveCombo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            await ctx.Service.DeactivateAsync(inactiveCombo.Id);

            var result = await ctx.Service.GetAvailableCombosAsync();

            Assert.Single(result);
            Assert.Equal(activeCombo.Id, result[0].Id);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task AddComboToOrderAsync_AddsComponentsToOrder()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id, 30m);

            var order = await CreateOrderAsync(ctx.OrderService);

            var result = await ctx.Service.AddComboToOrderAsync(order.Id, combo.Id, 2);

            Assert.Equal(2, result.Items.Count);
            Assert.Contains(result.Items, i => i.ProductId == product1.Id && i.Quantity == 2);
            Assert.Contains(result.Items, i => i.ProductId == product2.Id && i.Quantity == 2);
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task AddComboToOrderAsync_WithNonexistentCombo_ThrowsInvalidOperationException()
    {
        var ctx = CreateContext();
        try
        {
            var order = await CreateOrderAsync(ctx.OrderService);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ctx.Service.AddComboToOrderAsync(order.Id, 999, 1));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task AddComboToOrderAsync_WithNonexistentOrder_ThrowsInvalidOperationException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ctx.Service.AddComboToOrderAsync(999, combo.Id, 1));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task AddComboToOrderAsync_WithFinalizedOrder_ThrowsInvalidOperationException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            var order = await CreateOrderAsync(ctx.OrderService);
            // Add an item first so the order can be closed
            await ctx.OrderService.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product1.Id, Quantity = 1 });
            await ctx.OrderService.StartPreparationAsync(order.Id);
            await ctx.OrderService.MarkAsReadyAsync(order.Id);
            await ctx.OrderService.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ctx.Service.AddComboToOrderAsync(order.Id, combo.Id, 1));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    [Fact]
    public async Task AddComboToOrderAsync_WithInactiveProduct_ThrowsInvalidOperationException()
    {
        var ctx = CreateContext();
        try
        {
            var product1 = await CreateProductAsync(ctx.ProductRepo, "X-Burguer", 20m);
            var product2 = await CreateProductAsync(ctx.ProductRepo, "Batata Frita", 8m, isActive: false);
            var combo = await CreateComboAsync(ctx.Service, ctx.ProductRepo, product1.Id, product2.Id);
            var order = await CreateOrderAsync(ctx.OrderService);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => ctx.Service.AddComboToOrderAsync(order.Id, combo.Id, 1));
        }
        finally
        {
            ctx.Provider.Dispose();
        }
    }

    private static async Task<Product> CreateProductAsync(FakeProductRepository repo, string name, decimal price, bool isActive = true)
    {
        var product = new Product
        {
            Name = name,
            Price = price,
            IsActive = isActive
        };

        await repo.AddAsync(product);

        return product;
    }

    private static async Task<ComboDto> CreateComboAsync(ComboService service, FakeProductRepository productRepo, int productId1, int productId2, decimal price = 25m)
    {
        return await service.CreateAsync(
            new CreateComboRequest
            {
                Name = "Combo Teste",
                Price = price,
                Items =
                [
                    new CreateComboItemRequest { ProductId = productId1, Quantity = 1 },
                    new CreateComboItemRequest { ProductId = productId2, Quantity = 1 }
                ]
            });
    }

    private static async Task<OrderDto> CreateOrderAsync(FakeOrderService orderService)
    {
        return await orderService.CreateAsync(new CreateOrderRequest());
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

    private sealed class FakeComboRepository : IComboRepository
    {
        private readonly List<Combo> _combos = [];
        private readonly FakeProductRepository _productRepository;
        private int _nextId = 1;

        public FakeComboRepository(FakeProductRepository productRepository)
        {
            _productRepository = productRepository;
        }

        private void PopulateProducts(Combo combo)
        {
            foreach (var item in combo.ComboItems)
            {
                if (item.Product == null)
                {
                    item.Product = _productRepository.GetByIdAsync(item.ProductId).Result;
                }
            }
        }

        public Task<IReadOnlyList<Combo>> GetAllAsync(
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Combo> query = _combos;

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            var result = query.OrderBy(c => c.Name).ToList();

            foreach (var combo in result)
            {
                PopulateProducts(combo);
            }

            return Task.FromResult<IReadOnlyList<Combo>>(result);
        }

        public Task<IReadOnlyList<Combo>> GetAvailableAsync(CancellationToken cancellationToken = default)
        {
            var result = _combos
                .Where(c => c.IsActive && c.ComboItems.All(ci => ci.Product?.IsActive == true))
                .OrderBy(c => c.Name)
                .ToList();

            foreach (var combo in result)
            {
                PopulateProducts(combo);
            }

            return Task.FromResult<IReadOnlyList<Combo>>(result);
        }

        public Task<Combo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var combo = _combos.FirstOrDefault(c => c.Id == id);
            if (combo != null)
            {
                PopulateProducts(combo);
            }
            return Task.FromResult(combo);
        }

        public Task<Combo?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
        {
            var combo = _combos.FirstOrDefault(c => c.Id == id);
            if (combo != null)
            {
                PopulateProducts(combo);
            }
            return Task.FromResult(combo);
        }

        public Task<Combo?> GetWithItemsTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            var combo = _combos.FirstOrDefault(c => c.Id == id);
            if (combo != null)
            {
                PopulateProducts(combo);
            }
            return Task.FromResult(combo);
        }

        public Task<Combo?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_combos.FirstOrDefault(c => c.Id == id));
        }

        public Task AddAsync(Combo combo, CancellationToken cancellationToken = default)
        {
            combo.Id = _nextId++;
            _combos.Add(combo);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Combo combo, CancellationToken cancellationToken = default)
        {
            var index = _combos.FindIndex(c => c.Id == combo.Id);

            if (index >= 0)
            {
                _combos[index] = combo;
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(Combo combo, CancellationToken cancellationToken = default)
        {
            _combos.Remove(combo);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = [];
        private readonly FakeOrderService _orderService;
        private readonly FakeProductRepository _productRepository;
        private int _nextId = 1;

        public FakeOrderRepository(FakeOrderService orderService, FakeProductRepository productRepository)
        {
            _orderService = orderService;
            _productRepository = productRepository;
        }

        public Task<IReadOnlyList<Order>> GetAllAsync(
            int? status = null,
            int? customerId = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null,
            string? searchTerm = null,
            string? sortBy = null,
            bool sortDescending = true,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Order> query = _orders;

            if (status.HasValue)
            {
                query = query.Where(o => (int)o.Status == status.Value);
            }

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(o => o.OrderDate >= dateFrom.Value);
            }

            if (dateTo.HasValue)
            {
                query = query.Where(o => o.OrderDate <= dateTo.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(term) ||
                    (o.Customer != null && o.Customer.Name.ToLower().Contains(term)) ||
                    o.Status.ToString().ToLower().Contains(term));
            }

            query = (sortBy?.ToLowerInvariant()) switch
            {
                "orderdate" or "datahora" => sortDescending
                    ? query.OrderByDescending(o => o.OrderDate)
                    : query.OrderBy(o => o.OrderDate),
                "status" => sortDescending
                    ? query.OrderByDescending(o => o.Status)
                    : query.OrderBy(o => o.Status),
                "customer" or "cliente" => sortDescending
                    ? query.OrderByDescending(o => o.Customer != null ? o.Customer.Name : "")
                    : query.OrderBy(o => o.Customer != null ? o.Customer.Name : ""),
                "total" or "totalamount" => sortDescending
                    ? query.OrderByDescending(o => o.TotalAmount)
                    : query.OrderBy(o => o.TotalAmount),
                _ => query.OrderByDescending(o => o.OrderDate)
            };

            return Task.FromResult<IReadOnlyList<Order>>(query.ToList());
        }

        public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
        }

        public Task<Order?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_orders.FirstOrDefault(o => o.Id == id));
        }

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
            {
                _orders[index] = order;
            }

            return Task.CompletedTask;
        }

        public Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
        {
            var query = _orders.Where(o => o.Status == OrderStatus.Pendente);

            if (customerId.HasValue)
            {
                query = query.Where(o => o.CustomerId == customerId.Value);
            }

            return Task.FromResult(query.OrderByDescending(o => o.OrderDate).FirstOrDefault());
        }

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.Pendente));
        }

        public Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null)
            {
                return Task.FromResult<IReadOnlyList<OrderItem>>(Enumerable.Empty<OrderItem>().ToList());
            }

            foreach (var item in order.OrderItems)
            {
                item.Product = _productRepository.GetByIdAsync(item.ProductId, cancellationToken).Result;
            }

            return Task.FromResult((IReadOnlyList<OrderItem>)order.OrderItems.ToList());
        }
    }

    private sealed class FakeOrderService : IOrderService
    {
        private readonly FakeOrderRepository _orderRepository;
        private readonly FakeProductRepository _productRepository;
        public event EventHandler? OrderChanged;

        public FakeOrderService(FakeOrderRepository orderRepository, FakeProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderFilterRequest? filter = null, CancellationToken cancellationToken = default)
        {
            var orders = _orderRepository.GetAllAsync(filter?.Status, filter?.CustomerId, filter?.DateFrom, filter?.DateTo, filter?.SearchTerm, filter?.SortBy, filter?.SortDescending ?? true, cancellationToken).Result;
            return Task.FromResult<IReadOnlyList<OrderDto>>(orders.Select(MapToDto).ToList());
        }

        public Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(id, cancellationToken).Result;
            return Task.FromResult(order != null ? MapToDto(order) : null);
        }

        public Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = new Order
            {
                CustomerId = request.CustomerId,
                OrderDate = DateTime.Now,
                Status = OrderStatus.Pendente,
                TotalAmount = 0,
                Notes = string.Empty
            };

            _orderRepository.AddAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> AddItemAsync(AddOrderItemRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;

            if (order is null)
            {
                throw new InvalidOperationException("O pedido informado não existe.");
            }

            if (order.Status != OrderStatus.Pendente)
            {
                throw new InvalidOperationException("Apenas pedidos abertos podem ser alterados.");
            }

            var product = _productRepository.GetByIdAsync(request.ProductId, cancellationToken).Result;

            if (product is null)
            {
                throw new InvalidOperationException("O produto informado não existe.");
            }

            if (!product.IsActive)
            {
                throw new InvalidOperationException("Não é permitido adicionar produto inativo ao pedido.");
            }

            if (request.Quantity <= 0)
            {
                throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(request.Quantity));
            }

            var orderItem = new OrderItem
            {
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price
            };

            orderItem.Validate();

            order.OrderItems.Add(orderItem);
            order.RecalculateTotal();

            _orderRepository.UpdateAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> RemoveItemAsync(int orderId, int orderItemId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;

            if (order is null)
            {
                throw new InvalidOperationException("O pedido informado não existe.");
            }

            if (order.Status != OrderStatus.Pendente)
            {
                throw new InvalidOperationException("Apenas pedidos abertos podem ser alterados.");
            }

            var item = order.OrderItems.FirstOrDefault(i => i.Id == orderItemId);

            if (item is null)
            {
                throw new InvalidOperationException("Item não encontrado no pedido.");
            }

            order.OrderItems.Remove(item);
            order.RecalculateTotal();

            _orderRepository.UpdateAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> UpdateItemQuantityAsync(UpdateOrderItemRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;

            if (order is null)
            {
                throw new InvalidOperationException("O pedido informado não existe.");
            }

            if (order.Status != OrderStatus.Pendente)
            {
                throw new InvalidOperationException("Apenas pedidos abertos podem ser alterados.");
            }

            var item = order.OrderItems.FirstOrDefault(i => i.Id == request.OrderItemId);

            if (item is null)
            {
                throw new InvalidOperationException("Item não encontrado no pedido.");
            }

            if (request.Quantity <= 0)
            {
                throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(request.Quantity));
            }

            item.Quantity = request.Quantity;
            item.Validate();

            order.RecalculateTotal();

            _orderRepository.UpdateAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> CloseAsync(CloseOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;

            if (order is null)
            {
                throw new InvalidOperationException("O pedido informado não existe.");
            }

            if (order.Status != OrderStatus.Pronto)
            {
                throw new InvalidOperationException("Apenas pedidos prontos podem ser finalizados.");
            }

            if (!order.OrderItems.Any())
            {
                throw new InvalidOperationException("Pedido não pode ser finalizado sem itens.");
            }

            order.Finalize();

            _orderRepository.UpdateAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;

            if (order is null)
            {
                throw new InvalidOperationException("O pedido informado não existe.");
            }

            if (order.Status != OrderStatus.Pendente)
            {
                throw new InvalidOperationException("Apenas pedidos abertos podem ser cancelados.");
            }

            order.Cancel();

            _orderRepository.UpdateAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(MapToDto(order));
        }

        public Task<CloseOrderWithPaymentResult> CloseWithPaymentAsync(CloseOrderWithPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;

            if (order is null)
            {
                return Task.FromResult(new CloseOrderWithPaymentResult(false, null, null, null, "O pedido informado não existe."));
            }

            if (order.Status != OrderStatus.Pendente)
            {
                return Task.FromResult(new CloseOrderWithPaymentResult(false, null, null, null, "Apenas pedidos abertos podem ser fechados."));
            }

            if (!order.OrderItems.Any())
            {
                return Task.FromResult(new CloseOrderWithPaymentResult(false, null, null, null, "Pedido não pode ser fechado sem itens."));
            }

            order.Finalize();

            _orderRepository.UpdateAsync(order, cancellationToken).Wait();

            OrderChanged?.Invoke(this, EventArgs.Empty);

            return Task.FromResult(new CloseOrderWithPaymentResult(true, order.Id, null, null, null));
        }

        public Task<IReadOnlyList<ProductSalesDto>> GetTopProductsAsync(int topCount = 5, int days = 7, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<ProductSalesDto>>(Array.Empty<ProductSalesDto>());
        }

        public Task<OrderDto> RecalculateWithPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(MapToDto(_orderRepository.GetByIdAsync(orderId, cancellationToken).Result!));
        }

        public Task<IReadOnlyList<PromotionDto>> GetAppliedPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PromotionDto>>(Array.Empty<PromotionDto>());
        }

        public Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<PromotionDto>>(Array.Empty<PromotionDto>());
        }

        public Task<OrderDto> StartPreparationAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;
            if (order is null) throw new InvalidOperationException("O pedido informado não existe.");
            if (order.Status != OrderStatus.Pendente) throw new InvalidOperationException("Apenas pedidos pendentes podem iniciar preparação.");
            order.StartPreparation();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> MarkAsReadyAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;
            if (order is null) throw new InvalidOperationException("O pedido informado não existe.");
            if (order.Status != OrderStatus.EmPreparacao) throw new InvalidOperationException("Apenas pedidos em preparo podem ser marcados como prontos.");
            order.MarkAsReady();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> FinalizeAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;
            if (order is null) throw new InvalidOperationException("O pedido informado não existe.");
            if (order.Status != OrderStatus.Pronto) throw new InvalidOperationException("Apenas pedidos prontos podem ser finalizados.");
            if (!order.OrderItems.Any()) throw new InvalidOperationException("Pedido não pode ser finalizado sem itens.");
            order.Finalize();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            OrderChanged?.Invoke(this, EventArgs.Empty);
            return Task.FromResult(MapToDto(order));
        }

        private static OrderDto MapToDto(Order order)
        {
            return new OrderDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                CustomerName = order.Customer?.Name,
                OrderDate = order.OrderDate,
                Status = order.Status,
                TotalAmount = order.TotalAmount,
                Notes = order.Notes,
                Items = order.OrderItems.Select(i => new OrderItemDto
                {
                    Id = i.Id,
                    OrderId = i.OrderId,
                    ProductId = i.ProductId,
                    ProductName = i.Product?.Name ?? string.Empty,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    Subtotal = i.Subtotal
                }).ToList()
            };
        }
    }
}