using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class PendingPaymentServiceTests
{
    private readonly FakePendingPaymentRepository _pendingPaymentRepository;
    private readonly FakeOrderRepository _orderRepository;
    private readonly FakeCustomerRepository _customerRepository;
    private readonly PendingPaymentService _service;

    public PendingPaymentServiceTests()
    {
        _customerRepository = new FakeCustomerRepository();
        _orderRepository = new FakeOrderRepository();
        _pendingPaymentRepository = new FakePendingPaymentRepository(_customerRepository);

        _service = new PendingPaymentService(
            _pendingPaymentRepository,
            _orderRepository,
            _customerRepository);
    }

    #region RegisterPaymentAsync - Validations

    [Fact]
    public async Task RegisterPaymentAsync_NonexistentPendingPayment_ReturnsFailure()
    {
        var request = new RegisterPendingPaymentRequest(
            999,
            50m,
            FormaPagamento.Dinheiro,
            "Teste"
        );

        var result = await _service.RegisterPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("não encontrada", result.ErrorMessage!);
    }

    [Fact]
    public async Task RegisterPaymentAsync_AlreadyPaid_ReturnsFailure()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        // Quitar a pendência
        var payRequest = new RegisterPendingPaymentRequest(
            pending.Id,
            100m,
            FormaPagamento.Dinheiro,
            "Pagamento total"
        );
        await _service.RegisterPaymentAsync(payRequest);

        // Tentar pagar novamente
        var payRequest2 = new RegisterPendingPaymentRequest(
            pending.Id,
            50m,
            FormaPagamento.Dinheiro,
            "Pagamento extra"
        );
        var result = await _service.RegisterPaymentAsync(payRequest2);

        Assert.False(result.Success);
        Assert.Contains("já está quitada", result.ErrorMessage!);
    }

    [Fact]
    public async Task RegisterPaymentAsync_ZeroValor_ReturnsFailure()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var request = new RegisterPendingPaymentRequest(
            pending.Id,
            0m,
            FormaPagamento.Dinheiro,
            "Teste"
        );

        var result = await _service.RegisterPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("maior que zero", result.ErrorMessage!);
    }

    [Fact]
    public async Task RegisterPaymentAsync_ValorExceedsSaldo_ReturnsFailure()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var request = new RegisterPendingPaymentRequest(
            pending.Id,
            150m, // Excede o saldo de 100
            FormaPagamento.Dinheiro,
            "Teste"
        );

        var result = await _service.RegisterPaymentAsync(request);

        Assert.False(result.Success);
        Assert.Contains("excede saldo pendente", result.ErrorMessage!);
    }

    #endregion

    #region RegisterPaymentAsync - Partial Payment

    [Fact]
    public async Task RegisterPaymentAsync_PartialPayment_UpdatesSaldoAndStatus()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var request = new RegisterPendingPaymentRequest(
            pending.Id,
            40m,
            FormaPagamento.Dinheiro,
            "Pagamento parcial"
        );

        var result = await _service.RegisterPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.PendingPayment);
        Assert.Equal(40m, result.PendingPayment!.ValorPago);
        Assert.Equal(60m, result.PendingPayment.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.NaoPago, result.PendingPayment.StatusPagamento);
        Assert.Null(result.PendingPayment.DataPagamento);
    }

    [Fact]
    public async Task RegisterPaymentAsync_MultiplePartialPayments_AccumulatesCorrectly()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        // Primeiro pagamento: 30
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 30m, FormaPagamento.Dinheiro, "Parte 1"));

        // Segundo pagamento: 25
        var result2 = await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 25m, FormaPagamento.Cartao, "Parte 2"));

        Assert.True(result2.Success);
        Assert.Equal(55m, result2.PendingPayment!.ValorPago);
        Assert.Equal(45m, result2.PendingPayment.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.NaoPago, result2.PendingPayment.StatusPagamento);
    }

    #endregion

    #region RegisterPaymentAsync - Full Payment

    [Fact]
    public async Task RegisterPaymentAsync_FullPayment_SetsStatusToPago()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var request = new RegisterPendingPaymentRequest(
            pending.Id,
            100m,
            FormaPagamento.Pix,
            "Pagamento total"
        );

        var result = await _service.RegisterPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.PendingPayment);
        Assert.Equal(100m, result.PendingPayment!.ValorPago);
        Assert.Equal(0m, result.PendingPayment.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.Pago, result.PendingPayment.StatusPagamento);
        Assert.NotNull(result.PendingPayment.DataPagamento);
    }

    [Fact]
    public async Task RegisterPaymentAsync_FullPayment_RecordsHistory()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var request = new RegisterPendingPaymentRequest(
            pending.Id,
            100m,
            FormaPagamento.Cartao,
            "Parcelado"
        );

        var result = await _service.RegisterPaymentAsync(request);

        Assert.True(result.Success);
        Assert.NotNull(result.PendingPayment);
        Assert.Single(result.PendingPayment.History);
        Assert.Equal(100m, result.PendingPayment.History[0].ValorPago);
        Assert.Equal(FormaPagamento.Cartao, result.PendingPayment.History[0].FormaPagamento);
        Assert.Equal("Parcelado", result.PendingPayment.History[0].Observacoes);
    }

    [Fact]
    public async Task RegisterPaymentAsync_PartialThenFull_HistoryHasTwoEntries()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 40m, FormaPagamento.Dinheiro, "Parte 1"));
        var result = await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 60m, FormaPagamento.Pix, "Parte 2"));

        Assert.True(result.Success);
        Assert.Equal(2, result.PendingPayment!.History.Count);
        Assert.Equal(40m, result.PendingPayment.History[1].ValorPago); // Mais recente primeiro
        Assert.Equal(60m, result.PendingPayment.History[0].ValorPago);
    }

    #endregion

    #region CreateForOrderAsync

    [Fact]
    public async Task CreateForOrderAsync_ValidData_CreatesPendingPayment()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);

        var result = await _service.CreateForOrderAsync(order.Id, customer.Id, 150m, "Fiado VIP");

        Assert.NotNull(result);
        Assert.Equal(customer.Id, result.CustomerId);
        Assert.Equal(customer.Name, result.CustomerName);
        Assert.Equal(order.Id, result.OrderId);
        Assert.Equal(150m, result.ValorOriginal);
        Assert.Equal(0m, result.ValorPago);
        Assert.Equal(150m, result.SaldoPendente);
        Assert.Equal(StatusPagamentoPendente.NaoPago, result.StatusPagamento);
        Assert.Equal("Fiado VIP", result.Observacoes);
        Assert.Empty(result.History);
    }

    [Fact]
    public async Task CreateForOrderAsync_NonexistentOrder_ThrowsException()
    {
        var customer = await CreateCustomerAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateForOrderAsync(999, customer.Id, 100m, "Teste"));

        Assert.Contains("não existe", ex.Message);
    }

    [Fact]
    public async Task CreateForOrderAsync_NonexistentCustomer_ThrowsException()
    {
        var order = await CreateOrderAsync(1);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateForOrderAsync(order.Id, 999, 100m, "Teste"));

        Assert.Contains("não existe", ex.Message);
    }

    [Fact]
    public async Task CreateForOrderAsync_DuplicateOrder_ThrowsException()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);

        await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Primeira");

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.CreateForOrderAsync(order.Id, customer.Id, 50m, "Segunda"));

        Assert.Contains("Já existe uma pendência", ex.Message);
    }

    #endregion

    #region GetByIdAsync / GetByOrderIdAsync / GetByCustomerIdAsync

    [Fact]
    public async Task GetByIdAsync_ExistingPendingPayment_ReturnsDto()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var result = await _service.GetByIdAsync(pending.Id);

        Assert.NotNull(result);
        Assert.Equal(pending.Id, result!.Id);
        Assert.Equal(100m, result.ValorOriginal);
    }

    [Fact]
    public async Task GetByIdAsync_Nonexistent_ReturnsNull()
    {
        var result = await _service.GetByIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByOrderIdAsync_Existing_ReturnsPendingPayment()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        var result = await _service.GetByOrderIdAsync(order.Id);

        Assert.NotNull(result);
        Assert.Equal(pending.Id, result!.Id);
    }

    [Fact]
    public async Task GetByOrderIdAsync_Nonexistent_ReturnsNull()
    {
        var result = await _service.GetByOrderIdAsync(999);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsAllForCustomer()
    {
        var customer1 = await CreateCustomerAsync("João");
        var customer2 = await CreateCustomerAsync("Maria");

        var order1 = await CreateOrderAsync(customer1.Id);
        var order2 = await CreateOrderAsync(customer1.Id);
        var order3 = await CreateOrderAsync(customer2.Id);

        await _service.CreateForOrderAsync(order1.Id, customer1.Id, 50m, "A");
        await _service.CreateForOrderAsync(order2.Id, customer1.Id, 75m, "B");
        await _service.CreateForOrderAsync(order3.Id, customer2.Id, 100m, "C");

        var result = await _service.GetByCustomerIdAsync(customer1.Id);

        Assert.Equal(2, result.Count);
        Assert.All(result, p => Assert.Equal(customer1.Id, p.CustomerId));
    }

    #endregion

    #region GetAllAsync / GetByStatusAsync

    [Fact]
    public async Task GetAllAsync_ReturnsAllPendingPayments()
    {
        var customer = await CreateCustomerAsync();
        var order1 = await CreateOrderAsync(customer.Id);
        var order2 = await CreateOrderAsync(customer.Id);

        await _service.CreateForOrderAsync(order1.Id, customer.Id, 50m, "A");
        await _service.CreateForOrderAsync(order2.Id, customer.Id, 75m, "B");

        var result = await _service.GetAllAsync();

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetByStatusAsync_NaoPago_ReturnsOnlyOpen()
    {
        var customer = await CreateCustomerAsync();
        var order1 = await CreateOrderAsync(customer.Id);
        var order2 = await CreateOrderAsync(customer.Id);

        var pending1 = await _service.CreateForOrderAsync(order1.Id, customer.Id, 50m, "A");
        var pending2 = await _service.CreateForOrderAsync(order2.Id, customer.Id, 75m, "B");

        // Quitar a primeira
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending1.Id, 50m, FormaPagamento.Dinheiro, "Quitado"));

        var result = await _service.GetByStatusAsync(StatusPagamentoPendente.NaoPago);

        Assert.Single(result);
        Assert.Equal(pending2.Id, result[0].Id);
    }

    [Fact]
    public async Task GetByStatusAsync_Pago_ReturnsOnlyPaid()
    {
        var customer = await CreateCustomerAsync();
        var order1 = await CreateOrderAsync(customer.Id);
        var order2 = await CreateOrderAsync(customer.Id);

        var pending1 = await _service.CreateForOrderAsync(order1.Id, customer.Id, 50m, "A");
        await _service.CreateForOrderAsync(order2.Id, customer.Id, 75m, "B");

        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending1.Id, 50m, FormaPagamento.Dinheiro, "Quitado"));

        var result = await _service.GetByStatusAsync(StatusPagamentoPendente.Pago);

        Assert.Single(result);
        Assert.Equal(pending1.Id, result[0].Id);
    }

    #endregion

    #region GetHistoryAsync

    [Fact]
    public async Task GetHistoryAsync_ReturnsHistoryOrderedByDateDesc()
    {
        var customer = await CreateCustomerAsync();
        var order = await CreateOrderAsync(customer.Id);
        var pending = await _service.CreateForOrderAsync(order.Id, customer.Id, 100m, "Teste");

        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 30m, FormaPagamento.Dinheiro, "Primeiro"));
        await Task.Delay(10); // Ensure different timestamps
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 40m, FormaPagamento.Cartao, "Segundo"));
        await Task.Delay(10);
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending.Id, 30m, FormaPagamento.Pix, "Terceiro"));

        var history = await _service.GetHistoryAsync(pending.Id);

        Assert.Equal(3, history.Count);
        // Mais recente primeiro
        Assert.Equal(30m, history[0].ValorPago);
        Assert.Equal(FormaPagamento.Pix, history[0].FormaPagamento);
        Assert.Equal(40m, history[1].ValorPago);
        Assert.Equal(30m, history[2].ValorPago);
    }

    [Fact]
    public async Task GetHistoryAsync_NonexistentPendingPayment_ReturnsEmpty()
    {
        var history = await _service.GetHistoryAsync(999);

        Assert.Empty(history);
    }

    #endregion

    #region GetSaldoDevedorAsync

    [Fact]
    public async Task GetSaldoDevedorAsync_CalculatesCorrectly()
    {
        var customer = await CreateCustomerAsync();
        var order1 = await CreateOrderAsync(customer.Id);
        var order2 = await CreateOrderAsync(customer.Id);

        var pending1 = await _service.CreateForOrderAsync(order1.Id, customer.Id, 100m, "A");
        var pending2 = await _service.CreateForOrderAsync(order2.Id, customer.Id, 200m, "B");

        // Pagar 30 da primeira
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending1.Id, 30m, FormaPagamento.Dinheiro, "Parte"));

        var saldo = await _service.GetSaldoDevedorAsync(customer.Id);

        // Saldo: (100-30) + 200 = 270
        Assert.Equal(270m, saldo);
    }

    [Fact]
    public async Task GetSaldoDevedorAsync_IgnoresPaidPendingPayments()
    {
        var customer = await CreateCustomerAsync();
        var order1 = await CreateOrderAsync(customer.Id);
        var order2 = await CreateOrderAsync(customer.Id);

        var pending1 = await _service.CreateForOrderAsync(order1.Id, customer.Id, 100m, "A");
        var pending2 = await _service.CreateForOrderAsync(order2.Id, customer.Id, 200m, "B");

        // Quitar a primeira completamente
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending1.Id, 100m, FormaPagamento.Dinheiro, "Total"));

        var saldo = await _service.GetSaldoDevedorAsync(customer.Id);

        // Apenas a segunda pendência conta: 200
        Assert.Equal(200m, saldo);
    }

    [Fact]
    public async Task GetSaldoDevedorAsync_NonexistentCustomer_ReturnsZero()
    {
        var saldo = await _service.GetSaldoDevedorAsync(999);

        Assert.Equal(0m, saldo);
    }

    #endregion

    #region GetDevedoresAsync

    [Fact]
    public async Task GetDevedoresAsync_ReturnsDebtorsOrderedByBalanceDesc()
    {
        var customer1 = await CreateCustomerAsync("João", "11999991111");
        var customer2 = await CreateCustomerAsync("Maria", "11999992222");
        var customer3 = await CreateCustomerAsync("Pedro", "11999993333");

        var order1 = await CreateOrderAsync(customer1.Id);
        var order2 = await CreateOrderAsync(customer2.Id);
        var order3 = await CreateOrderAsync(customer3.Id);

        // João: 500 devedor
        await _service.CreateForOrderAsync(order1.Id, customer1.Id, 500m, "A");

        // Maria: 200 devedor (300 - 100 pago)
        var pending2 = await _service.CreateForOrderAsync(order2.Id, customer2.Id, 300m, "B");
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending2.Id, 100m, FormaPagamento.Dinheiro, "Parte"));

        // Pedro: 0 devedor (quitado)
        var pending3 = await _service.CreateForOrderAsync(order3.Id, customer3.Id, 100m, "C");
        await _service.RegisterPaymentAsync(new RegisterPendingPaymentRequest(
            pending3.Id, 100m, FormaPagamento.Pix, "Total"));

        var devedores = await _service.GetDevedoresAsync();

        Assert.Equal(2, devedores.Count); // Pedro não aparece (saldo 0)
        Assert.Equal(customer1.Id, devedores[0].CustomerId); // João tem maior saldo
        Assert.Equal(500m, devedores[0].SaldoDevedor);
        Assert.Equal(1, devedores[0].QuantidadePendencias);
        Assert.Equal(customer2.Id, devedores[1].CustomerId);
        Assert.Equal(200m, devedores[1].SaldoDevedor);
    }

    [Fact]
    public async Task GetDevedoresAsync_ExcludesInactiveCustomers()
    {
        var customer1 = await CreateCustomerAsync("João", "11999991111");
        var customer2 = await CreateCustomerAsync("Maria", "11999992222", false); // Inativo

        var order1 = await CreateOrderAsync(customer1.Id);
        var order2 = await CreateOrderAsync(customer2.Id);

        await _service.CreateForOrderAsync(order1.Id, customer1.Id, 100m, "A");
        await _service.CreateForOrderAsync(order2.Id, customer2.Id, 200m, "B");

        var devedores = await _service.GetDevedoresAsync();

        Assert.Single(devedores);
        Assert.Equal(customer1.Id, devedores[0].CustomerId);
    }

    [Fact]
    public async Task GetDevedoresAsync_CustomerWithMultiplePendingPayments_SumsCorrectly()
    {
        var customer = await CreateCustomerAsync("João", "11999991111");

        var order1 = await CreateOrderAsync(customer.Id);
        var order2 = await CreateOrderAsync(customer.Id);
        var order3 = await CreateOrderAsync(customer.Id);

        await _service.CreateForOrderAsync(order1.Id, customer.Id, 100m, "A");
        await _service.CreateForOrderAsync(order2.Id, customer.Id, 200m, "B");
        await _service.CreateForOrderAsync(order3.Id, customer.Id, 50m, "C");

        var devedores = await _service.GetDevedoresAsync();

        Assert.Single(devedores);
        Assert.Equal(350m, devedores[0].SaldoDevedor);
        Assert.Equal(3, devedores[0].QuantidadePendencias);
    }

    #endregion

    #region Helpers

    private async Task<Customer> CreateCustomerAsync(string name = "Test Customer", string phone = "11999999999", bool isActive = true)
    {
        var customer = new Customer
        {
            Name = name,
            Phone = phone,
            IsActive = isActive
        };
        await _customerRepository.AddAsync(customer);
        return customer;
    }

    private async Task<Order> CreateOrderAsync(int customerId, decimal total = 100m)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Status = OrderStatus.Pendente,
            TotalAmount = total,
            OrderDate = DateTime.Now
        };
        await _orderRepository.AddAsync(order);
        return order;
    }

    #endregion

    #region Fakes

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
            // Set navigation property for Customer
            var customer = _customerRepository.GetByIdAsync(pendingPayment.CustomerId, cancellationToken).Result;
            if (customer != null)
            {
                pendingPayment.Customer = customer;
            }
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
            => Task.FromResult<Order?>(_orders.Where(o => o.Status == OrderStatus.Pendente && (!customerId.HasValue || o.CustomerId == customerId)).OrderByDescending(o => o.OrderDate).FirstOrDefault());

        public Task<bool> ExistsOpenOrderForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
            => Task.FromResult(_orders.Any(o => o.CustomerId == customerId && o.Status == OrderStatus.Pendente));

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

    #endregion
}