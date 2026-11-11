using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;
using SmartLanche.Presentation.Views.Payments;

namespace SmartLanche.Presentation.ViewModels.Payments;

public sealed class PendingPaymentListViewModel : ListViewModelBase<PendingPaymentDto, IPendingPaymentService>
{
    private readonly ICustomerService _customerService;
    private readonly IToastNotificationService _toastService;
    private readonly IServiceProvider _services;

    private StatusPagamentoPendente? _selectedStatus;
    private int? _selectedCustomerId;

    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<StatusPagamentoPendente> StatusFilters { get; } = new()
    {
        StatusPagamentoPendente.NaoPago,
        StatusPagamentoPendente.Pago
    };

    public ICommand RegisterPaymentCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand OpenHistoryCommand { get; }

    public StatusPagamentoPendente? SelectedStatus
    {
        get => _selectedStatus;
        set => SetProperty(ref _selectedStatus, value);
    }

    public int? SelectedCustomerId
    {
        get => _selectedCustomerId;
        set => SetProperty(ref _selectedCustomerId, value);
    }

    public PendingPaymentListViewModel(
        IPendingPaymentService pendingPaymentService,
        ICustomerService customerService,
        IToastNotificationService toastService,
        IServiceProvider services)
        : base(pendingPaymentService)
    {
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        RegisterPaymentCommand = new RelayCommand(async param => await RegisterPaymentAsync(param), _ => !IsLoading);
        RefreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);
        OpenHistoryCommand = new RelayCommand(async param => await OpenHistoryAsync(param), _ => !IsLoading);
    }

    protected override async Task<IReadOnlyList<PendingPaymentDto>> GetItemsAsync()
    {
        await LoadCustomersAsync();

        var pendingPayments = await Service.GetAllAsync();
        return pendingPayments;
    }

    protected override IReadOnlyList<PendingPaymentDto> ApplySearchFilter(IReadOnlyList<PendingPaymentDto> items, string searchTerm)
    {
        var filtered = items.Where(p =>
            p.Id.ToString().Contains(searchTerm) ||
            p.CustomerName?.ToLowerInvariant().Contains(searchTerm) == true ||
            p.OrderId.ToString().Contains(searchTerm) ||
            p.ValorOriginal.ToString("N2").Contains(searchTerm) ||
            p.SaldoPendente.ToString("N2").Contains(searchTerm)
        );

        if (SelectedStatus.HasValue)
        {
            filtered = filtered.Where(p => p.StatusPagamento == SelectedStatus.Value);
        }

        if (SelectedCustomerId.HasValue && SelectedCustomerId.Value > 0)
        {
            filtered = filtered.Where(p => p.CustomerId == SelectedCustomerId.Value);
        }

        return filtered.ToList();
    }

    protected override IReadOnlyList<PendingPaymentDto> ApplySort(IReadOnlyList<PendingPaymentDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.OrderByDescending(p => p.DataHora).ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "id" => items.OrderBy(p => p.Id),
            "customername" or "clientenome" => items.OrderBy(p => p.CustomerName),
            "orderid" or "pedidoid" => items.OrderBy(p => p.OrderId),
            "valortotal" or "valororiginal" => items.OrderBy(p => p.ValorOriginal),
            "valorpago" => items.OrderBy(p => p.ValorPago),
            "saldopendente" or "saldo" => items.OrderBy(p => p.SaldoPendente),
            "datahora" or "data" => items.OrderBy(p => p.DataHora),
            "status" or "statuspagamento" => items.OrderBy(p => p.StatusPagamento),
            _ => items.OrderByDescending(p => p.DataHora)
        };

        return SortDescending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
    }

    private async Task LoadCustomersAsync()
    {
        try
        {
            var customers = await _customerService.GetAllAsync(true);

            Customers.Clear();
            Customers.Add(new CustomerDto { Id = 0, Name = "Todos os clientes" });

            foreach (var customer in customers)
            {
                Customers.Add(customer);
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao carregar clientes: {ex.Message}");
        }
    }

    private async Task RegisterPaymentAsync(object? parameter)
    {
        if (parameter is not PendingPaymentDto pendingPayment) return;

        if (pendingPayment.StatusPagamento == StatusPagamentoPendente.Pago)
        {
            _toastService.ShowWarning("Esta pendência já está quitada.");
            return;
        }

        var dialogViewModel = new RegisterPaymentDialogViewModel(pendingPayment, Service, _toastService);

        var dialog = new RegisterPaymentDialog
        {
            DataContext = dialogViewModel,
            Owner = System.Windows.Application.Current.MainWindow
        };

        dialogViewModel.RequestClose += (result) =>
        {
            dialog.DialogResult = result;
            dialog.Close();
        };

        var result = dialog.ShowDialog();

        if (result == true)
        {
            await LoadAsync();
        }
    }

    private async Task OpenHistoryAsync(object? parameter)
    {
        if (parameter is not PendingPaymentDto pendingPayment) return;

        var historyViewModel = _services.GetRequiredService<PendingPaymentHistoryViewModel>();
        await historyViewModel.InitializeAsync(pendingPayment.Id);

        var window = new Window
        {
            Title = $"Histórico - Pendência #{pendingPayment.Id}",
            Content = new PendingPaymentHistoryView { DataContext = historyViewModel },
            Width = 600,
            Height = 400,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = System.Windows.Application.Current.MainWindow
        };

        window.ShowDialog();
    }
}