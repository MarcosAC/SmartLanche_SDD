using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels.Inventory;

public sealed class InventoryMovementViewModel : INotifyPropertyChanged
{
    private readonly IInventoryMovementService _movementService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<InventoryMovementDto> _movements = [];
    private int _inventoryItemId;
    private DateTime? _filterFrom;
    private DateTime? _filterTo;
    private InventoryMovementType? _filterType;
    private bool _isLoading;

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? RequestClose;

    public ObservableCollection<InventoryMovementDto> Movements
    {
        get => _movements;
        private set => SetProperty(ref _movements, value);
    }

    public int InventoryItemId
    {
        get => _inventoryItemId;
        private set => SetProperty(ref _inventoryItemId, value);
    }

    public string ProductName { get; private set; } = string.Empty;

    public DateTime? FilterFrom
    {
        get => _filterFrom;
        set => SetProperty(ref _filterFrom, value);
    }

    public DateTime? FilterTo
    {
        get => _filterTo;
        set => SetProperty(ref _filterTo, value);
    }

    public InventoryMovementType? FilterType
    {
        get => _filterType;
        set => SetProperty(ref _filterType, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand CloseCommand { get; }

    public InventoryMovementViewModel(IInventoryMovementService movementService, IToastNotificationService toastService)
    {
        _movementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        CloseCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    public void Initialize(int inventoryItemId, string productName)
    {
        InventoryItemId = inventoryItemId;
        ProductName = productName;
        OnPropertyChanged(nameof(ProductName));
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;

        try
        {
            var movements = await _movementService.GetByItemAsync(InventoryItemId, FilterFrom, FilterTo);
            Movements = new ObservableCollection<InventoryMovementDto>(
                FilterType.HasValue
                    ? movements.Where(m => m.Tipo == FilterType.Value)
                    : movements);
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao carregar movimentações: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
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