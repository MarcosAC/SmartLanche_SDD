using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Orders;

public sealed class OrderDetailViewModel : INotifyPropertyChanged
{
    private readonly IOrderService _orderService;

    private OrderDto? _order;
    private bool _isLoading;
    private string? _errorMessage;

    public event Action? RequestClose;
    public event PropertyChangedEventHandler? PropertyChanged;

    public OrderDetailViewModel(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));

        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    public ICommand CloseCommand { get; }

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
            }
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