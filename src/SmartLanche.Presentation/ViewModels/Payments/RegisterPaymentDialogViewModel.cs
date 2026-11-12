using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Payments;

public sealed class RegisterPaymentDialogViewModel : INotifyPropertyChanged
{
    private readonly PendingPaymentDto _pendingPayment;
    private readonly IPendingPaymentService _pendingPaymentService;
    private readonly IToastNotificationService _toastService;

    private decimal? _valorPagamento;
    private FormaPagamento _selectedFormaPagamento = FormaPagamento.Dinheiro;
    private string? _observacoes;
    private string? _validationError;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action<bool?>? RequestClose;

    public RegisterPaymentDialogViewModel(
        PendingPaymentDto pendingPayment,
        IPendingPaymentService pendingPaymentService,
        IToastNotificationService toastService)
    {
        _pendingPayment = pendingPayment ?? throw new ArgumentNullException(nameof(pendingPayment));
        _pendingPaymentService = pendingPaymentService ?? throw new ArgumentNullException(nameof(pendingPaymentService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        ValorPagamento = _pendingPayment.SaldoPendente;

        ConfirmCommand = new AsyncRelayCommand(_ => ConfirmAsync(), _ => CanConfirm);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke(false));
    }

    public PendingPaymentDto PendingPayment => _pendingPayment;

    public FormaPagamento[] FormasPagamento => Enum.GetValues<FormaPagamento>();

    public decimal? ValorPagamento
    {
        get => _valorPagamento;
        set
        {
            if (SetProperty(ref _valorPagamento, value))
            {
                Validate();
                OnPropertyChanged(nameof(CanConfirm));
            }
        }
    }

    public FormaPagamento SelectedFormaPagamento
    {
        get => _selectedFormaPagamento;
        set
        {
            if (SetProperty(ref _selectedFormaPagamento, value))
            {
                Validate();
                OnPropertyChanged(nameof(CanConfirm));
            }
        }
    }

    public string? Observacoes
    {
        get => _observacoes;
        set => SetProperty(ref _observacoes, value);
    }

    public string? ValidationError
    {
        get => _validationError;
        private set => SetProperty(ref _validationError, value);
    }

    public bool CanConfirm => !string.IsNullOrWhiteSpace(ValidationError);

    public ICommand ConfirmCommand { get; }
    public ICommand CancelCommand { get; }

    private void Validate()
    {
        if (!ValorPagamento.HasValue || ValorPagamento.Value <= 0)
        {
            ValidationError = "Informe um valor válido maior que zero.";
            return;
        }

        if (ValorPagamento.Value > _pendingPayment.SaldoPendente + 0.01m)
        {
            ValidationError = $"Valor excede o saldo pendente (R$ {_pendingPayment.SaldoPendente:N2}).";
            return;
        }

        ValidationError = null;
    }

    private async Task ConfirmAsync()
    {
        Validate();

        if (!string.IsNullOrWhiteSpace(ValidationError))
            return;

        try
        {
            var valor = ValorPagamento!.Value;

            var request = new RegisterPendingPaymentRequest(
                _pendingPayment.Id,
                valor,
                SelectedFormaPagamento,
                string.IsNullOrWhiteSpace(Observacoes) ? null : Observacoes);

            var result = await _pendingPaymentService.RegisterPaymentAsync(request);

            if (result.Success)
            {
                _toastService.ShowSuccess($"Pagamento de R$ {valor:N2} registrado com sucesso!");
                RequestClose?.Invoke(true);
            }
            else
            {
                ValidationError = result.ErrorMessage ?? "Erro ao registrar pagamento.";
            }
        }
        catch (Exception ex)
        {
            ValidationError = $"Erro: {ex.Message}";
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