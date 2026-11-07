using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Orders;

public sealed class OrderDetailViewModel : INotifyPropertyChanged
{
    private readonly IOrderService _orderService;
    private readonly ICustomerService _customerService;
    private readonly IToastNotificationService _toastService;

    private OrderDto? _order;
    private bool _isLoading;
    private string? _errorMessage;

    private FormaPagamento? _selectedFormaPagamento;
    private decimal? _valorPago = 0m;
    private int? _parcelas;
    private string? _paymentObservacoes;
    private int? _selectedCustomerId;
    private ObservableCollection<CustomerDto> _customers = [];

    public event Action? RequestClose;
    public event PropertyChangedEventHandler? PropertyChanged;

    public OrderDetailViewModel(
        IOrderService orderService,
        ICustomerService customerService,
        IToastNotificationService toastService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
        CloseOrderCommand = new RelayCommand(async _ => await CloseOrderAsync(), _ => CanCloseOrder && !IsLoading);
        CancelOrderCommand = new RelayCommand(async _ => await CancelOrderAsync(), _ => CanCancelOrder && !IsLoading);
    }

    public ICommand CloseCommand { get; }
    public ICommand CloseOrderCommand { get; }
    public ICommand CancelOrderCommand { get; }

    public OrderDto? Order
    {
        get => _order;
        private set => SetProperty(ref _order, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    // Payment properties
    public FormaPagamento? SelectedFormaPagamento
    {
        get => _selectedFormaPagamento;
        set
        {
            if (SetProperty(ref _selectedFormaPagamento, value))
            {
                UpdatePaymentVisibility();
                UpdateTrocoAndParcela();
            }
        }
    }

    public decimal? ValorPago
    {
        get => _valorPago;
        set
        {
            if (SetProperty(ref _valorPago, value))
            {
                UpdateTrocoAndParcela();
            }
        }
    }

    public int? Parcelas
    {
        get => _parcelas;
        set
        {
            if (SetProperty(ref _parcelas, value))
            {
                UpdateTrocoAndParcela();
            }
        }
    }

    public string? PaymentObservacoes
    {
        get => _paymentObservacoes;
        set => SetProperty(ref _paymentObservacoes, value);
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    public ObservableCollection<CustomerDto> Customers => _customers;

    public FormaPagamento[] FormasPagamento => Enum.GetValues<FormaPagamento>();

    public bool ShowParcelas => SelectedFormaPagamento == FormaPagamento.Cartao;

    public bool ShowTroco => SelectedFormaPagamento == FormaPagamento.Dinheiro;

    public bool ShowCustomerSelection => SelectedFormaPagamento == FormaPagamento.Fiado;

    public decimal Troco
    {
        get
        {
            if (Order is null || !ValorPago.HasValue) return 0;
            return ValorPago.Value - Order.TotalAmount;
        }
    }

    public decimal ValorParcela
    {
        get
        {
            if (Order is null || !Parcelas.HasValue || Parcelas.Value <= 0) return 0;
            return Order.TotalAmount / Parcelas.Value;
        }
    }

    public bool CanCloseOrder => Order is not null
        && (Order.Status == OrderStatus.EmPreparacao || Order.Status == OrderStatus.Pronto)
        && !IsLoading
        && HasValidPaymentInfo();

    public bool CanCancelOrder => Order is not null
        && Order.Status != OrderStatus.Cancelado
        && Order.Status != OrderStatus.Finalizado
        && !IsLoading;

    public async Task InitializeAsync(int orderId)
    {
        try
        {
            IsLoading = true;
            ErrorMessage = null;

            Order = await _orderService.GetByIdAsync(orderId);

            if (Order is null)
            {
                ErrorMessage = "Pedido não encontrado.";
                return;
            }

            // Load customers for Fiado payment
            await LoadCustomersAsync();

            UpdateCommandStates();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers = await _customerService.GetAllAsync(true);
            _customers.Clear();
            foreach (var c in customers)
            {
                _customers.Add(c);
            }
        }
        catch
        {
            // Ignore errors loading customers
        }
    }

    private void UpdatePaymentVisibility()
    {
        OnPropertyChanged(nameof(ShowParcelas));
        OnPropertyChanged(nameof(ShowTroco));
        OnPropertyChanged(nameof(ShowCustomerSelection));
        OnPropertyChanged(nameof(CanCloseOrder));
    }

    private void UpdateTrocoAndParcela()
    {
        OnPropertyChanged(nameof(Troco));
        OnPropertyChanged(nameof(ValorParcela));
        OnPropertyChanged(nameof(CanCloseOrder));
    }

    private bool HasValidPaymentInfo()
    {
        if (!SelectedFormaPagamento.HasValue) return false;

        return SelectedFormaPagamento.Value switch
        {
            FormaPagamento.Dinheiro => ValorPago.HasValue && ValorPago.Value >= (Order?.TotalAmount ?? 0),
            FormaPagamento.Cartao => Parcelas.HasValue && Parcelas.Value >= 1 && Parcelas.Value <= 12,
            FormaPagamento.Fiado => SelectedCustomerId.HasValue,
            _ => false
        };
    }

    private async Task CloseOrderAsync()
    {
        if (Order is null || !CanCloseOrder || IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var request = new CloseOrderRequest
            {
                OrderId = Order.Id,
                FormaPagamento = SelectedFormaPagamento,
                ValorPago = ValorPago,
                Parcelas = Parcelas,
                Observacoes = PaymentObservacoes,
                CustomerId = SelectedCustomerId
            };

            Order = await _orderService.CloseAsync(request);

            UpdateCommandStates();

            _toastService.ShowSuccess("Pedido fechado com sucesso.");
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelOrderAsync()
    {
        if (Order is null || CanCancelOrder == false || IsLoading) return;

        var result = MessageBox.Show(
            "Tem certeza que deseja cancelar este pedido?",
            "Confirmar cancelamento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var request = new CancelOrderRequest { OrderId = Order.Id, DeleteIfPendente = false };
            Order = await _orderService.CancelAsync(request);

            UpdateCommandStates();

            _toastService.ShowSuccess("Pedido cancelado com sucesso.");
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            _toastService.ShowError(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(CanCloseOrder));
        OnPropertyChanged(nameof(CanCancelOrder));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}