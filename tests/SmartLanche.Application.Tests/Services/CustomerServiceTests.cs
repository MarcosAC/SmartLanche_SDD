using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Tests.Services;

public class CustomerServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly FakeCustomerRepository _customerRepository;
    private readonly FakePendingPaymentRepository _pendingPaymentRepository;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        var services = new ServiceCollection();
        _customerRepository = new FakeCustomerRepository();
        _pendingPaymentRepository = new FakePendingPaymentRepository(_customerRepository);
        services.AddSingleton<ICustomerRepository>(_customerRepository);
        services.AddSingleton<IPendingPaymentRepository>(_pendingPaymentRepository);
        _serviceProvider = services.BuildServiceProvider();

        _service = new CustomerService(_serviceProvider.GetRequiredService<IServiceScopeFactory>(), _pendingPaymentRepository);
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCustomer()
    {
        var result = await _service.CreateAsync(
            new CreateCustomerRequest
            {
                Name = "João Silva",
                Phone = "11999999999",
                Notes = "Cliente VIP",
                CreditLimit = 500m
            });

        Assert.True(result.Id > 0);
        Assert.Equal("João Silva", result.Name);
        Assert.Equal("11999999999", result.Phone);
        Assert.Equal("Cliente VIP", result.Notes);
        Assert.Equal(500m, result.CreditLimit);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task CreateAsync_WithMinimalData_CreatesCustomer()
    {
        var result = await _service.CreateAsync(
            new CreateCustomerRequest
            {
                Name = "Maria Santos",
                CreditLimit = 0m
            });

        Assert.True(result.Id > 0);
        Assert.Equal("Maria Santos", result.Name);
        Assert.Null(result.Phone);
        Assert.Null(result.Notes);
        Assert.Equal(0m, result.CreditLimit);
        Assert.True(result.IsActive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateCustomerRequest
                {
                    Name = name,
                    CreditLimit = 100m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNameLongerThan150Characters_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateCustomerRequest
                {
                    Name = new string('A', 151),
                    CreditLimit = 100m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithPhoneLongerThan20Characters_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateCustomerRequest
                {
                    Name = "João Silva",
                    Phone = new string('1', 21),
                    CreditLimit = 100m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNotesLongerThan500Characters_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateCustomerRequest
                {
                    Name = "João Silva",
                    Notes = new string('A', 501),
                    CreditLimit = 100m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithNegativeCreditLimit_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateAsync(
                new CreateCustomerRequest
                {
                    Name = "João Silva",
                    CreditLimit = -1m
                }));
    }

    [Fact]
    public async Task CreateAsync_WithZeroCreditLimit_CreatesCustomer()
    {
        var result = await _service.CreateAsync(
            new CreateCustomerRequest
            {
                Name = "João Silva",
                CreditLimit = 0m
            });

        Assert.Equal(0m, result.CreditLimit);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyPhone_StoresNullPhone()
    {
        var result = await _service.CreateAsync(
            new CreateCustomerRequest
            {
                Name = "João Silva",
                Phone = "   ",
                CreditLimit = 100m
            });

        Assert.Null(result.Phone);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyNotes_StoresNullNotes()
    {
        var result = await _service.CreateAsync(
            new CreateCustomerRequest
            {
                Name = "João Silva",
                Notes = "   ",
                CreditLimit = 100m
            });

        Assert.Null(result.Notes);
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateName_AllowsBothCustomers()
    {
        var request = new CreateCustomerRequest
        {
            Name = "João Silva",
            CreditLimit = 100m
        };

        var first = await _service.CreateAsync(request);
        var second = await _service.CreateAsync(request);

        Assert.NotEqual(first.Id, second.Id);

        var customers = await _service.GetAllAsync();

        Assert.Equal(2, customers.Count);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCustomer()
    {
        var customer = await CreateCustomerAsync();

        var result = await _service.UpdateAsync(
            new UpdateCustomerRequest
            {
                Id = customer.Id,
                Name = "João Silva Atualizado",
                Phone = "11888888888",
                Notes = "Notas atualizadas",
                CreditLimit = 1000m,
                IsActive = true
            });

        Assert.Equal("João Silva Atualizado", result.Name);
        Assert.Equal("11888888888", result.Phone);
        Assert.Equal("Notas atualizadas", result.Notes);
        Assert.Equal(1000m, result.CreditLimit);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentCustomer_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateAsync(
                new UpdateCustomerRequest
                {
                    Id = 999,
                    Name = "João Silva",
                    CreditLimit = 100m
                }));
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyName_ThrowsArgumentException()
    {
        var customer = await CreateCustomerAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateAsync(
                new UpdateCustomerRequest
                {
                    Id = customer.Id,
                    Name = "   ",
                    CreditLimit = 100m
                }));
    }

    [Fact]
    public async Task UpdateAsync_WithNegativeCreditLimit_ThrowsArgumentException()
    {
        var customer = await CreateCustomerAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => _service.UpdateAsync(
                new UpdateCustomerRequest
                {
                    Id = customer.Id,
                    Name = "João Silva",
                    CreditLimit = -1m
                }));
    }

    [Fact]
    public async Task DeactivateAsync_ExistingCustomer_SetsInactive()
    {
        var customer = await CreateCustomerAsync();

        await _service.DeactivateAsync(customer.Id);

        var result = await _service.GetAllAsync(isActive: false);

        Assert.Single(result);
        Assert.False(result[0].IsActive);
    }

    [Fact]
    public async Task DeactivateAsync_NonexistentCustomer_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeactivateAsync(999));
    }

    [Fact]
    public async Task ReactivateAsync_InactiveCustomer_SetsActive()
    {
        var customer = await CreateCustomerAsync();
        await _service.DeactivateAsync(customer.Id);

        await _service.ReactivateAsync(customer.Id);

        var result = await _service.GetAllAsync(isActive: true);

        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_NonexistentCustomer_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReactivateAsync(999));
    }

    [Fact]
    public async Task GetAllAsync_WithStatusFilter_ReturnsMatchingCustomers()
    {
        var activeCustomer = await CreateCustomerAsync();
        var inactiveCustomer = await CreateCustomerAsync();

        await _service.DeactivateAsync(inactiveCustomer.Id);

        var activeCustomers = await _service.GetAllAsync(isActive: true);
        var inactiveCustomers = await _service.GetAllAsync(isActive: false);

        Assert.Single(activeCustomers);
        Assert.Equal(activeCustomer.Id, activeCustomers[0].Id);

        Assert.Single(inactiveCustomers);
        Assert.Equal(inactiveCustomer.Id, inactiveCustomers[0].Id);
    }

    [Fact]
    public async Task GetAllAsync_WithoutFilter_ReturnsAllCustomers()
    {
        await CreateCustomerAsync();
        var inactiveCustomer = await CreateCustomerAsync();
        await _service.DeactivateAsync(inactiveCustomer.Id);

        var allCustomers = await _service.GetAllAsync();

        Assert.Equal(2, allCustomers.Count);
    }

    [Fact]
    public async Task GetSaldoDevedorAsync_ExistingCustomer_ReturnsCorrectSum()
    {
        var customer = await CreateCustomerAsync();
        var pendingPayment1 = new PendingPayment
        {
            CustomerId = customer.Id,
            OrderId = 1,
            ValorOriginal = 100m,
            ValorPago = 30m,
            StatusPagamento = StatusPagamentoPendente.NaoPago,
            DataHora = DateTime.Now
        };
        var pendingPayment2 = new PendingPayment
        {
            CustomerId = customer.Id,
            OrderId = 2,
            ValorOriginal = 200m,
            ValorPago = 50m,
            StatusPagamento = StatusPagamentoPendente.NaoPago,
            DataHora = DateTime.Now.AddDays(-1)
        };
        await _pendingPaymentRepository.AddAsync(pendingPayment1);
        await _pendingPaymentRepository.AddAsync(pendingPayment2);

        var saldo = await _service.GetSaldoDevedorAsync(customer.Id);

        Assert.Equal(220m, saldo);
    }

    [Fact]
    public async Task GetDevedoresAsync_ReturnsDevedorDtos()
    {
        var customer = await CreateCustomerAsync();
        var pendingPayment = new PendingPayment
        {
            CustomerId = customer.Id,
            OrderId = 1,
            ValorOriginal = 100m,
            ValorPago = 0m,
            StatusPagamento = StatusPagamentoPendente.NaoPago,
            DataHora = DateTime.Now
        };
        await _pendingPaymentRepository.AddAsync(pendingPayment);

        var devedores = await _service.GetDevedoresAsync();

        Assert.Single(devedores);
        Assert.Equal(customer.Id, devedores[0].CustomerId);
        Assert.Equal(100m, devedores[0].SaldoDevedor);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private async Task<CustomerDto> CreateCustomerAsync()
    {
        return await _service.CreateAsync(
            new CreateCustomerRequest
            {
                Name = "João Silva",
                CreditLimit = 100m
            });
    }

    private sealed class FakeCustomerRepository : ICustomerRepository
    {
        private readonly List<Customer> _customers = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Customer>> GetAllAsync(
            bool? isActive = null,
            CancellationToken cancellationToken = default)
        {
            IEnumerable<Customer> query = _customers;

            if (isActive.HasValue)
            {
                query = query.Where(customer => customer.IsActive == isActive.Value);
            }

            IReadOnlyList<Customer> result = query.OrderBy(customer => customer.Name).ToList();

            return Task.FromResult(result);
        }

        public Task<Customer?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_customers.FirstOrDefault(customer => customer.Id == id));
        }

        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            customer.Id = _nextId++;
            _customers.Add(customer);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
        {
            var index = _customers.FindIndex(item => item.Id == customer.Id);

            if (index >= 0)
            {
                _customers[index] = customer;
            }

            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_customers.Any(customer => customer.Id == id));
        }
    }

    private sealed class FakePendingPaymentRepository : IPendingPaymentRepository
    {
        private readonly List<PendingPayment> _pendingPayments = [];
        private int _nextId = 1;
        private readonly FakeCustomerRepository _customerRepository;

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
            => Task.CompletedTask;

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