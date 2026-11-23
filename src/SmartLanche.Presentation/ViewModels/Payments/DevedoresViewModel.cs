using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Payments;

public sealed class DevedoresViewModel : INotifyPropertyChanged
{
    private readonly ICustomerService _customerService;
    private readonly IPendingPaymentService _pendingPaymentService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<DevedorDto> _devedores = [];
    private bool _isLoading;
    private decimal _totalDevedor;

    public DevedoresViewModel(
        ICustomerService customerService,
        IPendingPaymentService pendingPaymentService,
        IToastNotificationService toastService)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _pendingPaymentService = pendingPaymentService ?? throw new ArgumentNullException(nameof(pendingPaymentService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        LoadCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);
        RefreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);
    }

    public ObservableCollection<DevedorDto> Devedores { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public decimal TotalDevedor
    {
        get => _totalDevedor;
        private set => SetProperty(ref _totalDevedor, value);
    }

    public string TotalDevedorFormatted => $"R$ {TotalDevedor:N2}";

    public int TotalDevedoresCount => Devedores.Count;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;

            var devedores = await _customerService.GetDevedoresAsync();

            Devedores.Clear();
            foreach (var devedor in devedores)
            {
                Devedores.Add(devedor);
            }

            TotalDevedor = Devedores.Sum(d => d.SaldoDevedor);
            OnPropertyChanged(nameof(TotalDevedorFormatted));
            OnPropertyChanged(nameof(TotalDevedoresCount));
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao carregar relatório de inadimplência: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        return true;
    }
}