using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using System.Windows.Input;

namespace SmartLanche.Presentation.ViewModels.Inventory;

public sealed class InventoryMovementsViewModel : ListViewModelBase<InventoryMovementDto, IInventoryMovementService>
{
    private readonly IInventoryMovementService _movementService;
    private readonly IToastNotificationService _toastService;

    private StockStatus? _filterStatus;
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
    private InventoryMovementSummaryDto? _summary;
    private string _statusText = string.Empty;

    public new ICommand LoadCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    public StockStatus? FilterStatus
    {
        get => _filterStatus;
        set
        {
            if (SetProperty(ref _filterStatus, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public InventoryMovementFilter Filter
    {
        get => _filter;
        set => SetProperty(ref _filter, value);
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
        IToastNotificationService toastService) : base(movementService)
    {
        _movementService = movementService ?? throw new ArgumentNullException(nameof(movementService));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        ClearFiltersCommand = new RelayCommand(OnClearFilters);
    }

    protected override async Task<IReadOnlyList<InventoryMovementDto>> GetItemsAsync()
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

        return movements;
    }

    protected override IReadOnlyList<InventoryMovementDto> ApplySearchFilter(IReadOnlyList<InventoryMovementDto> items, string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return items.ToList();

        var term = searchTerm.ToLowerInvariant();
        return items.Where(m =>
            (m.ItemNome?.ToLowerInvariant().Contains(term) ?? false) ||
            (m.Notes?.ToLowerInvariant().Contains(term) ?? false) ||
            (m.ReferenceId?.ToLowerInvariant().Contains(term) ?? false) ||
            (m.Type.ToString().ToLowerInvariant().Contains(term)) ||
            (m.Origin.ToLowerInvariant().Contains(term))
        ).ToList();
    }

    public override async Task LoadAsync()
    {
        if (IsLoading)
            return;

        StatusText = "Carregando movimentações...";

        try
        {
            await base.LoadAsync();
            StatusText = $"{Items.Count} movimentação(ões) encontrada(s)";
        }
        catch (Exception ex)
        {
            _toastService.ShowError($"Erro ao carregar movimentações: {ex.Message}");
            StatusText = "Erro ao carregar";
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

        SearchText = string.Empty;
        FilterStatus = null;
        LoadCommand.Execute(null);
    }
}