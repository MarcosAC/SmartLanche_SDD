using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Application.Tests.Fakes;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class OrderServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeOrderRepository _orderRepository;
    private readonly FakeCustomerRepository _customerRepository;
    private readonly FakeProductRepository _productRepository;
    private readonly FakeCategoryRepository _categoryRepository;
    private readonly FakePromotionService _promotionService;
    private readonly FakeInventoryService _inventoryService;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        var services = new ServiceCollection();
        _customerRepository = new FakeCustomerRepository();
        _productRepository = new FakeProductRepository();
        _categoryRepository = new FakeCategoryRepository();
        _orderRepository = new FakeOrderRepository(_customerRepository, _productRepository);
        _promotionService = new FakePromotionService();
        _inventoryService = new FakeInventoryService();

        services.AddSingleton<IOrderRepository>(_orderRepository);
        services.AddSingleton<IPromotionService>(_promotionService);
        services.AddSingleton<ICustomerRepository>(_customerRepository);
        services.AddSingleton<IProductRepository>(_productRepository);
        services.AddSingleton<ICategoryRepository>(_categoryRepository);
        _serviceProvider = services.BuildServiceProvider();

        var orderPromotionService = new OrderPromotionService(
            _orderRepository,
            _promotionService);

        var paymentService = new PaymentService(new FakePaymentRepository(), _orderRepository);
        var pendingPaymentService = new PendingPaymentService(new FakePendingPaymentRepository(_customerRepository), _orderRepository, _customerRepository);

        _service = new OrderService(_orderRepository, _customerRepository, _productRepository, _inventoryService, orderPromotionService, paymentService, pendingPaymentService);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private async Task<OrderDto> AdvanceOrderToReadyAsync(int orderId)
    {
        // Pendente -> EmPreparacao
        await _service.StartPreparationAsync(orderId);
        // EmPreparacao -> Pronto
        return await _service.MarkAsReadyAsync(orderId);
    }

    [Fact]
    public async Task CreateAsync_WithoutCustomer_CreatesOrder()
    {
        var result = await _service.CreateAsync(new CreateOrderRequest());

        Assert.True(result.Id > 0);
        Assert.Null(result.CustomerId);
        Assert.Equal(OrderStatus.Pendente, result.Status);
        Assert.Equal(0m, result.TotalAmount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task CreateAsync_WithValidCustomer_CreatesOrder()
    {
        var customer = await CreateCustomerAsync();

        var result = await _service.CreateAsync(new CreateOrderRequest { CustomerId = customer.Id });

        Assert.True(result.Id > 0);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(customer.Name, result.CustomerName);
        Assert.Equal(OrderStatus.Pendente, result.Status);
        Assert.Equal(0m, result.TotalAmount);
    }

    [Fact]
    public async Task CreateAsync_WithNonexistentCustomer_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(new CreateOrderRequest { CustomerId = 999 }));
    }

    [Fact]
    public async Task CreateAsync_WithInactiveCustomer_ThrowsInvalidOperationException()
    {
        var customer = await CreateCustomerAsync(isActive: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CreateAsync(new CreateOrderRequest { CustomerId = customer.Id }));
    }

    [Fact]
    public async Task AddItemAsync_WithValidData_AddsItemAndCalculatesTotal()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 25.50m);

        var result = await _service.AddItemAsync(
            new AddOrderItemRequest
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = 2
            });

        Assert.Equal(OrderStatus.Pendente, result.Status);
        Assert.Equal(51.00m, result.TotalAmount);
        Assert.Single(result.Items);
        Assert.Equal(product.Id, result.Items[0].ProductId);
        Assert.Equal(product.Name, result.Items[0].ProductName);
        Assert.Equal(2, result.Items[0].Quantity);
        Assert.Equal(25.50m, result.Items[0].UnitPrice);
        Assert.Equal(51.00m, result.Items[0].Subtotal);
    }

    [Fact]
    public async Task AddItemAsync_ToNonexistentOrder_ThrowsInvalidOperationException()
    {
        var product = await CreateProductAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddItemAsync(
                new AddOrderItemRequest
                {
                    OrderId = 999,
                    ProductId = product.Id,
                    Quantity = 1
                }));
    }

    [Fact]
    public async Task AddItemAsync_ToFinalizedOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(order.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddItemAsync(
                new AddOrderItemRequest
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = 1
                }));
    }

    [Fact]
    public async Task AddItemAsync_WithNonexistentProduct_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddItemAsync(
                new AddOrderItemRequest
                {
                    OrderId = order.Id,
                    ProductId = 999,
                    Quantity = 1
                }));
    }

    [Fact]
    public async Task AddItemAsync_WithInactiveProduct_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(isActive: false);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.AddItemAsync(
                new AddOrderItemRequest
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = 1
                }));
    }

    [Fact]
    public async Task AddItemAsync_WithZeroQuantity_ThrowsArgumentException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(
                new AddOrderItemRequest
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = 0
                }));
    }

    [Fact]
    public async Task AddItemAsync_WithNegativeQuantity_ThrowsArgumentException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.AddItemAsync(
                new AddOrderItemRequest
                {
                    OrderId = order.Id,
                    ProductId = product.Id,
                    Quantity = -1
                }));
    }

    [Fact]
    public async Task AddItemAsync_LocksUnitPriceToProductPrice()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 30.00m);

        var result = await _service.AddItemAsync(
            new AddOrderItemRequest
            {
                OrderId = order.Id,
                ProductId = product.Id,
                Quantity = 1
            });

        Assert.Equal(30.00m, result.Items[0].UnitPrice);
    }

    [Fact]
    public async Task AddMultipleItems_CalculatesTotalCorrectly()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product1 = await CreateProductAsync(price: 25.50m);
        var product2 = await CreateProductAsync(price: 10.00m);

        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product1.Id, Quantity = 2 });
        var result = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product2.Id, Quantity = 3 });

        Assert.Equal(81.00m, result.TotalAmount); // 25.50 * 2 + 10.00 * 3 = 51.00 + 30.00 = 81.00
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task RemoveItemAsync_WithValidData_RemovesItemAndRecalculatesTotal()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 20.00m);
        var addResult = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 3 });
        var itemId = addResult.Items[0].Id;

        var result = await _service.RemoveItemAsync(order.Id, itemId);

        Assert.Equal(0m, result.TotalAmount);
        Assert.Empty(result.Items);
    }

    [Fact]
    public async Task RemoveItemAsync_FromNonexistentOrder_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveItemAsync(999, 1));
    }

    [Fact]
    public async Task RemoveItemAsync_FromFinalizedOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        var addResult = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(order.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveItemAsync(order.Id, addResult.Items[0].Id));
    }

    [Fact]
    public async Task RemoveItemAsync_NonexistentItem_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RemoveItemAsync(order.Id, 999));
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WithValidData_UpdatesQuantityAndRecalculatesTotal()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 15.00m);
        var addResult = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 2 });
        var itemId = addResult.Items[0].Id;

        var result = await _service.UpdateItemQuantityAsync(
            new UpdateOrderItemRequest
            {
                OrderId = order.Id,
                OrderItemId = itemId,
                Quantity = 5
            });

        Assert.Equal(75.00m, result.TotalAmount); // 15.00 * 5
        Assert.Single(result.Items);
        Assert.Equal(5, result.Items[0].Quantity);
        Assert.Equal(75.00m, result.Items[0].Subtotal);
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WithZeroQuantity_ThrowsArgumentException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        var addResult = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateItemQuantityAsync(
                new UpdateOrderItemRequest
                {
                    OrderId = order.Id,
                    OrderItemId = addResult.Items[0].Id,
                    Quantity = 0
                }));
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_WithNegativeQuantity_ThrowsArgumentException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        var addResult = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateItemQuantityAsync(
                new UpdateOrderItemRequest
                {
                    OrderId = order.Id,
                    OrderItemId = addResult.Items[0].Id,
                    Quantity = -1
                }));
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ForNonexistentOrder_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateItemQuantityAsync(
                new UpdateOrderItemRequest
                {
                    OrderId = 999,
                    OrderItemId = 1,
                    Quantity = 2
                }));
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ForFinalizedOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        var addResult = await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(order.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateItemQuantityAsync(
                new UpdateOrderItemRequest
                {
                    OrderId = order.Id,
                    OrderItemId = addResult.Items[0].Id,
                    Quantity = 2
                }));
    }

    [Fact]
    public async Task UpdateItemQuantityAsync_ForNonexistentItem_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateItemQuantityAsync(
                new UpdateOrderItemRequest
                {
                    OrderId = order.Id,
                    OrderItemId = 999,
                    Quantity = 2
                }));
    }

    [Fact]
    public async Task CloseAsync_WithItems_FinalizesOrder()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 20.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 2 });
        await AdvanceOrderToReadyAsync(order.Id);

        var result = await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        Assert.Equal(OrderStatus.Finalizado, result.Status);
        Assert.Equal(40.00m, result.TotalAmount);
    }

    [Fact]
    public async Task CloseAsync_WithoutItems_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id }));
    }

    [Fact]
    public async Task CloseAsync_NonexistentOrder_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloseAsync(new CloseOrderRequest { OrderId = 999 }));
    }

    [Fact]
    public async Task CloseAsync_AlreadyFinalizedOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(order.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id }));
    }

    [Fact]
    public async Task CloseAsync_CanceledOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await _service.CancelAsync(new CancelOrderRequest { OrderId = order.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id }));
    }

    [Fact]
    public async Task CancelAsync_OpenOrder_CancelsOrder()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        var result = await _service.CancelAsync(new CancelOrderRequest { OrderId = order.Id });

        Assert.Equal(OrderStatus.Cancelado, result.Status);
    }

    [Fact]
    public async Task CancelAsync_NonexistentOrder_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelAsync(new CancelOrderRequest { OrderId = 999 }));
    }

    [Fact]
    public async Task CancelAsync_FinalizedOrder_CancelsAndRestoresStock()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(order.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        // Act - Cancel finalized order (should work now for stock restoration)
        var result = await _service.CancelAsync(new CancelOrderRequest { OrderId = order.Id });

        // Assert - Order cancelled successfully
        Assert.Equal(OrderStatus.Cancelado, result.Status);
    }

    [Fact]
    public async Task CancelAsync_AlreadyCanceledOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        await _service.CancelAsync(new CancelOrderRequest { OrderId = order.Id });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.CancelAsync(new CancelOrderRequest { OrderId = order.Id }));
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingOrders()
    {
        var openOrder = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync();
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = openOrder.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(openOrder.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = openOrder.Id });

        var openOrders = await _service.GetAllAsync(new OrderFilterRequest { Status = (int)OrderStatus.Pendente });
        var closedOrders = await _service.GetAllAsync(new OrderFilterRequest { Status = (int)OrderStatus.Finalizado });

        Assert.Empty(openOrders);
        Assert.Single(closedOrders);
    }

    [Fact]
    public async Task GetAllAsync_WithCustomerFilter_ReturnsMatchingOrders()
    {
        var customer1 = await CreateCustomerAsync();
        var customer2 = await CreateCustomerAsync();

        var order1 = await _service.CreateAsync(new CreateOrderRequest { CustomerId = customer1.Id });
        await _service.CreateAsync(new CreateOrderRequest { CustomerId = customer2.Id });

        var filtered = await _service.GetAllAsync(new OrderFilterRequest { CustomerId = customer1.Id });

        Assert.Single(filtered);
        Assert.Equal(order1.Id, filtered[0].Id);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingOrder_ReturnsOrder()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());

        var result = await _service.GetByIdAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentOrder_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithPercentageDiscount_AppliesDiscount()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 100.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 2 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "10% OFF",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // 2 items * 100 = 200, 10% discount = 20, total = 180
        Assert.Equal(180.00m, result.TotalAmount);
        Assert.Equal(90.00m, result.Items[0].UnitPrice); // 100 - 10% = 90
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithFixedAmountDiscount_AppliesDiscount()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 50.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 3 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "R$ 5 OFF",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // Fixed amount discount is applied to subtotal (150), then divided by quantity (3)
        // Discount per unit = 5/3 = 1.666..., new unit price = 50 - 1.666... = 48.333...
        // Total = 48.333... * 3 = 145
        Assert.Equal(145.00m, result.TotalAmount);
        Assert.Equal(48.33m, Math.Round(result.Items[0].UnitPrice, 2));
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithExpiredPromotion_DoesNotApply()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 100.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Expired Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 50,
            StartDate = DateTime.Today.AddDays(-30),
            EndDate = DateTime.Today.AddDays(-1), // Expired yesterday
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // No discount applied, total = 100
        Assert.Equal(100.00m, result.TotalAmount);
        Assert.Equal(100.00m, result.Items[0].UnitPrice);
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithFuturePromotion_DoesNotApply()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 100.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Future Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 50,
            StartDate = DateTime.Today.AddDays(1), // Starts tomorrow
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // No discount applied, total = 100
        Assert.Equal(100.00m, result.TotalAmount);
        Assert.Equal(100.00m, result.Items[0].UnitPrice);
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithMultiplePromotions_HighestPriorityWins()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 100.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        // Lower priority (lower number = lower priority with OrderByDescending)
        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Low Priority 5%",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 5,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        // Higher priority (higher number = higher priority with OrderByDescending)
        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "High Priority 20%",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 10,
            ApplicableProductIds = [product.Id]
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // High priority 20% wins: 100 * 0.20 = 20 discount, total = 80
        Assert.Equal(80.00m, result.TotalAmount);
        Assert.Equal(80.00m, result.Items[0].UnitPrice);
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithGlobalPromotion_AppliesToAllItems()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product1 = await CreateProductAsync(price: 50.00m);
        var product2 = await CreateProductAsync(price: 30.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product1.Id, Quantity = 1 });
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product2.Id, Quantity = 1 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Global 10%",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [], // Global
            ApplicableCategoryIds = []
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // Total 80, 10% = 8 discount, total = 72
        Assert.Equal(72.00m, result.TotalAmount);
        Assert.Equal(45.00m, result.Items[0].UnitPrice); // 50 - 10%
        Assert.Equal(27.00m, result.Items[1].UnitPrice); // 30 - 10%
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_WithCategoryPromotion_AppliesToCategoryProducts()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var category = await CreateCategoryAsync();
        var product1 = await CreateProductAsync(price: 50.00m, categoryId: category.Id);
        var product2 = await CreateProductAsync(price: 30.00m, categoryId: category.Id);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product1.Id, Quantity = 1 });
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product2.Id, Quantity = 1 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Category 20%",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 20,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [],
            ApplicableCategoryIds = [category.Id]
        });

        var result = await _service.RecalculateWithPromotionsAsync(order.Id);

        // Both products in category: 50 * 0.8 = 40, 30 * 0.8 = 24, total = 64
        Assert.Equal(64.00m, result.TotalAmount);
        Assert.Equal(40.00m, result.Items[0].UnitPrice);
        Assert.Equal(24.00m, result.Items[1].UnitPrice);
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_FinalizedOrder_ThrowsInvalidOperationException()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 100.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });
        await AdvanceOrderToReadyAsync(order.Id);
        await _service.CloseAsync(new CloseOrderRequest { OrderId = order.Id });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculateWithPromotionsAsync(order.Id));
    }

    [Fact]
    public async Task RecalculateWithPromotionsAsync_NonexistentOrder_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.RecalculateWithPromotionsAsync(999));
    }

    [Fact]
    public async Task GetAppliedPromotionsAsync_ReturnsAppliedPromotions()
    {
        var order = await _service.CreateAsync(new CreateOrderRequest());
        var product = await CreateProductAsync(price: 100.00m);
        await _service.AddItemAsync(new AddOrderItemRequest { OrderId = order.Id, ProductId = product.Id, Quantity = 1 });

        _promotionService.AddPromotion(new PromotionDto
        {
            Name = "Test Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1,
            ApplicableProductIds = [product.Id]
        });

        await _service.RecalculateWithPromotionsAsync(order.Id);
        var applied = await _service.GetAppliedPromotionsAsync(order.Id);

        Assert.Single(applied);
        Assert.Equal("Test Promo", applied[0].Name);
    }

    [Fact]
    public async Task GetAppliedPromotionsAsync_NonexistentOrder_ReturnsEmpty()
    {
        var applied = await _service.GetAppliedPromotionsAsync(999);
        Assert.Empty(applied);
    }

    private async Task<Category> CreateCategoryAsync()
    {
        var category = new Category
        {
            Name = "Categoria " + Guid.NewGuid().ToString("N")[..8],
            IsActive = true
        };
        await _categoryRepository.AddAsync(category);
        return category;
    }

    private async Task<CustomerDto> CreateCustomerAsync(bool isActive = true)
    {
        var customer = new Customer
        {
            Name = "Cliente " + Guid.NewGuid().ToString("N")[..8],
            IsActive = isActive,
            CreditLimit = 100m
        };
        await _customerRepository.AddAsync(customer);

        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            IsActive = customer.IsActive,
            CreditLimit = customer.CreditLimit
        };
    }

    private async Task<ProductDto> CreateProductAsync(decimal price = 20m, bool isActive = true, int? categoryId = null)
    {
        var product = new Product
        {
            Name = "Produto " + Guid.NewGuid().ToString("N")[..8],
            Price = price,
            IsActive = isActive,
            CategoryId = categoryId ?? 1
        };
        await _productRepository.AddAsync(product);

        return new ProductDto
        {
            Id = product.Id,
            Name = product.Name,
            Price = product.Price,
            IsActive = product.IsActive,
            CategoryId = product.CategoryId
        };
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Category>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Category> query = _categories;

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return Task.FromResult((IReadOnlyList<Category>)query.OrderBy(c => c.Name).ToList().AsReadOnly());
        }

        public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_categories.FirstOrDefault(c => c.Id == id));
        }

        public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_categories.Any(c => c.Id != excludeId && c.Name.Trim().ToUpperInvariant() == normalizedName));
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

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly List<Customer> _customers = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Customer>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer> query = _customers;

            if (isActive.HasValue)
            {
                query = query.Where(c => c.IsActive == isActive.Value);
            }

            return Task.FromResult((IReadOnlyList<Customer>)query.OrderBy(c => c.Name).ToList().AsReadOnly());
        }

        public Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_customers.FirstOrDefault(c => c.Id == id));
        }

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
        {
            return Task.FromResult(_customers.Any(c => c.Id == id));
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
                query = query.Where(p => p.IsActive == isActive.Value);
            }

            return Task.FromResult((IReadOnlyList<Product>)query.OrderBy(p => p.Name).ToList().AsReadOnly());
        }

        public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
        }

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

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(o =>
                    o.Id.ToString().Contains(term) ||
                    (o.Customer != null && o.Customer.Name.ToLower().Contains(term)) ||
                    o.Status.ToString().ToLower().Contains(term));
            }

            // Apply sorting
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

            var orders = query.ToList();
            foreach (var order in orders)
            {
                LoadNavigationProperties(order);
            }

            return Task.FromResult((IReadOnlyList<Order>)orders.AsReadOnly());
        }

        public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                LoadNavigationProperties(order);
            }
            return Task.FromResult(order);
        }

        public Task<Order?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            var order = _orders.FirstOrDefault(o => o.Id == id);
            if (order != null)
            {
                LoadNavigationProperties(order);
            }
            return Task.FromResult(order);
        }

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            order.Id = _nextId++;
            _orders.Add(order);
            LoadNavigationProperties(order);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Order order, CancellationToken cancellationToken = default)
        {
            var index = _orders.FindIndex(o => o.Id == order.Id);
            if (index >= 0)
            {
                _orders[index] = order;
                LoadNavigationProperties(order);
            }
            return Task.CompletedTask;
        }

        public Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
        {
            var query = _orders.Where(o => o.Status == OrderStatus.Pendente);

            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);

            var order = query.OrderByDescending(o => o.OrderDate).FirstOrDefault();
            if (order != null)
            {
                LoadNavigationProperties(order);
            }
            return Task.FromResult(order);
        }

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.Pendente));
        }

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

        private void LoadNavigationProperties(Order order)
        {
            if (order.CustomerId.HasValue)
            {
                order.Customer = _customerRepository.GetByIdAsync(order.CustomerId.Value).Result;
            }

            foreach (var item in order.OrderItems)
            {
                item.Product = _productRepository.GetByIdAsync(item.ProductId).Result;
            }
        }
    }

    private class FakePaymentService : IPaymentService
    {
        private readonly List<PaymentDto> _payments = [];
        private int _nextId = 1;

        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
        {
            var payment = new PaymentDto(
                _nextId++,
                request.OrderId,
                request.FormaPagamento,
                request.ValorPago,
                request.FormaPagamento == FormaPagamento.Dinheiro ? request.ValorPago - 10m : 0m,
                request.Parcelas,
                request.Parcelas > 0 ? request.ValorPago / request.Parcelas : 0m,
                DateTime.Now,
                request.Observacoes,
                false,
                null,
                null);
            _payments.Add(payment);
            return Task.FromResult(new ProcessPaymentResult(true, payment, null));
        }

        public Task<PaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PaymentDto?>(_payments.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<PaymentDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentDto>>(_payments.Where(p => p.OrderId == orderId).ToList());

        public Task<IReadOnlyList<PaymentDto>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentDto>>(_payments.ToList());

        public Task<ProcessPaymentResult> EstornarPaymentAsync(int paymentId, string motivo, CancellationToken cancellationToken = default)
            => Task.FromResult(new ProcessPaymentResult(true, null, "Not implemented"));
    }

    private class FakePendingPaymentService : IPendingPaymentService
    {
        private readonly List<PendingPaymentDto> _pendingPayments = [];
        private int _nextId = 1;

        public Task<RegisterPendingPaymentResult> RegisterPaymentAsync(RegisterPendingPaymentRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(new RegisterPendingPaymentResult(true, null, "Not implemented"));

        public Task<PendingPaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PendingPaymentDto?>(_pendingPayments.FirstOrDefault(p => p.Id == id));

        public Task<PendingPaymentDto?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<PendingPaymentDto?>(_pendingPayments.FirstOrDefault(p => p.OrderId == orderId));

        public Task<IReadOnlyList<PendingPaymentDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPaymentDto>>(_pendingPayments.Where(p => p.CustomerId == customerId).ToList());

        public Task<IReadOnlyList<PendingPaymentDto>> GetAllAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPaymentDto>>(_pendingPayments.ToList());

        public Task<IReadOnlyList<PendingPaymentDto>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPaymentDto>>(_pendingPayments.Where(p => p.StatusPagamento == status).ToList());

        public Task<IReadOnlyList<PendingPaymentHistoryDto>> GetHistoryAsync(int pendingPaymentId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PendingPaymentHistoryDto>>(Array.Empty<PendingPaymentHistoryDto>());

        public Task<PendingPaymentDto> CreateForOrderAsync(int orderId, int customerId, decimal valor, string? observacoes, CancellationToken cancellationToken = default)
        {
            var pending = new PendingPaymentDto(
                _nextId++,
                customerId,
                "Test Customer",
                orderId,
                valor,
                0m,
                valor,
                DateTime.Now,
                StatusPagamentoPendente.NaoPago,
                null,
                observacoes,
                Array.Empty<PendingPaymentHistoryDto>());
            _pendingPayments.Add(pending);
            return Task.FromResult(pending);
        }

        // Pending-orders specific
        public Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var saldo = _pendingPayments
                .Where(p => p.CustomerId == customerId && p.StatusPagamento == StatusPagamentoPendente.NaoPago)
                .Sum(p => p.SaldoPendente);
            return Task.FromResult(saldo);
        }

        public Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
        {
            var devedores = _pendingPayments
                .Where(p => p.StatusPagamento == StatusPagamentoPendente.NaoPago)
                .GroupBy(p => p.CustomerId)
                .Select(g => new DevedorDto(
                    g.Key,
                    "Test Customer",
                    "123456789",
                    g.Sum(p => p.SaldoPendente),
                    g.Count(),
                    g.Max(p => p.DataHora)))
                .OrderByDescending(d => d.SaldoDevedor)
                .ToList();
            return Task.FromResult<IReadOnlyList<DevedorDto>>(devedores);
        }
    }

    private sealed class FakeInventoryService : IInventoryService
    {
        private readonly List<InventoryItemDto> _items = [];
        private readonly List<InventoryMovementDto> _movements = [];
        private int _nextItemId = 1;
        private int _nextMovementId = 1;

        public Task<InventoryItemDto> CreateAsync(CreateInventoryItemRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
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
            // Track that it was called
            BaixaAutomaticaCalled = true;
            LastBaixaOrderId = orderId;
            LastOrderItems = orderItems;
            return Task.CompletedTask;
        }

        public IReadOnlyList<OrderItem>? LastOrderItems { get; private set; }

        public Task EstornarBaixaPedidoAsync(int orderId, CancellationToken cancellationToken = default)
        {
            // Track that it was called
            EstornoBaixaCalled = true;
            LastEstornoOrderId = orderId;
            return Task.CompletedTask;
        }

        public bool BaixaAutomaticaCalled { get; private set; }
        public int LastBaixaOrderId { get; private set; }
        public bool EstornoBaixaCalled { get; private set; }
        public int LastEstornoOrderId { get; private set; }
    }

    private sealed class FakePromotionService : IPromotionService
    {
        private readonly List<PromotionDto> _promotions = [];
        private int _nextId = 1;

        public void AddPromotion(PromotionDto promotion)
        {
            promotion.Id = _nextId++;
            _promotions.Add(promotion);
        }

        public Task<IReadOnlyList<PromotionDto>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<PromotionDto> query = _promotions;
            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<PromotionDto>>(query.OrderBy(p => p.Priority).ThenBy(p => p.Id).ToList());
        }

        public Task<PromotionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PromotionDto?>(_promotions.FirstOrDefault(p => p.Id == id));

        public Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PromotionDto> UpdateAsync(UpdatePromotionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
        {
            var promo = _promotions.FirstOrDefault(p => p.Id == id);
            if (promo != null) promo.IsActive = false;
            return Task.CompletedTask;
        }

        public Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
        {
            var promo = _promotions.FirstOrDefault(p => p.Id == id);
            if (promo != null) promo.IsActive = true;
            return Task.CompletedTask;
        }

        public Task<decimal> CalculateDiscountAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
        {
            var result = _promotions
                .Where(p => p.IsActive
                    && p.StartDate.Date <= date.Date
                    && (!p.EndDate.HasValue || p.EndDate.Value.Date >= date.Date))
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Id)
                .ToList();
            return Task.FromResult<IReadOnlyList<PromotionDto>>(result);
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
            _pendingPayments.Add(pendingPayment);
            return Task.FromResult(pendingPayment);
        }

        public Task UpdateAsync(PendingPayment pendingPayment, CancellationToken cancellationToken = default)
        {
            var index = _pendingPayments.FindIndex(p => p.Id == pendingPayment.Id);
            if (index >= 0)
                _pendingPayments[index] = pendingPayment;
            return Task.CompletedTask;
        }

        public Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
        {
            var saldo = _pendingPayments
                .Where(p => p.CustomerId == customerId && p.StatusPagamento == StatusPagamentoPendente.NaoPago)
                .Sum(p => p.ValorOriginal - p.ValorPago);
            return Task.FromResult(saldo);
        }

        public Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
        {
            var devedores = _pendingPayments
                .Where(p => p.StatusPagamento == StatusPagamentoPendente.NaoPago)
                .GroupBy(p => p.CustomerId)
                .Select(g => new DevedorDto(
                    g.Key,
                    "Test Customer",
                    "123456789",
                    g.Sum(p => p.ValorOriginal - p.ValorPago),
                    g.Count(),
                    g.Max(p => p.DataHora)))
                .OrderByDescending(d => d.SaldoDevedor)
                .ToList();
            return Task.FromResult<IReadOnlyList<DevedorDto>>(devedores);
        }
    }
}