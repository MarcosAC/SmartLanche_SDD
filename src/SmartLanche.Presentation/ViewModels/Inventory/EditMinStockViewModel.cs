using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Inventory;

public sealed class EditMinStockViewModel : INotifyPropertyChanged
{
    private readonly IInventoryService _inventoryService;
    private readonly IToastNotificationService _toastService;

    private decimal _minQuantity;
    private decimal _valorUnitario;
    private string _unidadeMedida = string.Empty;
    private string _observacoes = string.Empty;
    private bool _isSaving;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestClose;

    public int InventoryItemId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public decimal CurrentQuantity { get; private set; }

    public decimal MinQuantity
    {
        get => _minQuantity;
        set => SetProperty(ref _minQuantity, value);
    }

    public decimal ValorUnitario
    {
        get => _valorUnitario;
        set => SetProperty(ref _valorUnitario, value);
    }

    public string UnidadeMedida
    {
        get => _unidadeMedida;
        set => SetProperty(ref _unidadeMedida, value);
    }

    public string Notes
    {
        get => _observacoes;
        set => SetProperty(ref _observacoes, value);
    }

    // XAML-friendly alias
    public string Reason
    {
        get => _observacoes;
        set => Notes = value;
    }

    public bool IsSaving
    {
        get => _isSaving;
        private set => SetProperty(ref _isSaving, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public EditMinStockViewModel(IInventoryService inventoryService, IToastNotificationService toastService)
    {
        _inventoryService = inventoryService ?? throw new ArgumentNullException(nameof(inventoryService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => MinQuantity >= 0 && !IsSaving);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    public void Initialize(int inventoryItemId, string productName, decimal currentQuantity, decimal currentMinQuantity, decimal valorUnitario, string unidadeMedida, string? observacoes)
    {
        InventoryItemId = inventoryItemId;
        ProductName = productName;
        CurrentQuantity = currentQuantity;
        MinQuantity = currentMinQuantity;
        ValorUnitario = valorUnitario;
        UnidadeMedida = unidadeMedida;
        Notes = observacoes ?? string.Empty;
        OnPropertyChanged(nameof(ProductName));
        OnPropertyChanged(nameof(CurrentQuantity));
    }

    private async Task SaveAsync()
    {
        if (MinQuantity < 0)
            return;

        IsSaving = true;

        try
        {
            var request = new UpdateInventoryItemRequest(
                MinQuantity,
                ValorUnitario,
                UnidadeMedida,
                Notes);

            await _inventoryService.UpdateAsync(InventoryItemId, request);
            _toastService.ShowSuccess("Quantidade mínima atualizada com sucesso");
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao atualizar quantidade mínima: {ex.Message}");
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