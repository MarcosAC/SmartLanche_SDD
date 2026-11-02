using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class OrderServiceCloseWithPaymentTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeOrderRepository _orderRepository;
    private readonly FakeCustomerRepository _customerRepository;
    private readonly FakeProductRepository _productRepository;
    private readonly FakeCategoryRepository _categoryRepository;
    private readonly FakePromotionService _promotionService;
    private readonly FakePaymentRepository _paymentRepository;
    private readonly FakePendingPaymentRepository _pendingPaymentRepository;
    private readonly FakeInventoryService _inventoryService;
    private readonly OrderService _service;

    public OrderServiceCloseWithPaymentTests()
    {
        var services = new ServiceCollection();
        _orderRepository = new FakeOrderRepository();
        _customerRepository = new FakeCustomerRepository();
        _productRepository = new FakeProductRepository();
        _categoryRepository = new FakeCategoryRepository();
        _promotionService = new FakePromotionService();
        _paymentRepository = new FakePaymentRepository();
        _pendingPaymentRepository = new FakePendingPaymentRepository(_customerRepository);
        _inventoryService = new FakeInventoryService();

        services.AddSingleton<IOrderRepository>(_orderRepository);
        services.AddSingleton<ICustomerRepository>(_customerRepository);
        services.AddSingleton<IProductRepository>(_productRepository);
        services.AddSingleton<ICategoryRepository>(_categoryRepository);
        services.AddSingleton<IPromotionService>(_promotionService);
        _serviceProvider = services.BuildServiceProvider();

        var orderPromotionService = new OrderPromotionService(
            _orderRepository,
            _promotionService);

        var paymentService = new PaymentService(_paymentRepository, _orderRepository);
        var pendingPaymentService = new PendingPaymentService(_pendingPaymentRepository, _orderRepository, _customerRepository);
        var orderPaymentService = new OrderPaymentService(
            _orderRepository,
            _customerRepository,
            paymentService,
            pendingPaymentService,
            _inventoryService);

        _service = new OrderService(
            _orderRepository,
            _customerRepository,
            _productRepository,
            _inventoryService,
            orderPromotionService,
            orderPaymentService);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    #region CloseWithPaymentAsync - Validations

    [Fact]
    public async Task CloseWithPaymentAsync_NonexistentOrder_ReturnsFailure()
    {
        var request = new CloseOrderWithPaymentRequest(
            999,
            FormaPagamento.Dinheiro,
            50m,
            1,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("não existe", result.ErrorMessage!);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_ClosedOrder_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);
        order.Close();
        await _orderRepository.UpdateAsync(order);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("abertos", result.ErrorMessage!);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_CancelledOrder_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);
        order.Cancel();
        await _orderRepository.UpdateAsync(order);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("abertos", result.ErrorMessage!);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_OrderWithoutItems_ReturnsFailure()
    {
        var order = await CreateOrderAsync(); // Sem itens

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("sem itens", result.ErrorMessage!);
    }

    #endregion

    #region CloseWithPaymentAsync - Dinheiro

    [Fact]
    public async Task CloseWithPaymentAsync_Dinheiro_ExactAmount_ClosesOrderAndCreatesPayment()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            "Pagamento exato",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.True(result.Success);
        Assert.Equal(order.Id, result.OrderId);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Dinheiro, result.Payment!.FormaPagamento);
        Assert.Equal(50m, result.Payment.ValorPago);
        Assert.Equal(0m, result.Payment.Troco);
        Assert.Null(result.PendingPayment);

        // Verify order is closed
        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Fechado, updatedOrder!.Status);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Dinheiro_WithTroco_ClosesOrderAndCreatesPaymentWithTroco()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            100m,
            1,
            "Com troco",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(100m, result.Payment!.ValorPago);
        Assert.Equal(50m, result.Payment.Troco); // 100 - 50
        Assert.Null(result.PendingPayment);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Dinheiro_InsufficientAmount_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 100m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m, // Insuficiente
            1,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("insuficiente", result.ErrorMessage!);

        // Order should remain open
        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Aberto, updatedOrder!.Status);
    }

    #endregion

    #region CloseWithPaymentAsync - Cartão

    [Fact]
    public async Task CloseWithPaymentAsync_Cartao_ExactAmount_ClosesOrder()
    {
        var order = await CreateOrderWithItemsAsync(total: 75m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            75m,
            3,
            "Cartão 3x",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Cartao, result.Payment!.FormaPagamento);
        Assert.Equal(3, result.Payment.Parcelas);
        Assert.Equal(25m, result.Payment.ValorParcela);
        Assert.Equal(0m, result.Payment.Troco);
        Assert.Null(result.PendingPayment);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Cartao_WrongAmount_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 100m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            90m, // Deve ser igual ao total
            2,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("igual ao total", result.ErrorMessage!);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Cartao_InvalidParcelas_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            50m,
            0, // Inválido
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("maior ou igual a 1", result.ErrorMessage!);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Cartao_ParcelasExceeds12_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            50m,
            13, // Excede 12
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("não pode exceder 12", result.ErrorMessage!);
    }

    #endregion

    #region CloseWithPaymentAsync - Pix

    [Fact]
    public async Task CloseWithPaymentAsync_Pix_ExactAmount_ClosesOrder()
    {
        var order = await CreateOrderWithItemsAsync(total: 30m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Pix,
            30m,
            1,
            "PIX",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Pix, result.Payment!.FormaPagamento);
        Assert.Equal(1, result.Payment.Parcelas);
        Assert.Equal(30m, result.Payment.ValorParcela);
        Assert.Equal(0m, result.Payment.Troco);
        Assert.Null(result.PendingPayment);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Pix_WrongAmount_ReturnsFailure()
    {
        var order = await CreateOrderWithItemsAsync(total: 50m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Pix,
            45m, // Deve ser igual ao total
            1,
            "Teste",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("igual ao total", result.ErrorMessage!);
    }

    #endregion

    #region CloseWithPaymentAsync - Fiado

    [Fact]
    public async Task CloseWithPaymentAsync_Fiado_ValidCustomer_CreatesPendingPayment()
    {
        var customer = await CreateCustomerAsync(creditLimit: 1000m);
        var order = await CreateOrderWithItemsAsync(customer.Id, total: 200m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m, // Valor não usado para Fiado, mas requerido
            1,
            "Fiado para cliente VIP",
            customer.Id);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Fiado, result.Payment!.FormaPagamento);
        Assert.NotNull(result.PendingPayment);
        Assert.Equal(customer.Id, result.PendingPayment!.CustomerId);
        Assert.Equal(order.Id, result.PendingPayment.OrderId);
        Assert.Equal(200m, result.PendingPayment.ValorOriginal);
        Assert.Equal(0m, result.PendingPayment.ValorPago);
        Assert.Equal(200m, result.PendingPayment.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.NaoPago, result.PendingPayment.StatusPagamento);

        // Verify order is closed
        var updatedOrder = await _orderRepository.GetByIdAsync(order.Id);
        Assert.Equal(OrderStatus.Fechado, updatedOrder!.Status);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Fiado_WithoutCustomerId_AllowedAtServiceLevel()
    {
        // O OrderService.CloseWithPaymentAsync NÃO valida CustomerId para Fiado
        // Essa validação é feita no nível da UI/ViewModel
        var order = await CreateOrderWithItemsAsync(total: 200m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m,
            1,
            "Fiado sem cliente",
            null);

        var result = await _service.CloseWithPaymentAsync(request);

        // O PaymentService aceita Fiado sem validar cliente
        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Fiado, result.Payment!.FormaPagamento);
        // Mas não cria PendingPayment sem CustomerId
        Assert.Null(result.PendingPayment);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Fiado_CustomerInactive_ReturnsFailure()
    {
        var customer = await CreateCustomerAsync(creditLimit: 1000m, isActive: false);
        var order = await CreateOrderWithItemsAsync(customer.Id, total: 200m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m,
            1,
            "Fiado",
            customer.Id);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("inativo", result.ErrorMessage!);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Fiado_CreditLimitExceeded_ReturnsFailure()
    {
        var customer = await CreateCustomerAsync(creditLimit: 100m);
        var order = await CreateOrderWithItemsAsync(customer.Id, total: 200m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m,
            1,
            "Fiado",
            customer.Id);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Limite de crédito excedido", result.ErrorMessage!);
        Assert.Contains("100,00", result.ErrorMessage!); // Limite
        Assert.Contains("200,00", result.ErrorMessage!); // Novo pedido
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Fiado_CreditLimitNotExceeded_AllowsPayment()
    {
        var customer = await CreateCustomerAsync(creditLimit: 500m);
        var order = await CreateOrderWithItemsAsync(customer.Id, total: 200m);

        // Criar uma pendência anterior de 100
        var pendingPayment = await _pendingPaymentRepository.AddAsync(new PendingPayment
        {
            CustomerId = customer.Id,
            OrderId = 999, // Order inexistente, mas não importa para o teste
            ValorOriginal = 100m,
            ValorPago = 0m,
            DataHora = DateTime.Now,
            StatusPagamento = StatusPagamentoPendente.NaoPago
        });
        // Set customer navigation
        var cust = await _customerRepository.GetByIdAsync(customer.Id);
        pendingPayment.Customer = cust!;

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m,
            1,
            "Fiado",
            customer.Id);

        var result = await _service.CloseWithPaymentAsync(request);

        // Saldo devedor anterior: 100, Novo pedido: 200, Total: 300 <= Limite: 500
        Assert.True(result.Success);
        Assert.NotNull(result.PendingPayment);
    }

    [Fact]
    public async Task CloseWithPaymentAsync_Fiado_ZeroCreditLimit_Unlimited()
    {
        var customer = await CreateCustomerAsync(creditLimit: 0m); // Sem limite
        var order = await CreateOrderWithItemsAsync(customer.Id, total: 10000m);

        var request = new CloseOrderWithPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            10000m,
            1,
            "Fiado sem limite",
            customer.Id);

        var result = await _service.CloseWithPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.PendingPayment);
    }

    #endregion

    #region Helpers

    private async Task<Customer> CreateCustomerAsync(string name = "Test Customer", string phone = "11999999999", bool isActive = true, decimal creditLimit = 0m)
    {
        var customer = new Customer
        {
            Name = name,
            Phone = phone,
            IsActive = isActive,
            CreditLimit = creditLimit
        };
        await _customerRepository.AddAsync(customer);
        return customer;
    }

    private async Task<Order> CreateOrderAsync(int? customerId = null)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Aberto,
            TotalAmount = 0,
            OrderDate = DateTime.Now,
            Notes = string.Empty
        };
        await _orderRepository.AddAsync(order);
        return order;
    }

    private async Task<Order> CreateOrderWithItemsAsync(int? customerId = null, decimal total = 50m)
    {
        var order = await CreateOrderAsync(customerId);

        // Add a product
        var product = new Product
        {
            Name = "Test Product",
            Price = total,
            IsActive = true,
            CategoryId = 1
        };
        product.Id = 1;
        await _productRepository.AddAsync(product);

        var item = new OrderItem
        {
            OrderId = order.Id,
            ProductId = product.Id,
            Quantity = 1,
            UnitPrice = total
        };
        item.Validate();
        order.OrderItems.Add(item);
        order.RecalculateTotal();

        await _orderRepository.UpdateAsync(order);
        return order;
    }

    #endregion

    #region Fakes

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = [];
        private int _nextId = 1;

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

            return Task.FromResult<IReadOnlyList<Order>>(query.OrderByDescending(o => o.OrderDate).ToList());
        }

        public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
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

        public Task<Order?> GetOpenOrderAsync(int? customerId = null, CancellationToken cancellationToken = default)
            => Task.FromResult<Order?>(_orders.Where(o => o.Status == OrderStatus.Aberto && (!customerId.HasValue || o.CustomerId == customerId)).OrderByDescending(o => o.OrderDate).FirstOrDefault());

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.Aberto));

        public Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OrderItem>>([]);
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
            if (index >= 0)
                _customers[index] = customer;
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult(_customers.Any(c => c.Id == id));
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
            return Task.FromResult<IReadOnlyList<Product>>(query.ToList());
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
            if (index >= 0)
                _products[index] = product;
            return Task.CompletedTask;
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
        {
            IEnumerable<Payment> query = _payments;
            if (filter is not null)
            {
                if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
                {
                    var term = filter.SearchTerm.Trim().ToLower();
                    query = query.Where(p =>
                        (p.Observacoes != null && p.Observacoes.ToLower().Contains(term)) ||
                        p.OrderId.ToString().Contains(term));
                }
                if (filter.FormaPagamento.HasValue)
                    query = query.Where(p => p.FormaPagamento == filter.FormaPagamento.Value);
                if (filter.StartDate.HasValue)
                    query = query.Where(p => p.DataHora >= filter.StartDate.Value);
                if (filter.EndDate.HasValue)
                    query = query.Where(p => p.DataHora <= filter.EndDate.Value);
            }
            return Task.FromResult<IReadOnlyList<Payment>>(query.OrderByDescending(p => p.DataHora).ToList());
        }

        public Task<Payment> AddAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            payment.Id = _nextId++;
            _payments.Add(payment);
            return Task.FromResult(payment);
        }

        public Task UpdateAsync(Payment payment, CancellationToken cancellationToken = default)
        {
            var index = _payments.FindIndex(p => p.Id == payment.Id);
            if (index >= 0)
                _payments[index] = payment;
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
            if (customer != null)
                pendingPayment.Customer = customer;
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
            var sum = _pendingPayments
                .Where(p => p.CustomerId == customerId && p.StatusPagamento == StatusPagamentoPendente.NaoPago)
                .Sum(p => p.SaldoPendente);
            return Task.FromResult(sum);
        }

        public Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
        {
            var devedores = _pendingPayments
                .Where(p => p.StatusPagamento == StatusPagamentoPendente.NaoPago && p.Customer is not null && p.Customer.IsActive)
                .GroupBy(p => p.Customer!)
                .Select(g => new DevedorDto(
                    g.Key.Id,
                    g.Key.Name,
                    g.Key.Phone,
                    g.Sum(p => p.SaldoPendente),
                    g.Count(),
                    g.Max(p => p.DataHora)))
                .OrderByDescending(d => d.SaldoDevedor)
                .ToList();

            return Task.FromResult<IReadOnlyList<DevedorDto>>(devedores);
        }
    }

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
            if (index >= 0)
                _categories[index] = category;
            return Task.CompletedTask;
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

    #endregion
}