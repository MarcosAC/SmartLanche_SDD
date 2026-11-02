using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        // Show payment dialog
        var dialog = new RegisterPaymentDialog(pendingPayment, Service, _toastService);
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

// Dialog for registering payment on a pending payment
public sealed class RegisterPaymentDialog : Window
{
    private readonly PendingPaymentDto _pendingPayment;
    private readonly IPendingPaymentService _pendingPaymentService;
    private readonly IToastNotificationService _toastService;

    public RegisterPaymentDialog(PendingPaymentDto pendingPayment, IPendingPaymentService pendingPaymentService, IToastNotificationService toastService)
    {
        _pendingPayment = pendingPayment;
        _pendingPaymentService = pendingPaymentService;
        _toastService = toastService;

        Title = "Registrar Pagamento";
        Width = 400;
        Height = 350;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Owner = System.Windows.Application.Current.MainWindow;
        ResizeMode = ResizeMode.NoResize;

        InitializeUI();
    }

    private void InitializeUI()
    {
        var grid = new Grid { Margin = new Thickness(20) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var stackPanel = new StackPanel();

        // Header info
        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Cliente: {_pendingPayment.CustomerName}",
            FontWeight = FontWeights.Bold,
            FontSize = 14,
            Margin = new Thickness(0, 0, 0, 5)
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Pedido: #{_pendingPayment.OrderId}",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 5)
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Valor Original: R$ {_pendingPayment.ValorOriginal:N2}",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 5)
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Valor Pago: R$ {_pendingPayment.ValorPago:N2}",
            FontSize = 12,
            Margin = new Thickness(0, 0, 0, 5)
        });
        stackPanel.Children.Add(new TextBlock
        {
            Text = $"Saldo Pendente: R$ {_pendingPayment.SaldoPendente:N2}",
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = System.Windows.Media.Brushes.Red,
            Margin = new Thickness(0, 0, 0, 15)
        });

        // Valor do pagamento
        stackPanel.Children.Add(new TextBlock { Text = "Valor do Pagamento:", FontWeight = FontWeights.SemiBold });
        var txtValor = new TextBox
        {
            Name = "txtValor",
            Margin = new Thickness(0, 5, 0, 10),
            Text = _pendingPayment.SaldoPendente.ToString("N2")
        };
        stackPanel.Children.Add(txtValor);

        // Forma de pagamento
        stackPanel.Children.Add(new TextBlock { Text = "Forma de Pagamento:", FontWeight = FontWeights.SemiBold });
        var cmbForma = new ComboBox
        {
            Name = "cmbForma",
            Margin = new Thickness(0, 5, 0, 10),
            ItemsSource = Enum.GetValues(typeof(FormaPagamento)),
            SelectedItem = FormaPagamento.Dinheiro
        };
        stackPanel.Children.Add(cmbForma);

        // Observações
        stackPanel.Children.Add(new TextBlock { Text = "Observações:", FontWeight = FontWeights.SemiBold });
        var txtObs = new TextBox
        {
            Name = "txtObs",
            Margin = new Thickness(0, 5, 0, 15),
            Height = 60,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        stackPanel.Children.Add(txtObs);

        // Buttons
        var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var btnConfirm = new Button
        {
            Content = "Confirmar",
            Width = 100,
            Height = 35,
            Margin = new Thickness(0, 0, 10, 0),
            IsDefault = true
        };
        btnConfirm.Click += async (s, e) => await ConfirmClick(txtValor, cmbForma, txtObs);
        btnPanel.Children.Add(btnConfirm);

        var btnCancel = new Button
        {
            Content = "Cancelar",
            Width = 100,
            Height = 35,
            IsCancel = true
        };
        btnPanel.Children.Add(btnCancel);
        stackPanel.Children.Add(btnPanel);

        grid.Children.Add(stackPanel);
        Content = grid;
    }

    private async Task ConfirmClick(TextBox txtValor, ComboBox cmbForma, TextBox txtObs)
    {
        if (!decimal.TryParse(txtValor.Text, out var valor) || valor <= 0)
        {
            _toastService.ShowError("Informe um valor válido maior que zero.");
            return;
        }

        if (valor > _pendingPayment.SaldoPendente + 0.01m) // Small tolerance for rounding
        {
            _toastService.ShowError($"Valor excede o saldo pendente (R$ {_pendingPayment.SaldoPendente:N2}).");
            return;
        }

        var forma = (FormaPagamento)cmbForma.SelectedItem!;
        var observacoes = string.IsNullOrWhiteSpace(txtObs.Text) ? null : txtObs.Text;

        try
        {
            IsEnabled = false;

            var request = new RegisterPendingPaymentRequest(
                _pendingPayment.Id,
                valor,
                forma,
                observacoes);

            var result = await _pendingPaymentService.RegisterPaymentAsync(request);

            if (result.Success)
            {
                _toastService.ShowSuccess($"Pagamento de R$ {valor:N2} registrado com sucesso!");
                DialogResult = true;
                Close();
            }
            else
            {
                _toastService.ShowError(result.ErrorMessage ?? "Erro ao registrar pagamento.");
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro: {ex.Message}");
        }
        finally
        {
            IsEnabled = true;
        }
    }
}