using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class InventoryOrderIntegrationTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeProductRepository _productRepository;
    private readonly FakeCategoryRepository _categoryRepository;
    private readonly FakeCustomerRepository _customerRepository;
    private readonly FakeComboRepository _comboRepository;
    private readonly FakeOrderRepository _orderRepository;
    private readonly FakeOrderService _fakeOrderService;
    private readonly FakeInventoryRepository _inventoryRepository;
    private readonly FakeInventoryMovementRepository _inventoryMovementRepository;
    private readonly FakePromotionService _promotionService;
    private readonly FakePaymentRepository _paymentRepository;
    private readonly FakePendingPaymentRepository _pendingPaymentRepository;
    private readonly ProductService _productService;
    private readonly ComboService _comboService;
    private readonly OrderService _orderService;
    private readonly InventoryService _inventoryService;

    public InventoryOrderIntegrationTests()
    {
        var services = new ServiceCollection();
        _categoryRepository = new FakeCategoryRepository();
        _productRepository = new FakeProductRepository();
        _customerRepository = new FakeCustomerRepository();
        _inventoryRepository = new FakeInventoryRepository();
        _inventoryMovementRepository = new FakeInventoryMovementRepository();
        _promotionService = new FakePromotionService();
        _paymentRepository = new FakePaymentRepository();
        _pendingPaymentRepository = new FakePendingPaymentRepository(_customerRepository);

        _orderRepository = new FakeOrderRepository(_customerRepository, _productRepository);
        _fakeOrderService = new FakeOrderService(_orderRepository, _productRepository);
        _comboRepository = new FakeComboRepository(_productRepository);

        // Register repositories and basic services
        services.AddSingleton<IProductRepository>(_productRepository);
        services.AddSingleton<ICategoryRepository>(_categoryRepository);
        services.AddSingleton<ICustomerRepository>(_customerRepository);
        services.AddSingleton<IOrderRepository>(_orderRepository);
        services.AddSingleton<IComboRepository>(_comboRepository);
        services.AddSingleton<IPromotionService>(_promotionService);
        services.AddSingleton<IPaymentRepository>(_paymentRepository);
        services.AddSingleton<IPendingPaymentRepository>(_pendingPaymentRepository);
        var inventoryService = new InventoryService(_inventoryRepository, _inventoryMovementRepository);
        services.AddSingleton<IInventoryService>(inventoryService);

        // Build service provider to get IServiceScopeFactory
        _serviceProvider = services.BuildServiceProvider();
        var scopeFactory = _serviceProvider.GetRequiredService<IServiceScopeFactory>();

        // Create services in dependency order (no circular deps)
        var paymentService = new PaymentService(_paymentRepository, _orderRepository);
        var pendingPaymentService = new PendingPaymentService(_pendingPaymentRepository, _orderRepository, _customerRepository);
        var orderPromotionService = new OrderPromotionService(_orderRepository, _promotionService);
        var orderPaymentService = new OrderPaymentService(_orderRepository, _customerRepository, paymentService, pendingPaymentService, inventoryService);
        _orderService = new OrderService(_orderRepository, _customerRepository, _productRepository, inventoryService, orderPromotionService, orderPaymentService);

        _productService = new ProductService(scopeFactory, inventoryService);
        _comboService = new ComboService(scopeFactory, _productRepository, _orderRepository, _orderService, NullLogger<ComboService>.Instance);

        // Register the created services
        services.AddSingleton<IProductService>(_productService);
        services.AddSingleton<IComboService>(_comboService);
        services.AddSingleton<IOrderService>(_orderService);

        // Rebuild service provider with all services
        _serviceProvider = services.BuildServiceProvider();

        _inventoryService = inventoryService;

        // Seed category
        var category = new Category { Name = "Lanches", IsActive = true };
        _categoryRepository.AddAsync(category).Wait();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private async Task AdvanceOrderToReadyAsync(int orderId)
    {
        await _orderService.StartPreparationAsync(orderId);
        await _orderService.MarkAsReadyAsync(orderId);
    }

    [Fact]
    public async Task FullFlow_ProductWithInventory_OrderClosed_StockDeducted()
    {
        // Arrange - Create category
        var category = await CreateCategoryAsync("Lanches");

        // Act - Create product with inventory
        var product = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "X-Burguer",
            CategoryId = category.Id,
            Price = 25.50m,
            InventoryRequest = new CreateInventoryItemRequest(
                ProductId: null,
                Nome: "X-Burguer 150g",
                QuantidadeAtual: 50,
                QuantidadeMinima: 10,
                ValorUnitario: 8.50m,
                UnidadeMedida: "UN",
                Observacoes: "Estoque inicial")
        });

        // Verify product created with inventory
        Assert.True(product.Id > 0);
        var inventoryItems = await _inventoryRepository.GetAllAsync(null, null, default);
        var item = inventoryItems.FirstOrDefault(i => i.ProductId == product.Id);
        Assert.NotNull(item);
        Assert.Equal(50, item.QuantidadeAtual);

        // Create order
        var order = await _orderService.CreateAsync(new CreateOrderRequest());

        // Add product to order
        await _orderService.AddItemAsync(new AddOrderItemRequest
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 2
        });

        // Close order
        await AdvanceOrderToReadyAsync(order.Id);
        await _orderService.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        // Assert - Verify stock was deducted
        var updatedItem = await _inventoryRepository.GetByProductIdAsync(product.Id, default);
        Assert.NotNull(updatedItem);
        Assert.Equal(48, updatedItem.QuantidadeAtual); // 50 - 2 = 48

        // Verify inventory movement was created
        var movements = await _inventoryMovementRepository.GetByOrderAsync(order.Id, default);
        Assert.Single(movements);
        Assert.Equal(InventoryMovementType.Saida, movements[0].Tipo);
        Assert.Equal(InventoryMovementReason.Venda, movements[0].Motivo);
        Assert.Equal(2, movements[0].Quantidade);
        Assert.Equal(order.Id, movements[0].OrderId);
    }

    [Fact]
    public async Task FullFlow_ComboWithComponents_OrderClosed_AllComponentsDeducted()
    {
        // Arrange - Create category
        var category = await CreateCategoryAsync("Lanches");

        // Create products with inventory
        var burger = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "Hambúrguer",
            CategoryId = category.Id,
            Price = 15m,
            InventoryRequest = new CreateInventoryItemRequest(null, "Hambúrguer 150g", 30, 5, 8m, "UN", null)
        });

        var fries = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "Batata Frita",
            CategoryId = category.Id,
            Price = 8m,
            InventoryRequest = new CreateInventoryItemRequest(null, "Batata Frita 200g", 40, 10, 3m, "UN", null)
        });

        var drink = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "Refrigerante",
            CategoryId = category.Id,
            Price = 6m,
            InventoryRequest = new CreateInventoryItemRequest(null, "Refrigerante 350ml", 50, 10, 2m, "UN", null)
        });

        // Create combo
        var combo = await _comboService.CreateAsync(new CreateComboRequest
        {
            Name = "Combo Completo",
            Description = "Hambúrguer + Batata + Refrigerante",
            Price = 25m,
            Items =
            [
                new CreateComboItemRequest { ProductId = burger.Id, Quantity = 1 },
                new CreateComboItemRequest { ProductId = fries.Id, Quantity = 1 },
                new CreateComboItemRequest { ProductId = drink.Id, Quantity = 1 }
            ]
        });

        // Create order
        var order = await _orderService.CreateAsync(new CreateOrderRequest());

        // Add combo to order (quantity = 2)
        await _comboService.AddComboToOrderAsync(order.Id, combo.Id, 2);

        // Close order
        await AdvanceOrderToReadyAsync(order.Id);
        await _orderService.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        // Assert - Verify all components were deducted
        var burgerItem = await _inventoryRepository.GetByProductIdAsync(burger.Id, default);
        var friesItem = await _inventoryRepository.GetByProductIdAsync(fries.Id, default);
        var drinkItem = await _inventoryRepository.GetByProductIdAsync(drink.Id, default);

        Assert.NotNull(burgerItem);
        Assert.NotNull(friesItem);
        Assert.NotNull(drinkItem);

        // 2 combos = 2 of each component
        Assert.Equal(28, burgerItem.QuantidadeAtual); // 30 - 2 = 28
        Assert.Equal(38, friesItem.QuantidadeAtual); // 40 - 2 = 38
        Assert.Equal(48, drinkItem.QuantidadeAtual); // 50 - 2 = 48

        // Verify movements for each component with ComboId
        var movements = await _inventoryMovementRepository.GetByOrderAsync(order.Id, default);
        Assert.Equal(3, movements.Count);

        foreach (var movement in movements)
        {
            Assert.Equal(InventoryMovementType.Saida, movement.Tipo);
            Assert.Equal(InventoryMovementReason.Venda, movement.Motivo);
            Assert.Equal(2, movement.Quantidade);
            Assert.Equal(order.Id, movement.OrderId);
            Assert.Equal(combo.Id, movement.ComboId);
        }
    }

    [Fact]
    public async Task FullFlow_OrderClosedFiado_StockDeductedAndPendingPaymentCreated()
    {
        // Arrange
        var category = await CreateCategoryAsync("Lanches");
        var customer = await CreateCustomerAsync(creditLimit: 500m);

        var product = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "X-Burguer",
            CategoryId = category.Id,
            Price = 25m,
            InventoryRequest = new CreateInventoryItemRequest(null, "X-Burguer", 20, 5, 8m, "UN", null)
        });

        // Act - Create order with customer
        var order = await _orderService.CreateAsync(new CreateOrderRequest { CustomerId = customer.Id });
        await _orderService.AddItemAsync(new AddOrderItemRequest
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1
        });
        await AdvanceOrderToReadyAsync(order.Id);

        // Close with fiado payment
        var result = await _orderService.CloseWithPaymentAsync(new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            25m,
            1,
            "Fiado",
            customer.Id
        ));

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.PendingPayment);

        var item = await _inventoryRepository.GetByProductIdAsync(product.Id, default);
        Assert.Equal(19, item!.QuantidadeAtual); // 20 - 1 = 19

        // Verify inventory movement
        var movements = await _inventoryMovementRepository.GetByOrderAsync(order.Id, default);
        Assert.Single(movements);
        Assert.Equal(InventoryMovementReason.Venda, movements[0].Motivo);
    }

    [Fact]
    public async Task FullFlow_DeliveredOrderCancelled_StockRestored()
    {
        // Arrange
        var category = await CreateCategoryAsync("Lanches");

        var product = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "X-Burguer",
            CategoryId = category.Id,
            Price = 25m,
            InventoryRequest = new CreateInventoryItemRequest(null, "X-Burguer", 20, 5, 8m, "UN", null)
        });

        var order = await _orderService.CreateAsync(new CreateOrderRequest());
        await _orderService.AddItemAsync(new AddOrderItemRequest
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 3
        });

        // Close order
        await AdvanceOrderToReadyAsync(order.Id);
        await _orderService.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        // Verify stock deducted
        var itemAfterClose = await _inventoryRepository.GetByProductIdAsync(product.Id, default);
        Assert.Equal(17, itemAfterClose!.QuantidadeAtual); // 20 - 3 = 17

        // Act - Cancel closed order (should restore stock)
        await _orderService.CancelAsync(new CancelOrderRequest { OrderId = order.Id });

        // Assert - Stock restored
        var itemAfterCancel = await _inventoryRepository.GetByProductIdAsync(product.Id, default);
        Assert.Equal(20, itemAfterCancel!.QuantidadeAtual); // Restored to 20

        // Verify estorno movement created
        var movements = await _inventoryMovementRepository.GetByOrderAsync(order.Id, default);
        var estornoMovements = movements.Where(m => m.Motivo == InventoryMovementReason.EstornoVenda).ToList();
        Assert.Single(estornoMovements);
        Assert.Equal(3, estornoMovements[0].Quantidade);
        Assert.Equal(InventoryMovementType.Entrada, estornoMovements[0].Tipo);
        Assert.Equal(InventoryMovementReason.EstornoVenda, estornoMovements[0].Motivo);
    }

    [Fact]
    public async Task FullFlow_ProductWithoutInventory_OrderClosed_LogsWarningButDoesNotBlock()
    {
        // Arrange
        var category = await CreateCategoryAsync("Lanches");

        // Product WITHOUT inventory
        var product = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "X-Burguer",
            CategoryId = category.Id,
            Price = 25m
        });

        var order = await _orderService.CreateAsync(new CreateOrderRequest());
        await _orderService.AddItemAsync(new AddOrderItemRequest
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 2
        });

        // Act - Close order (should not throw)
        await AdvanceOrderToReadyAsync(order.Id);
        await _orderService.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        // Assert - Order closed successfully
        var closedOrder = await _orderRepository.GetByIdAsync(order.Id, default);
        Assert.Equal(OrderStatus.Finalizado, closedOrder!.Status);

        // Verify NO log movement was created for product without inventory (current behavior)
        var movements = await _inventoryMovementRepository.GetByOrderAsync(order.Id, default);
        Assert.Empty(movements);
    }

    [Fact]
    public async Task FullFlow_InsufficientStock_OrderClosed_AllowsNegativeWithAlert()
    {
        // Arrange
        var category = await CreateCategoryAsync("Lanches");

        var product = await _productService.CreateAsync(new CreateProductRequest
        {
            Name = "X-Burguer",
            CategoryId = category.Id,
            Price = 25m,
            InventoryRequest = new CreateInventoryItemRequest(null, "X-Burguer", 5, 5, 8m, "UN", null)
        });

        var order = await _orderService.CreateAsync(new CreateOrderRequest());
        await _orderService.AddItemAsync(new AddOrderItemRequest
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 10 // More than available stock
        });

        // Act - Close order (should allow negative stock)
        await AdvanceOrderToReadyAsync(order.Id);
        await _orderService.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        // Assert - Stock went negative
        var item = await _inventoryRepository.GetByProductIdAsync(product.Id, default);
        Assert.Equal(-5, item!.QuantidadeAtual); // 5 - 10 = -5
        Assert.True(item.EstoqueZerado);
    }

    private async Task<Category> CreateCategoryAsync(string name)
    {
        var category = new Category { Name = name, IsActive = true };
        await _categoryRepository.AddAsync(category);
        return category;
    }

    private async Task<Customer> CreateCustomerAsync(string name = "Test", string phone = "11999999999", decimal creditLimit = 0m)
    {
        var customer = new Customer { Name = name, Phone = phone, IsActive = true, CreditLimit = creditLimit };
        await _customerRepository.AddAsync(customer);
        return customer;
    }

    #region Fakes

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Category>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Category> query = _categories;
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<Category>>(query.OrderBy(c => c.Name).ToList());
        }

        public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Category?>(_categories.FirstOrDefault(c => c.Id == id));

        public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = _categories.Any(c => c.Id != excludeId && c.Name.Trim().ToUpperInvariant() == normalizedName);
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
            var index = _categories.FindIndex(c => c.Id == category.Id);
            if (index >= 0) _categories[index] = category;
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
                query = query.Where(p => p.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<Product>>(query.OrderBy(p => p.Name).ToList());
        }

        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Product?>(_products.FirstOrDefault(p => p.Id == id));

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            product.Id = _nextId++;
            _products.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            var index = _products.FindIndex(p => p.Id == product.Id);
            if (index >= 0) _products[index] = product;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly List<Customer> _customers = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Customer>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer> query = _customers;
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<Customer>>(query.ToList());
        }

        public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Customer?>(_customers.FirstOrDefault(c => c.Id == id));

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            customer.Id = _nextId++;
            _customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            var index = _customers.FindIndex(c => c.Id == customer.Id);
            if (index >= 0) _customers[index] = customer;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_customers.Any(c => c.Id == id));
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

        public Task<IReadOnlyList<Combo>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Combo> query = _combos;
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<Combo>>(query.OrderBy(c => c.Name).ToList());
        }

        public Task<Combo?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Combo?>(_combos.FirstOrDefault(c => c.Id == id));

        public Task<Combo?> GetWithItemsAsync(int id, CancellationToken cancellationToken = default)
        {
            var combo = _combos.FirstOrDefault(c => c.Id == id);
            if (combo != null)
            {
                // Load product names
                foreach (var item in combo.ComboItems)
                {
                    var product = _productRepository.GetByIdAsync(item.ProductId, cancellationToken).Result;
                    if (product != null) item.Product = product;
                }
            }
            return Task.FromResult<Combo?>(combo);
        }

        public Task<Combo?> GetWithItemsTrackedAsync(int id, CancellationToken cancellationToken = default)
            => GetWithItemsAsync(id, cancellationToken);

        public Task<Combo?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
            => GetByIdAsync(id, cancellationToken);

        public Task<IReadOnlyList<Combo>> GetAvailableAsync(CancellationToken cancellationToken = default)
        {
            var available = _combos.Where(c => c.IsAvailable).OrderBy(c => c.Name).ToList();
            return Task.FromResult<IReadOnlyList<Combo>>(available);
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
            if (index >= 0) _combos[index] = combo;
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
        private readonly FakeCustomerRepository _customerRepository;
        private readonly FakeProductRepository _productRepository;
        private int _nextId = 1;

        public FakeOrderRepository(FakeCustomerRepository customerRepository, FakeProductRepository productRepository)
        {
            _customerRepository = customerRepository;
            _productRepository = productRepository;
        }

        public Task<IReadOnlyList<Order>> GetAllAsync(int? status = null, int? customerId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? searchTerm = null, string? sortBy = null, bool sortDescending = true, CancellationToken cancellationToken = default)
        {
            IEnumerable<Order> query = _orders;
            if (status.HasValue)
                query = query.Where(o => o.Status == (OrderStatus)status.Value);
            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);
            if (dateFrom.HasValue)
                query = query.Where(o => o.OrderDate >= dateFrom.Value);
            if (dateTo.HasValue)
                query = query.Where(o => o.OrderDate <= dateTo.Value);

            var result = sortDescending ? query.OrderByDescending(o => o.OrderDate) : query.OrderBy(o => o.OrderDate);
            return Task.FromResult<IReadOnlyList<Order>>(result.ToList());
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
            if (index >= 0) _orders[index] = order;
            return Task.CompletedTask;
        }

        public Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(_orders.Where(o => o.Status == OrderStatus.Pendente && (!customerId.HasValue || o.CustomerId == customerId)).OrderByDescending(o => o.OrderDate).FirstOrDefault());

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.Pendente));

        public Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == orderId);
            if (order == null) return Task.FromResult((IReadOnlyList<OrderItem>)Enumerable.Empty<OrderItem>().ToList().AsReadOnly());

            foreach (var item in order.OrderItems)
            {
                item.Product = _productRepository.GetByIdAsync(item.ProductId, cancellationToken).Result;
            }

            return Task.FromResult((IReadOnlyList<OrderItem>)order.OrderItems.ToList().AsReadOnly());
        }
    }

    private sealed class FakeOrderService : IOrderService
    {
        private readonly FakeOrderRepository _orderRepository;
        private readonly FakeProductRepository _productRepository;

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
            return Task.FromResult<OrderDto?>(order != null ? MapToDto(order) : null);
        }

        public Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = new Order { CustomerId = request.CustomerId, OrderDate = DateTime.Now, Status = OrderStatus.Pendente, TotalAmount = 0, Notes = string.Empty };
            _orderRepository.AddAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> AddItemAsync(AddOrderItemRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;
            var product = _productRepository.GetByIdAsync(request.ProductId, cancellationToken).Result;
            if (order == null || product == null) throw new InvalidOperationException("Order or product not found");

            var orderItem = new OrderItem { OrderId = request.OrderId, ProductId = request.ProductId, Quantity = request.Quantity, UnitPrice = product.Price, ComboId = request.ComboId };
            order.OrderItems.Add(orderItem);
            order.RecalculateTotal();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> RemoveItemAsync(int orderId, int orderItemId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<OrderDto> UpdateItemQuantityAsync(UpdateOrderItemRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<OrderDto> CloseAsync(CloseOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;
            if (order == null) throw new InvalidOperationException("Order not found");
            order.Finalize();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public Task<CloseOrderWithPaymentResult> CloseWithPaymentAsync(CloseOrderWithPaymentRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<OrderDto> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(request.OrderId, cancellationToken).Result;
            if (order == null) throw new InvalidOperationException("Order not found");
            order.Cancel();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public Task<IReadOnlyList<ProductSalesDto>> GetTopProductsAsync(int topCount = 5, int days = 7, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<OrderDto> RecalculateWithPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<PromotionDto>> GetAppliedPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<OrderDto> StartPreparationAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;
            if (order == null) throw new InvalidOperationException("Order not found");
            if (order.Status != OrderStatus.Pendente) throw new InvalidOperationException("Only pending orders can start preparation");
            order.StartPreparation();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> MarkAsReadyAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;
            if (order == null) throw new InvalidOperationException("Order not found");
            if (order.Status != OrderStatus.EmPreparacao) throw new InvalidOperationException("Only preparing orders can be marked as ready");
            order.MarkAsReady();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public Task<OrderDto> FinalizeAsync(int orderId, CancellationToken cancellationToken = default)
        {
            var order = _orderRepository.GetByIdAsync(orderId, cancellationToken).Result;
            if (order == null) throw new InvalidOperationException("Order not found");
            if (order.Status != OrderStatus.Pronto) throw new InvalidOperationException("Only ready orders can be finalized");
            if (!order.OrderItems.Any()) throw new InvalidOperationException("Order cannot be finalized without items");
            order.Finalize();
            _orderRepository.UpdateAsync(order, cancellationToken).Wait();
            return Task.FromResult(MapToDto(order));
        }

        public event EventHandler? OrderChanged;

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
                    Subtotal = i.Subtotal,
                    ComboId = i.ComboId
                }).ToList()
            };
        }
    }

    private sealed class FakePromotionService : IPromotionService
    {
        public Task<IReadOnlyList<PromotionDto>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PromotionDto>>([]);

        public Task<PromotionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PromotionDto?>(null);

        public Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PromotionDto> UpdateAsync(UpdatePromotionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<decimal> CalculateDiscountAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PromotionDto>>([]);
    }

    private sealed class FakePaymentRepository : IPaymentRepository
    {
        private readonly List<Payment> _payments = [];
        private int _nextId = 1;

        public Task<Payment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Payment?>(_payments.FirstOrDefault(p => p.Id == id));

        public Task<Payment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Payment?>(_payments.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Payment>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Payment>>(_payments.Where(p => p.OrderId == orderId).ToList());

        public Task<IReadOnlyList<Payment>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<Payment>>(_payments.ToList());

        public Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            payment.Id = _nextId++;
            _payments.Add(payment);
            return Task.FromResult(payment);
        }

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            var index = _payments.FindIndex(p => p.Id == payment.Id);
            if (index >= 0) _payments[index] = payment;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePendingPaymentRepository : IPendingPaymentRepository
    {
        private readonly List<PendingPayment> _pendingPayments = [];
        private readonly FakeCustomerRepository _customerRepository;
        private int _nextId = 1;

        public FakePendingPaymentRepository(FakeCustomerRepository customerRepository)
        {
            _customerRepository = customerRepository;
        }

        public Task<PendingPayment?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PendingPayment?>(_pendingPayments.FirstOrDefault(p => p.Id == id));

        public Task<PendingPayment?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PendingPayment?>(_pendingPayments.FirstOrDefault(p => p.Id == id));

        public Task<PendingPayment?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<PendingPayment?>(_pendingPayments.FirstOrDefault(p => p.OrderId == orderId));

        public Task<IReadOnlyList<PendingPayment>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPayment>>(_pendingPayments.Where(p => p.CustomerId == customerId).ToList());

        public Task<IReadOnlyList<PendingPayment>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPayment>>(_pendingPayments.ToList());

        public Task<IReadOnlyList<PendingPayment>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPayment>>(_pendingPayments.Where(p => p.StatusPagamento == status).ToList());

        public Task<PendingPayment> AddAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
        {
            pendingPayment.Id = _nextId++;
            var customer = _customerRepository.GetByIdAsync(pendingPayment.CustomerId, cancellationToken).Result;
            if (customer != null) pendingPayment.Customer = customer;
            _pendingPayments.Add(pendingPayment);
            return Task.FromResult(pendingPayment);
        }

        public Task UpdateAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
        {
            var index = _pendingPayments.FindIndex(p => p.Id == pendingPayment.Id);
            if (index >= 0) _pendingPayments[index] = pendingPayment;
            return Task.CompletedTask;
        }

        public Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var sum = _pendingPayments.Where(p => p.CustomerId == customerId && p.StatusPagamento == StatusPagamentoPendente.NaoPago).Sum(p => p.SaldoPendente);
            return Task.FromResult(sum);
        }

        public Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<DevedorDto>>([]);
    }

    private sealed class FakeInventoryRepository : IInventoryRepository
    {
        private readonly List<InventoryItem> _items = [];
        private int _nextId = 1;

        public Task<InventoryItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<InventoryItem?>(_items.FirstOrDefault(i => i.Id == id));

        public Task<InventoryItem?> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
            => Task.FromResult<InventoryItem?>(_items.FirstOrDefault(i => i.ProductId == productId));

        public Task<IReadOnlyList<InventoryItem>> GetAllAsync(bool? apenasBaixo, string? searchTerm, CancellationToken cancellationToken = default)
        {
            IEnumerable<InventoryItem> query = _items;
            if (apenasBaixo == true)
                query = query.Where(i => i.EstoqueBaixo);
            if (!string.IsNullOrWhiteSpace(searchTerm))
                query = query.Where(i => i.Nome.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            return Task.FromResult<IReadOnlyList<InventoryItem>>(query.OrderBy(i => i.Nome).ToList());
        }

        public Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
        {
            item.Id = _nextId++;
            _items.Add(item);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
        {
            var index = _items.FindIndex(i => i.Id == item.Id);
            if (index >= 0) _items[index] = item;
            return Task.CompletedTask;
        }

        public Task<decimal> GetValorTotalEstoqueAsync(CancellationToken cancellationToken = default)
        {
            var total = _items.Sum(i => i.QuantidadeAtual * i.ValorUnitario);
            return Task.FromResult(total);
        }
    }

    private sealed class FakeInventoryMovementRepository : IInventoryMovementRepository
    {
        private readonly List<InventoryMovement> _movements = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<InventoryMovement>> GetByItemAsync(int itemId, DateTime? dataInicio, DateTime? dataFim, CancellationToken cancellationToken = default)
        {
            var query = _movements.Where(m => m.InventoryItemId == itemId);
            if (dataInicio.HasValue) query = query.Where(m => m.DataHora >= dataInicio.Value);
            if (dataFim.HasValue) query = query.Where(m => m.DataHora <= dataFim.Value);
            return Task.FromResult<IReadOnlyList<InventoryMovement>>(query.OrderByDescending(m => m.DataHora).ToList());
        }

        public Task<IReadOnlyList<InventoryMovement>> GetByOrderAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<InventoryMovement>>(_movements.Where(m => m.OrderId == orderId).OrderByDescending(m => m.DataHora).ToList());

        public Task AddAsync(InventoryMovement movement, CancellationToken cancellationToken = default)
        {
            movement.Id = _nextId++;
            _movements.Add(movement);
            return Task.CompletedTask;
        }
    }

    #endregion
}