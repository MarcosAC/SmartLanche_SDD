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

public sealed class InventoryMovementsViewModel : INotifyPropertyChanged
{
    private readonly IInventoryMovementService _movementService;
    private readonly IToastNotificationService _toastService;

    private ObservableCollection<InventoryMovementDto> _movements = [];
    private InventoryMovementFilter _filter = new(
        ItemId: null,
        FromDate: DateTime.Today.AddDays(-30),
        ToDate: DateTime.Today,
        Tipo: null,
        Motivo: null,
        OrderId: null,
        ComboId: null,
        SearchTerm: null,
        Page: 1,
        PageSize: 50);
    private bool _isLoading;
    private InventoryMovementSummaryDto? _summary;
    private string _statusText = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public ObservableCollection<InventoryMovementDto> Movements
    {
        get => _movements;
        private set => SetProperty(ref _movements, value);
    }

    public InventoryMovementFilter Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public InventoryMovementSummaryDto? Summary
    {
        get => _summary;
        private set => SetProperty(ref _summary, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public InventoryMovementsViewModel(
        IInventoryMovementService movementService,
        IToastNotificationService toastService)
    {
        _movementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        ClearFiltersCommand = new RelayCommand(OnClearFilters);
    }

    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        StatusText = "Carregando movimentações...";

        try
        {
            var movements = await _movementService.GetFilteredAsync(
                Filter.ItemId,
                Filter.FromDate,
                Filter.ToDate,
                Filter.Tipo,
                Filter.Motivo,
                Filter.OrderId,
                Filter.ComboId,
                Filter.SearchTerm,
                Filter.Page,
                Filter.PageSize);

            Movements = new ObservableCollection<InventoryMovementDto>(movements);

            // Summary not available in new API - skip for now
            StatusText = $"{Movements.Count} movimentação(ões) encontrada(s)";
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao carregar movimentações: {ex.Message}");
            StatusText = "Erro ao carregar";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnClearFilters(object? _)
    {
        Filter = new InventoryMovementFilter(
            ItemId: null,
            FromDate: DateTime.Today.AddDays(-30),
            ToDate: DateTime.Today,
            Tipo: null,
            Motivo: null,
            OrderId: null,
            ComboId: null,
            SearchTerm: null,
            Page: 1,
            PageSize: 50);
        LoadCommand.Execute(null);
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
            return false;

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}