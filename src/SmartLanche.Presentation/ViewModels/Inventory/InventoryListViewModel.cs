using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Inventory;

public sealed class InventoryListViewModel : ListViewModelBase<InventoryItemDto, IInventoryService>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IToastNotificationService _toastService;

    private StockStatus? _filterStatus;
    private string _statusText = string.Empty;

    public event EventHandler<InventoryItemDto>? RequestAddStock;
    public event EventHandler<InventoryItemDto>? RequestRemoveStock;
    public event EventHandler<InventoryItemDto>? RequestViewHistory;
    public event EventHandler<InventoryItemDto>? RequestEditMinStock;

    public ICommand AddStockCommand { get; }
    public ICommand RemoveStockCommand { get; }
    public ICommand ViewHistoryCommand { get; }
    public ICommand EditMinStockCommand { get; }
    public ICommand RefreshCommand { get; }

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

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public InventoryListViewModel(
        IInventoryService inventoryService,
        IInventoryRepository inventoryRepository,
        IToastNotificationService toastService)
        : base(inventoryService)
    {
        _inventoryRepository = inventoryRepository ?? throw new ArgumentNullException(nameof(inventoryRepository));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        AddStockCommand = new RelayCommand<InventoryItemDto>(OnAddStock);
        RemoveStockCommand = new RelayCommand<InventoryItemDto>(OnRemoveStock);
        ViewHistoryCommand = new RelayCommand<InventoryItemDto>(OnViewHistory);
        EditMinStockCommand = new RelayCommand<InventoryItemDto>(OnEditMinStock);
        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
    }

    protected override async Task<IReadOnlyList<InventoryItemDto>> GetItemsAsync()
    {
        var items = await _inventoryRepository.GetAllAsync(null, null, CancellationToken.None);
        return items.Select(MapToDto).ToList();
    }

    protected override IReadOnlyList<InventoryItemDto> ApplySearchFilter(IReadOnlyList<InventoryItemDto> items, string searchTerm)
    {
        var filtered = items.Where(i =>
            i.Id.ToString().Contains(searchTerm) ||
            i.ProdutoNome?.ToLowerInvariant().Contains(searchTerm) == true ||
            i.Nome?.ToLowerInvariant().Contains(searchTerm) == true ||
            i.UnidadeMedida?.ToLowerInvariant().Contains(searchTerm) == true
        );

        if (FilterStatus.HasValue)
        {
            filtered = filtered.Where(i => i.Status == FilterStatus.Value);
        }

        return filtered.ToList();
    }

    protected override IReadOnlyList<InventoryItemDto> ApplySort(IReadOnlyList<InventoryItemDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            // Default: low stock first, then by product name
            return items
                .OrderByDescending(i => i.Status)
                .ThenBy(i => i.ProdutoNome)
                .ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "id" => items.OrderBy(i => i.Id),
            "produto" or "produtonome" => items.OrderBy(i => i.ProdutoNome),
            "nome" => items.OrderBy(i => i.Nome),
            "quantidadeatual" or "quantidade" or "estoque" => items.OrderBy(i => i.QuantidadeAtual),
            "quantidademinima" or "minimo" => items.OrderBy(i => i.QuantidadeMinima),
            "valorunitario" or "valor" => items.OrderBy(i => i.ValorUnitario),
            "valortotalestoque" or "valortotal" => items.OrderBy(i => i.ValorTotalEstoque),
            "unidademedida" or "unidade" => items.OrderBy(i => i.UnidadeMedida),
            "estoquebaixo" or "baixo" => items.OrderByDescending(i => i.EstoqueBaixo),
            "estoquezerado" or "zerado" => items.OrderByDescending(i => i.EstoqueZerado),
            _ => items.OrderBy(i => i.ProdutoNome)
        };

        return SortDescending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
        StatusText = $"{Items.Count} itens no estoque";
    }

    private static InventoryItemDto MapToDto(InventoryItem item)
    {
        return new InventoryItemDto(
            item.Id,
            item.ProductId,
            item.Produto?.Name,
            item.Nome,
            item.QuantidadeAtual,
            item.QuantidadeMinima,
            item.ValorUnitario,
            item.UnidadeMedida,
            item.Observacoes,
            item.EstoqueBaixo,
            item.EstoqueZerado,
            item.ValorTotalEstoque);
    }

    private void OnAddStock(InventoryItemDto? item)
    {
        if (item is not null)
            RequestAddStock?.Invoke(this, item);
    }

    private void OnRemoveStock(InventoryItemDto? item)
    {
        if (item is not null)
            RequestRemoveStock?.Invoke(this, item);
    }

    private void OnViewHistory(InventoryItemDto? item)
    {
        if (item is not null)
            RequestViewHistory?.Invoke(this, item);
    }

    private void OnEditMinStock(InventoryItemDto? item)
    {
        if (item is not null)
            RequestEditMinStock?.Invoke(this, item);
    }
}