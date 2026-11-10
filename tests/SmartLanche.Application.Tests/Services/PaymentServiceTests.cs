using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class PaymentServiceTests
{
    private readonly FakeOrderRepository _orderRepository;
    private readonly FakePaymentRepository _paymentRepository;
    private readonly PaymentService _service;

    public PaymentServiceTests()
    {
        _orderRepository = new FakeOrderRepository();
        _paymentRepository = new FakePaymentRepository();

        _service = new PaymentService(_paymentRepository, _orderRepository);
    }

    #region ProcessPaymentAsync - Dinheiro

    [Fact]
    public async Task ProcessPaymentAsync_Dinheiro_WithValidTroco_ReturnsSuccess()
    {
        var order = await CreateOrderAsync(total: 50m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            100m,
            1,
            "Teste dinheiro"
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(50m, result.Payment!.Troco); // 100 - 50 = 50
        Assert.Equal(1, result.Payment.Parcelas);
        Assert.Equal(100m, result.Payment.ValorParcela); // 100 / 1 = 100
    }

    [Fact]
    public async Task ProcessPaymentAsync_Dinheiro_ExactAmount_ZeroTroco()
    {
        var order = await CreateOrderAsync(total: 50m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.True(result.Success);
        Assert.Equal(0m, result.Payment!.Troco);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Dinheiro_InsufficientAmount_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 100m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m, // Insuficiente
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("insuficiente", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Dinheiro_ZeroValorPago_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            0m,
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("maior que zero", result.ErrorMessage!);
    }

    #endregion

    #region ProcessPaymentAsync - Cartão

    [Fact]
    public async Task ProcessPaymentAsync_Cartao_ExactAmount_ReturnsSuccess()
    {
        var order = await CreateOrderAsync(total: 75m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            75m,
            3,
            "Cartão 3x"
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Cartao, result.Payment!.FormaPagamento);
        Assert.Equal(3, result.Payment.Parcelas);
        Assert.Equal(25m, result.Payment.ValorParcela);
        Assert.Equal(0m, result.Payment.Troco);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Cartao_WrongAmount_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 100m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            90m, // Deve ser igual ao total
            2,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("igual ao total", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Cartao_InvalidParcelas_Zero_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            50m,
            0,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("maior ou igual a 1", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Cartao_InvalidParcelas_Exceeds12_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Cartao,
            50m,
            13,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("não pode exceder 12", result.ErrorMessage!);
    }

    #endregion

    #region ProcessPaymentAsync - Pix

    [Fact]
    public async Task ProcessPaymentAsync_Pix_ExactAmount_ReturnsSuccess()
    {
        var order = await CreateOrderAsync(total: 30m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Pix,
            30m,
            1,
            "PIX"
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Pix, result.Payment!.FormaPagamento);
        Assert.Equal(1, result.Payment.Parcelas);
        Assert.Equal(30m, result.Payment.ValorParcela);
        Assert.Equal(0m, result.Payment.Troco);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Pix_WrongAmount_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Pix,
            45m, // Deve ser igual ao total
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("igual ao total", result.ErrorMessage!);
    }

    #endregion

    #region ProcessPaymentAsync - Fiado

    [Fact]
    public async Task ProcessPaymentAsync_Fiado_CreatesPendingPayment_ReturnsSuccess()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(total: 200m, customerId: customer.Id);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m, // Não usado no fiado, mas requerido pelo DTO
            1,
            "Fiado para cliente VIP"
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.Equal(FormaPagamento.Fiado, result.Payment!.FormaPagamento);
        Assert.Equal(0m, result.Payment.Troco);
    }

    [Fact]
    public async Task ProcessPaymentAsync_Fiado_WithoutCustomer_AllowedAtServiceLevel()
    {
        // PaymentService não valida cliente para Fiado - essa validação é no OrderService.CloseWithPaymentAsync
        var order = await CreateOrderAsync(total: 200m, customerId: null);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Fiado,
            200m,
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        // PaymentService apenas processa o pagamento, não valida regras de negócio de Fiado
        Assert.True(result.Success);
        Assert.Equal(FormaPagamento.Fiado, result.Payment!.FormaPagamento);
    }

    #endregion

    #region ProcessPaymentAsync - General Validations

    [Fact]
    public async Task ProcessPaymentAsync_NonexistentOrder_ReturnsFailure()
    {
        var request = new ProcessPaymentRequest(
            999,
            FormaPagamento.Dinheiro,
            50m,
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("não existe", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessPaymentAsync_FinalizedOrder_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);
        order.Status = OrderStatus.Finalizado;
        await _orderRepository.UpdateAsync(order);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Apenas pedidos em preparação ou prontos podem receber pagamento", result.ErrorMessage!);
    }

    [Fact]
    public async Task ProcessPaymentAsync_CancelledOrder_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);
        order.Status = OrderStatus.Cancelado;
        await _orderRepository.UpdateAsync(order);

        var request = new ProcessPaymentRequest(
            order.Id,
            FormaPagamento.Dinheiro,
            50m,
            1,
            null
        );

        var result = await _service.ProcessPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("Apenas pedidos em preparação ou prontos podem receber pagamento", result.ErrorMessage!);
    }

    #endregion

    #region EstornarPaymentAsync

    [Fact]
    public async Task EstornarPaymentAsync_ValidPayment_ReturnsSuccess()
    {
        var order = await CreateOrderAsync(total: 50m);
        var payment = await CreatePaymentAsync(order.Id, FormaPagamento.Dinheiro, 50m);

        var result = await _service.EstornarPaymentAsync(payment.Id, "Erro no caixa");

        Assert.True(result.Success);
        Assert.NotNull(result.Payment);
        Assert.True(result.Payment!.Estornado);
        Assert.Equal("Erro no caixa", result.Payment.EstornoMotivo);
        Assert.NotNull(result.Payment.EstornoDataHora);
    }

    [Fact]
    public async Task EstornarPaymentAsync_AlreadyEstornado_ReturnsFailure()
    {
        var order = await CreateOrderAsync(total: 50m);
        var payment = await CreatePaymentAsync(order.Id, FormaPagamento.Dinheiro, 50m);

        // Primeiro estorno
        await _service.EstornarPaymentAsync(payment.Id, "Primeiro estorno");

        // Segundo estorno
        var result = await _service.EstornarPaymentAsync(payment.Id, "Segundo estorno");

        Assert.False(result.Success);
        Assert.Contains("já foi estornado", result.ErrorMessage!);
    }

    [Fact]
    public async Task EstornarPaymentAsync_NonexistentPayment_ReturnsFailure()
    {
        var result = await _service.EstornarPaymentAsync(999, "Teste");

        Assert.False(result.Success);
        Assert.Contains("não encontrado", result.ErrorMessage!);
    }

    #endregion

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllPayments()
    {
        var order1 = await CreateOrderAsync(total: 50m);
        var order2 = await CreateOrderAsync(total: 75m);

        await CreatePaymentAsync(order1.Id, FormaPagamento.Dinheiro, 50m);
        await CreatePaymentAsync(order2.Id, FormaPagamento.Cartao, 75m);

        var payments = await _service.GetAllAsync();

        Assert.Equal(2, payments.Count);
    }

    [Fact]
    public async Task GetAllAsync_WithFormaPagamentoFilter_ReturnsFiltered()
    {
        var order1 = await CreateOrderAsync(total: 50m);
        var order2 = await CreateOrderAsync(total: 75m);

        await CreatePaymentAsync(order1.Id, FormaPagamento.Dinheiro, 50m);
        await CreatePaymentAsync(order2.Id, FormaPagamento.Cartao, 75m);

        var payments = await _service.GetAllAsync(new PaymentFilterRequest { FormaPagamento = FormaPagamento.Dinheiro });

        Assert.Single(payments);
        Assert.Equal(FormaPagamento.Dinheiro, payments[0].FormaPagamento);
    }

    [Fact]
    public async Task GetAllAsync_WithSearchTerm_FiltersByNotes()
    {
        var order1 = await CreateOrderAsync(total: 50m);
        var order2 = await CreateOrderAsync(total: 75m);

        await CreatePaymentAsync(order1.Id, FormaPagamento.Dinheiro, 50m, "Cliente João");
        await CreatePaymentAsync(order2.Id, FormaPagamento.Cartao, 75m, "Cliente Maria");

        var payments = await _service.GetAllAsync(new PaymentFilterRequest { SearchTerm = "João" });

        Assert.Single(payments);
        Assert.Equal("Cliente João", payments[0].Notes);
    }

    [Fact]
    public async Task GetAllAsync_WithDateRange_ReturnsFiltered()
    {
        var order1 = await CreateOrderAsync(total: 50m);
        var order2 = await CreateOrderAsync(total: 75m);

        await CreatePaymentAsync(order1.Id, FormaPagamento.Dinheiro, 50m);
        await CreatePaymentAsync(order2.Id, FormaPagamento.Cartao, 75m);

        // Filter by today (payments will have DataHora = DateTime.Now which is today)
        var payments = await _service.GetAllAsync(new PaymentFilterRequest
        {
            StartDate = DateTime.Today,
            EndDate = DateTime.Today.AddDays(1)
        });

        Assert.Equal(2, payments.Count);
    }

    #endregion

    #region GetByIdAsync / GetByOrderIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingPayment_ReturnsPayment()
    {
        var order = await CreateOrderAsync(total: 50m);
        var payment = await CreatePaymentAsync(order.Id, FormaPagamento.Dinheiro, 50m);

        var result = await _service.GetByIdAsync(payment.Id);

        Assert.NotNull(result);
        Assert.Equal(payment.Id, result!.Id);
        Assert.Equal(50m, result.ValorPago);
    }

    [Fact]
    public async Task GetByIdAsync_NonexistentPayment_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrderIdAsync_ReturnsPaymentsForOrder()
    {
        var order1 = await CreateOrderAsync(total: 50m);
        var order2 = await CreateOrderAsync(total: 75m);

        // Dinheiro pode pagar 50 (total 50) - troco 0
        await CreatePaymentAsync(order1.Id, FormaPagamento.Dinheiro, 50m);
        // Cartão precisa ser igual ao total - mas order1 já fechou? Não, PaymentService não fecha pedido
        // Vamos usar outro pedido para o segundo pagamento
        var order3 = await CreateOrderAsync(total: 25m);
        await CreatePaymentAsync(order3.Id, FormaPagamento.Pix, 25m);
        await CreatePaymentAsync(order2.Id, FormaPagamento.Cartao, 75m);

        var payments = await _service.GetByOrderIdAsync(order1.Id);

        Assert.Single(payments);
        Assert.Equal(order1.Id, payments[0].OrderId);
    }

    #endregion

    #region Helpers

    private async Task<Customer> CreateCustomerAsync(string name = "Test Customer", bool isActive = true)
    {
        var customer = new Customer
        {
            Name = name,
            Phone = "11999999999",
            IsActive = isActive
        };
        await _orderRepository.AddCustomerAsync(customer);
        return customer;
    }

    private async Task<Order> CreateOrderAsync(decimal total, int? customerId = null, DateTime? date = null)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.EmPreparacao,
            TotalAmount = total,
            OrderDate = date ?? DateTime.Now
        };
        await _orderRepository.AddAsync(order);
        return order;
    }

    private async Task<PaymentDto> CreatePaymentAsync(int orderId, FormaPagamento forma, decimal valorPago, string? obs = null, DateTime? date = null)
    {
        var request = new ProcessPaymentRequest(
            orderId,
            forma,
            valorPago,
            forma == FormaPagamento.Cartao ? 2 : 1,
            obs
        );
        var result = await _service.ProcessPaymentAsync(request);
        return result.Payment!;
    }

    #endregion

    #region Fakes

    private sealed class FakeOrderRepository : IOrderRepository
    {
        private readonly List<Order> _orders = [];
        private readonly List<Customer> _customers = [];
        private int _nextOrderId = 1;
        private int _nextCustomerId = 1;
        public Order? LastRetrievedOrder { get; private set; }

        public Task<IReadOnlyList<Order>> GetAllAsync(int? status = null, int? customerId = null, DateTime? dateFrom = null, DateTime? dateTo = null, string? searchTerm = null, string? sortBy = null, bool sortDescending = true, bool excludeCancelled = true, CancellationToken cancellationToken = default)
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
                query = query.Where(o => o.Id.ToString().Contains(term) ||
                    (o.Customer != null && o.Customer.Name.ToLower().Contains(term)) ||
                    o.Status.ToString().ToLower().Contains(term));
            }

            if (excludeCancelled)
                query = query.Where(o => o.Status != OrderStatus.Cancelado);

            return Task.FromResult<IReadOnlyList<Order>>(query.OrderByDescending(o => o.OrderDate).ToList());
        }

        public Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            LastRetrievedOrder = _orders.FirstOrDefault(o => o.Id == id);
            return Task.FromResult<Order?>(LastRetrievedOrder);
        }

        public Task<Order?> GetByIdTrackedAsync(int id, CancellationToken cancellationToken = default)
        {
            LastRetrievedOrder = _orders.FirstOrDefault(o => o.Id == id);
            return Task.FromResult<Order?>(LastRetrievedOrder);
        }

        public Task AddAsync(Order order, CancellationToken cancellationToken = default)
        {
            order.Id = _nextOrderId++;
            if (order.CustomerId.HasValue)
            {
                order.Customer = _customers.FirstOrDefault(c => c.Id == order.CustomerId.Value);
            }
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
        {
            var query = _orders.Where(o => o.Status == OrderStatus.EmPreparacao);
            if (customerId.HasValue)
                query = query.Where(o => o.CustomerId == customerId.Value);
            return Task.FromResult<Order?>(query.OrderByDescending(o => o.OrderDate).FirstOrDefault());
        }

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.EmPreparacao));

        public Task<IReadOnlyList<OrderItem>> GetItemsByOrderAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<OrderItem>>([]);

        public Task DeleteAsync(Order order, CancellationToken cancellationToken = default)
        {
            _orders.RemoveAll(o => o.Id == order.Id);
            return Task.CompletedTask;
        }

        public Task AddCustomerAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            customer.Id = _nextCustomerId++;
            _customers.Add(customer);
            return Task.CompletedTask;
        }

        public Task<Customer?> GetCustomerByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<Customer?>(_customers.FirstOrDefault(c => c.Id == id));
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
                        (p.Notes != null && p.Notes.ToLower().Contains(term)) ||
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

    #endregion
}