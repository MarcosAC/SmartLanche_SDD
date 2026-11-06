using System.Collections.ObjectModel;
using System.Threading;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Payments;

public sealed class PaymentListViewModel : ListViewModelBase<PaymentDto, IPaymentService>
{
    private readonly IToastNotificationService _toastService;

    private FormaPagamento? _selectedFormaPagamento;
    private DateTime? _startDate;
    private DateTime? _endDate;
    private Timer? _searchDebounceTimer;

    public ObservableCollection<FormaPagamento> FormasPagamento { get; } = new(Enum.GetValues<FormaPagamento>());

    public ICommand RefreshCommand { get; }
    public ICommand ViewPaymentCommand { get; }
    public ICommand EstornarPaymentCommand { get; }

    public FormaPagamento? SelectedFormaPagamento
    {
        get => _selectedFormaPagamento;
        set => SetProperty(ref _selectedFormaPagamento, value);
    }

    public DateTime? StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public decimal TotalRecebido => Items.Sum(p => p.ValorPago);
    public string TotalRecebidoFormatted => $"R$ {TotalRecebido:N2}";
    public int TotalPaymentsCount => Items.Count;

    public PaymentListViewModel(
        IPaymentService paymentService,
        IToastNotificationService toastService)
        : base(paymentService)
    {
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        _searchDebounceTimer = new Timer(async _ => await LoadAsync(), null, Timeout.Infinite, Timeout.Infinite);

        RefreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);
        ViewPaymentCommand = new RelayCommand(async param => await ViewPaymentAsync(param), _ => !IsLoading);
        EstornarPaymentCommand = new RelayCommand(async param => await EstornarPaymentAsync(param), _ => !IsLoading);

        EndDate = DateTime.Today;
        StartDate = DateTime.Today.AddDays(-30);
    }

    protected override void OnSearchTextChanged()
    {
        _searchDebounceTimer?.Change(300, Timeout.Infinite);
    }

    protected override async Task<IReadOnlyList<PaymentDto>> GetItemsAsync()
    {
        var request = new PaymentFilterRequest
        {
            SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            FormaPagamento = SelectedFormaPagamento,
            StartDate = StartDate,
            EndDate = EndDate?.AddDays(1).AddTicks(-1) // End of day
        };

        return await Service.GetAllAsync(request);
    }

    protected override IReadOnlyList<PaymentDto> ApplySearchFilter(IReadOnlyList<PaymentDto> items, string searchTerm)
    {
        // Search is handled server-side via filter
        return items;
    }

    protected override IReadOnlyList<PaymentDto> ApplySort(IReadOnlyList<PaymentDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.OrderByDescending(p => p.DataHora).ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "id" => items.OrderBy(p => p.Id),
            "orderid" or "pedidoid" => items.OrderBy(p => p.OrderId),
            "formapagamento" or "forma" => items.OrderBy(p => p.FormaPagamento),
            "valorpago" or "valor" => items.OrderBy(p => p.ValorPago),
            "datahora" or "data" => items.OrderBy(p => p.DataHora),
            "estornado" => items.OrderBy(p => p.Estornado),
            _ => items.OrderByDescending(p => p.DataHora)
        };

        return SortDescending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
        OnPropertyChanged(nameof(TotalRecebidoFormatted));
        OnPropertyChanged(nameof(TotalPaymentsCount));
    }

    private async Task ViewPaymentAsync(object? parameter)
    {
        if (parameter is not PaymentDto payment) return;

        var message = $"Pagamento #{payment.Id}\n" +
                      $"Pedido: #{payment.OrderId}\n" +
                      $"Forma: {payment.FormaPagamento}\n" +
                      $"Valor: R$ {payment.ValorPago:N2}\n" +
                      $"Troco: R$ {payment.Troco:N2}\n" +
                      $"Parcelas: {payment.Parcelas}\n" +
                      $"Valor Parcela: R$ {payment.ValorParcela:N2}\n" +
                      $"Data: {payment.DataHora:dd/MM/yyyy HH:mm}\n" +
                      $"Observações: {payment.Observacoes ?? "Nenhuma"}\n" +
                      $"Estornado: {(payment.Estornado ? "Sim" : "Não")}" +
                      (payment.Estornado ? $"\nEstorno em: {payment.EstornoDataHora:dd/MM/yyyy HH:mm}\nMotivo: {payment.EstornoMotivo}" : "");

        System.Windows.MessageBox.Show(message, "Detalhes do Pagamento", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task EstornarPaymentAsync(object? parameter)
    {
        if (parameter is not PaymentDto payment) return;

        if (payment.Estornado)
        {
            _toastService.ShowWarning("Este pagamento já foi estornado.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Tem certeza que deseja estornar o pagamento #{payment.Id} (R$ {payment.ValorPago:N2})?\n\n" +
            "Esta ação não pode ser desfeita.",
            "Confirmar Estorno",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var estornoResult = await Service.EstornarPaymentAsync(payment.Id, "Estorno manual via interface");

            if (estornoResult.Success)
            {
                _toastService.ShowSuccess("Pagamento estornado com sucesso!");
                await LoadItemsAsync();
            }
            else
            {
                _toastService.ShowError(estornoResult.ErrorMessage ?? "Erro ao estornar pagamento.");
            }
        });
    }
}