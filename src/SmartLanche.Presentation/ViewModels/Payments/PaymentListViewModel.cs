using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using System.Windows.Input;

namespace SmartLanche.Presentation.ViewModels.Payments;

public sealed class PaymentListViewModel : ListViewModelBase<PaymentDto, IPaymentService>
{
    private readonly IToastNotificationService _toastService;

    private FormaPagamento? _selectedFormaPagamento;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    public ICommand RefreshCommand { get; }
    public ICommand ViewPaymentCommand { get; }
    public ICommand EstornarPaymentCommand { get; }

    public FormaPagamento? SelectedFormaPagamento
    {
        get => _selectedFormaPagamento;
        set
        {
            if (SetProperty(ref _selectedFormaPagamento, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public DateTime? DateFrom
    {
        get => _dateFrom;
        set
        {
            if (SetProperty(ref _dateFrom, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public DateTime? DateTo
    {
        get => _dateTo;
        set
        {
            if (SetProperty(ref _dateTo, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public decimal TotalRecebido => Items.Sum(p => p.AmountPaid);
    public string TotalRecebidoFormatted => $"R$ {TotalRecebido:N2}";
    public int TotalPaymentsCount => Items.Count;

    public PaymentListViewModel(
        IPaymentService paymentService,
        IToastNotificationService toastService)
        : base(paymentService)
    {
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        RefreshCommand = new RelayCommand(async _ => await LoadAsync(), _ => !IsLoading);
        ViewPaymentCommand = new RelayCommand(async param => await ViewPaymentAsync(param), _ => !IsLoading);
        EstornarPaymentCommand = new RelayCommand(async param => await EstornarPaymentAsync(param), _ => !IsLoading);
    }

    protected override async Task<IReadOnlyList<PaymentDto>> GetItemsAsync()
    {
        var request = new PaymentFilterRequest
        {
            SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            PaymentMethod = SelectedFormaPagamento.HasValue ? SelectedFormaPagamento.Value : null,
            StartDate = DateFrom,
            EndDate = DateTo?.AddDays(1).AddTicks(-1)
        };

        return await Service.GetAllAsync(request);
    }

    protected override IReadOnlyList<PaymentDto> ApplySearchFilter(IReadOnlyList<PaymentDto> items, string searchTerm)
    {
        return items.Where(o =>
            o.Id.ToString().Contains(searchTerm) ||
            o.OrderId.ToString().Contains(searchTerm) ||
            (o.Notes != null && o.Notes.ToLowerInvariant().Contains(searchTerm)) ||
            (o.CustomerName != null && o.CustomerName.ToLowerInvariant().Contains(searchTerm))
        ).ToList();
    }

    protected override IReadOnlyList<PaymentDto> ApplySort(IReadOnlyList<PaymentDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.OrderByDescending(p => p.DateTime).ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "id" => items.OrderBy(p => p.Id),
            "orderid" or "pedidoid" => items.OrderBy(p => p.OrderId),
            "formapagamento" or "forma" or "paymentmethod" => items.OrderBy(p => p.PaymentMethod),
            "valorpago" or "valor" or "amountpaid" => items.OrderBy(p => p.AmountPaid),
            "datahora" or "data" or "datetime" => items.OrderBy(p => p.DateTime),
            "estornado" or "isreversed" => items.OrderBy(p => p.IsReversed),
            _ => items.OrderByDescending(p => p.DateTime)
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
                      $"Forma: {payment.PaymentMethod}\n" +
                      $"Valor: R$ {payment.AmountPaid:N2}\n" +
                      $"Troco: R$ {payment.Change:N2}\n" +
                      $"Parcelas: {payment.Installments}\n" +
                      $"Valor Parcela: R$ {payment.InstallmentAmount:N2}\n" +
                      $"Data: {payment.DateTime:dd/MM/yyyy HH:mm}\n" +
                      $"Observações: {payment.Notes ?? "Nenhuma"}\n" +
                      $"Estornado: {(payment.IsReversed ? "Sim" : "Não")}" +
                      (payment.IsReversed ? $"\nEstorno em: {payment.ReversedAt:dd/MM/yyyy HH:mm}\nMotivo: {payment.ReversalReason}" : "");

        System.Windows.MessageBox.Show(message, "Detalhes do Pagamento", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
    }

    private async Task EstornarPaymentAsync(object? parameter)
    {
        if (parameter is not PaymentDto payment) return;

        if (payment.IsReversed)
        {
            _toastService.ShowWarning("Este pagamento já foi estornado.");
            return;
        }

        var result = System.Windows.MessageBox.Show(
            $"Tem certeza que deseja estornar o pagamento #{payment.Id} (R$ {payment.AmountPaid:N2})?\n\n" +
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