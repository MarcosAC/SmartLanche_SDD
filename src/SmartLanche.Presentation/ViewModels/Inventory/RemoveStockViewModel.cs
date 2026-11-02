using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Inventory;

public sealed class RemoveStockViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;
    private readonly IToastNotificationService _toastService;

    private int _quantity;
    private string _note = string.Empty;
    private bool _isSaving;
    private decimal _minQuantity;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestClose;

    public int InventoryItemId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal CurrentQuantity { get; private set; }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, value);
    }

    public string Note
    {
        get => _note;
        set => SetProperty(ref _note, value);
    }

    // XAML-friendly aliases
    public string Reason
    {
        get => _note;
        set => Note = value;
    }

    public decimal MinQuantity
    {
        get => _minQuantity;
        private set => SetProperty(ref _minQuantity, value);
    }

    public bool IsLowStock => CurrentQuantity <= MinQuantity;

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public RemoveStockViewModel(IInventoryService inventoryService, IToastNotificationService toastService)
    {
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => Quantity > 0 && Quantity <= CurrentQuantity && !IsSaving);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    public void Initialize(int inventoryItemId, string productName, decimal currentQuantity, decimal minQuantity = 0)
    {
        InventoryItemId = inventoryItemId;
        ProductName = productName;
        CurrentQuantity = currentQuantity;
        MinQuantity = minQuantity;
        Quantity = 1;
        Note = string.Empty;
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(CurrentQuantity));
        OnPropertyChanged(nameof(MinQuantity));
        OnPropertyChanged(nameof(IsLowStock));
    }

    private async Task SaveAsync()
    {
        if (Quantity <= 0 || Quantity > CurrentQuantity)
            return;

        IsSaving = true;

        try
        {
            var request = new RegistrarSaidaRequest(
                InventoryItemId,
                Quantity,
                InventoryMovementReason.Ajuste,
                Note);

            await _inventoryService.RegistrarSaidaAsync(request);
            _toastService.ShowSuccess("Saída de estoque registrada com sucesso");
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao registrar saída: {ex.Message}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}