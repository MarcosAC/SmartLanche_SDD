using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Orders;

public sealed class OrderListViewModel : ListViewModelBase<OrderDto, IOrderService>
{
    private int? _selectedStatus;
    private DateTime? _dateFrom;
    private DateTime? _dateTo;

    public event Action? RequestNewOrder;
    public event Action<OrderDto>? RequestViewOrder;
    public event Action<OrderDto>? RequestCloseOrder;

    public ICommand CloseOrderCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand MarkAsReadyCommand { get; }
    public ICommand FinalizeOrderCommand { get; }

    public ICommand ViewOrderCommand => EditItemCommand;

    public int? SelectedStatus
    {
        get => _selectedStatus;
        set
        {
            if (SetProperty(ref _selectedStatus, value))
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

    public OrderListViewModel(IOrderService orderService)
        : base(orderService)
    {
        // NewItemCommand and EditItemCommand are already initialized in base class
        // Just need to hook up the events
        RequestNewItem += () => RequestNewOrder?.Invoke();
        RequestEditItem += (order) => RequestViewOrder?.Invoke(order);

        CloseOrderCommand = new RelayCommand(async _ => await CloseOrderAsync(),
            _ => SelectedItem?.Status == OrderStatus.EmPreparacao && !IsLoading);

        MarkAsReadyCommand = new RelayCommand(async _ => await MarkAsReadyAsync(),
            _ => SelectedItem?.Status == OrderStatus.EmPreparacao && !IsLoading);

        FinalizeOrderCommand = new RelayCommand(_ =>
            {
                if (SelectedItem is not null)
                {
                    RequestCloseOrder?.Invoke(SelectedItem);
                }
            }, _ => SelectedItem?.Status == OrderStatus.Pronto && !IsLoading);

        CancelOrderCommand = new RelayCommand(async _ => await CancelOrderAsync(),
            _ => SelectedItem is not null &&
                 SelectedItem.Status != OrderStatus.Cancelado &&
                 SelectedItem.Status != OrderStatus.Finalizado &&
                 !IsLoading);
    }

    protected override async Task<IReadOnlyList<OrderDto>> GetItemsAsync()
    {
        var filter = new OrderFilterRequest
        {
            SearchTerm = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
            Status = SelectedStatus,
            DateFrom = DateFrom,
            DateTo = DateTo,
            SortBy = SortColumn,
            SortDescending = SortDescending,
            // Include cancelled orders when explicitly filtering for Cancelado status
            ExcludeCancelled = SelectedStatus != (int)OrderStatus.Cancelado
        };

        return await Service.GetAllAsync(filter);
    }

    protected override IReadOnlyList<OrderDto> ApplySearchFilter(IReadOnlyList<OrderDto> items, string searchTerm)
    {
        // Search is handled server-side via filter, but keep for fallback
        return items.Where(o =>
            o.Id.ToString().Contains(searchTerm) ||
            o.CustomerName?.ToLowerInvariant().Contains(searchTerm) == true
        ).ToList();
    }

    protected override IReadOnlyList<OrderDto> ApplySort(IReadOnlyList<OrderDto> items)
    {
        // Sorting is handled server-side via filter, but keep for fallback
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.OrderByDescending(o => o.OrderDate).ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "id" => items.OrderBy(o => o.Id),
            "customername" or "clientenome" => items.OrderBy(o => o.CustomerName),
            "total" or "valortotal" => items.OrderBy(o => o.TotalAmount),
            "status" => items.OrderBy(o => o.Status),
            "orderdate" or "data" => items.OrderBy(o => o.OrderDate),
            _ => items.OrderByDescending(o => o.OrderDate)
        };

        return SortDescending ? sorted.Reverse().ToList() : sorted.ToList();
    }

    private async Task CloseOrderAsync()
    {
        if (SelectedItem is null || IsLoading) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var request = new CloseOrderRequest { OrderId = SelectedItem.Id };
            await Service.CloseAsync(request);

            SuccessMessage = "Pedido fechado com sucesso.";
            await LoadItemsAsync();
        });
    }

    private async Task CancelOrderAsync()
    {
        if (SelectedItem is null || IsLoading) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            // DeleteIfPendente = false: mark as Cancelado but keep in DB (list behavior)
            var request = new CancelOrderRequest { OrderId = SelectedItem.Id, DeleteIfPendente = false };
            await Service.CancelAsync(request);

            SuccessMessage = "Pedido cancelado com sucesso.";
            await LoadItemsAsync();
        });
    }

    private async Task MarkAsReadyAsync()
    {
        if (SelectedItem is null || IsLoading) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            await Service.MarkAsReadyAsync(SelectedItem.Id);
            SuccessMessage = "Pedido marcado como pronto.";
            await LoadItemsAsync();
        });
    }

    }