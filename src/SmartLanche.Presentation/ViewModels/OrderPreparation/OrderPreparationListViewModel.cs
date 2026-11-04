using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.OrderPreparation;

public sealed class OrderPreparationListViewModel : ListViewModelBase<OrderPreparationDto, IOrderPreparationService>
{
    private readonly IToastNotificationService _toastService;
    private readonly IServiceProvider _services;

    private string _statusText = string.Empty;

    public event Action<OrderDto>? RequestViewOrder;

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public ICommand RefreshCommand { get; }
    public ICommand StartPreparationCommand { get; }
    public ICommand MarkReadyCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ViewDetailCommand { get; }

    public OrderPreparationListViewModel(
        IServiceProvider services,
        IToastNotificationService toastService,
        IOrderPreparationService orderPreparationService)
        : base(orderPreparationService)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        RefreshCommand = new RelayCommand(async _ => await LoadAsync());
        StartPreparationCommand = new RelayCommand<OrderPreparationDto>(async dto => await StartPreparationAsync(dto), dto => dto?.Status == OrderStatus.Pendente);
        MarkReadyCommand = new RelayCommand<OrderPreparationDto>(async dto => await MarkReadyAsync(dto), dto => dto?.Status == OrderStatus.EmPreparacao);
        CancelCommand = new RelayCommand<OrderPreparationDto>(async dto => await CancelAsync(dto), dto => dto?.Status == OrderStatus.Pendente || dto?.Status == OrderStatus.EmPreparacao);
        ViewDetailCommand = new RelayCommand<OrderPreparationDto>(async dto => await ViewDetailAsync(dto));

        Service.OrderChanged += OnOrderChanged;
    }

    protected override async Task<IReadOnlyList<OrderPreparationDto>> GetItemsAsync()
    {
        return await Service.GetPreparingOrdersAsync();
    }

    protected override void OnSearchTextChanged()
    {
        // Filter is applied in-memory for this view model
        _ = LoadAsync();
    }

    protected override IReadOnlyList<OrderPreparationDto> ApplySearchFilter(IReadOnlyList<OrderPreparationDto> items, string searchTerm)
    {
        return items
            .Where(o => o.Id.ToString().Contains(searchTerm)
                || o.CustomerName?.ToLowerInvariant().Contains(searchTerm) == true)
            .ToList();
    }

    protected override IReadOnlyList<OrderPreparationDto> ApplySort(IReadOnlyList<OrderPreparationDto> items)
    {
        // Default sort by status priority then by order date
        return items
            .OrderBy(o => o.Status)
            .ThenBy(o => o.OrderDate)
            .ToList();
    }

    public override async Task LoadAsync()
    {
        await base.LoadAsync();
        StatusText = $"{Items.Count} pedido(s) em preparação";
    }

    private void OnOrderChanged(object? sender, EventArgs e)
    {
        _ = LoadAsync();
    }

    private async Task StartPreparationAsync(OrderPreparationDto? orderDto)
    {
        if (orderDto is null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await Service.StartPreparationAsync(orderDto.Id);
            if (result is not null)
            {
                _toastService.ShowSuccess($"Pedido #{orderDto.Id} iniciado preparação");
                await LoadAsync();
            }
            else
            {
                _toastService.ShowError("Pedido não encontrado");
            }
        });
    }

    private async Task MarkReadyAsync(OrderPreparationDto? orderDto)
    {
        if (orderDto is null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await Service.MarkAsReadyAsync(orderDto.Id);
            if (result is not null)
            {
                _toastService.ShowSuccess($"Pedido #{orderDto.Id} marcado como pronto");
                await LoadAsync();
            }
            else
            {
                _toastService.ShowError("Pedido não encontrado");
            }
        });
    }

    private async Task CancelAsync(OrderPreparationDto? orderDto)
    {
        if (orderDto is null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var result = await Service.CancelOrderAsync(orderDto.Id);
            if (result is not null)
            {
                _toastService.ShowSuccess($"Pedido #{orderDto.Id} cancelado");
                await LoadAsync();
            }
            else
            {
                _toastService.ShowError("Pedido não encontrado");
            }
        });
    }

    private async Task ViewDetailAsync(OrderPreparationDto? orderDto)
    {
        if (orderDto is null) return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var orderService = _services.GetRequiredService<IOrderService>();
            var fullOrder = await orderService.GetByIdAsync(orderDto.Id);

            if (fullOrder is not null)
            {
                RequestViewOrder?.Invoke(fullOrder);
            }
            else
            {
                _toastService.ShowError("Pedido não encontrado");
            }
        });
    }
}