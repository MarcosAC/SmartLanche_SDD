using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Payments;

public sealed class PendingPaymentHistoryViewModel : INotifyPropertyChanged
{
    private readonly IPendingPaymentService _pendingPaymentService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<PendingPaymentHistoryDto> _history = [];
    private bool _isLoading;
    private int _pendingPaymentId;

    public PendingPaymentHistoryViewModel(
        IPendingPaymentService pendingPaymentService,
        IToastNotificationService toastService)
    {
        _pendingPaymentService = pendingPaymentService ?? throw new ArgumentNullException(nameof(pendingPaymentService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
    }

    public ObservableCollection<PendingPaymentHistoryDto> History { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public int PendingPaymentId
    {
        get => _pendingPaymentId;
        private set => SetProperty(ref _pendingPaymentId, value);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task InitializeAsync(int pendingPaymentId)
    {
        PendingPaymentId = pendingPaymentId;
        await LoadAsync();
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;

            var history = await _pendingPaymentService.GetHistoryAsync(PendingPaymentId);

            History.Clear();
            foreach (var item in history)
            {
                History.Add(item);
            }
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao carregar histórico: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
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