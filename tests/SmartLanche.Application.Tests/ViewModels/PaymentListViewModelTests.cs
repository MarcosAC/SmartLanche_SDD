using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.ViewModels.Payments;
using Xunit;

namespace SmartLanche.Application.Tests.ViewModels;

public class PaymentListViewModelTests
{
    private readonly FakePaymentService _paymentService;
    private readonly FakeToastNotificationService _toastService;
    private readonly IServiceProvider _serviceProvider;
    private readonly PaymentListViewModel _viewModel;

    public PaymentListViewModelTests()
    {
        _paymentService = new FakePaymentService();
        _toastService = new FakeToastNotificationService();
        var services = new ServiceCollection();
        _serviceProvider = services.BuildServiceProvider();
        _viewModel = new PaymentListViewModel(_paymentService, _toastService);
    }

    [Fact]
    public async Task LoadAsync_LoadsPayments()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Teste", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Cartão", false, null, null));

        await _viewModel.LoadAsync();

        Assert.Equal(2, _viewModel.Items.Count);
    }

    [Fact]
    public async Task LoadAsync_WithFormaPagamentoFilter_ReturnsFiltered()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Teste", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Cartão", false, null, null));

        _viewModel.SelectedFormaPagamento = FormaPagamento.Dinheiro;
        await _viewModel.LoadAsync();

        Assert.Single(_viewModel.Items);
        Assert.Equal(FormaPagamento.Dinheiro, _viewModel.Items[0].FormaPagamento);
    }

    [Fact]
    public async Task LoadAsync_WithStartDateFilter_ReturnsFiltered()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-5), "Antigo", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Hoje", false, null, null));

        _viewModel.StartDate = DateTime.Today.AddDays(-1);
        await _viewModel.LoadAsync();

        Assert.Single(_viewModel.Items);
        Assert.Equal("Hoje", _viewModel.Items[0].Notes);
    }

    [Fact]
    public async Task LoadAsync_WithEndDateFilter_ReturnsFiltered()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-5), "Antigo", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Hoje", false, null, null));

        _viewModel.EndDate = DateTime.Today.AddDays(-2);
        await _viewModel.LoadAsync();

        Assert.Single(_viewModel.Items);
        Assert.Equal("Antigo", _viewModel.Items[0].Notes);
    }

    [Fact]
    public async Task SearchText_FiltersPaymentsByNotes()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Cliente João", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Cliente Maria", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            3, 102, FormaPagamento.Pix, 30m, 0m, 1, 30m,
            DateTime.Today, "Venda rápida", false, null, null));

        await _viewModel.LoadAsync();

        _viewModel.SearchText = "João";
        await Task.Delay(550); // Wait for debounce

        Assert.Single(_viewModel.Items);
        Assert.Equal("Cliente João", _viewModel.Items[0].Notes);
    }

    [Fact]
    public async Task SearchText_FiltersPaymentsByOrderId()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Teste", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Teste", false, null, null));

        await _viewModel.LoadAsync();

        _viewModel.SearchText = "101";
        await Task.Delay(550); // Wait for debounce

        Assert.Single(_viewModel.Items);
        Assert.Equal(101, _viewModel.Items[0].OrderId);
    }

    [Fact]
    public async Task TotalRecebido_CalculatesSumOfValorPago()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Teste", false, null, null));

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Teste", false, null, null));

        await _viewModel.LoadAsync();

        Assert.Equal(125m, _viewModel.TotalRecebido);
    }

    [Fact]
    public async Task SelectedItem_PropertyChanged_Notifies()
    {
        var payment = new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Teste", false, null, null);

        bool notified = false;
        _viewModel.PropertyChanged += (sender, e) =>
        {
            if (e.PropertyName == nameof(PaymentListViewModel.SelectedItem))
                notified = true;
        };

        _viewModel.SelectedItem = payment;

        Assert.True(notified);
        Assert.Equal(payment, _viewModel.SelectedItem);
    }

    [Fact]
    public async Task FormasPagamento_ContainsAllEnumValues()
    {
        Assert.Equal(4, _viewModel.FormasPagamento.Count);
        Assert.Contains(FormaPagamento.Dinheiro, _viewModel.FormasPagamento);
        Assert.Contains(FormaPagamento.Cartao, _viewModel.FormasPagamento);
        Assert.Contains(FormaPagamento.Pix, _viewModel.FormasPagamento);
        Assert.Contains(FormaPagamento.Fiado, _viewModel.FormasPagamento);
    }

    [Fact]
    public async Task IsLoading_FalseInitially()
    {
        Assert.False(_viewModel.IsLoading);
    }

    [Fact]
    public async Task RefreshCommand_ReloadsData()
    {
        _paymentService.AddPayment(new PaymentDto(
            1, 100, FormaPagamento.Dinheiro, 50m, 0m, 1, 50m,
            DateTime.Today.AddDays(-1), "Teste", false, null, null));

        await _viewModel.LoadAsync();
        Assert.Single(_viewModel.Items);

        _paymentService.AddPayment(new PaymentDto(
            2, 101, FormaPagamento.Cartao, 75m, 0m, 2, 37.5m,
            DateTime.Today, "Novo", false, null, null));

        _viewModel.RefreshCommand.Execute(null);
        await Task.Delay(100); // Allow async to complete

        Assert.Equal(2, _viewModel.Items.Count);
    }

    private sealed class FakePaymentService : IPaymentService
    {
        private readonly List<PaymentDto> _payments = [];
        private int _nextId = 1;

        public void AddPayment(PaymentDto payment)
        {
            var newPayment = payment with { Id = _nextId++ };
            _payments.Add(newPayment);
        }

        public Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PaymentDto?>(_payments.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<PaymentDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PaymentDto>>(_payments.Where(p => p.OrderId == orderId).ToList());

        public Task<IReadOnlyList<PaymentDto>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<PaymentDto> query = _payments;

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
                {
                    query = query.Where(p => p.FormaPagamento == filter.FormaPagamento.Value);
                }

                if (filter.StartDate.HasValue)
                {
                    query = query.Where(p => p.DataHora >= filter.StartDate.Value);
                }

                if (filter.EndDate.HasValue)
                {
                    query = query.Where(p => p.DataHora <= filter.EndDate.Value);
                }
            }

            return Task.FromResult<IReadOnlyList<PaymentDto>>(query.OrderByDescending(p => p.DataHora).ToList());
        }

        public Task<ProcessPaymentResult> EstornarPaymentAsync(int paymentId, string motivo, CancellationToken cancellationToken = default)
        {
            var payment = _payments.FirstOrDefault(p => p.Id == paymentId);
            if (payment != null)
            {
                // Since PaymentDto is a record, we can't modify it. We'll just return success.
                return Task.FromResult(new ProcessPaymentResult(true, payment, null));
            }
            return Task.FromResult(new ProcessPaymentResult(false, null, "Pagamento não encontrado."));
        }
    }

    private sealed class FakeToastNotificationService : SmartLanche.Presentation.Services.IToastNotificationService
    {
        public IReadOnlyList<SmartLanche.Presentation.Services.ToastItem> Toasts => [];
        public string? LastError { get; private set; }
        public string? LastSuccess { get; private set; }
        public string? LastWarning { get; private set; }
        public string? LastInfo { get; private set; }

        public void ShowError(string message) => LastError = message;
        public void ShowSuccess(string message) => LastSuccess = message;
        public void ShowWarning(string message) => LastWarning = message;
        public void ShowInfo(string message) => LastInfo = message;
    }
}