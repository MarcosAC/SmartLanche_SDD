using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels.Categories;
using SmartLanche.Presentation.ViewModels.Combos;
using SmartLanche.Presentation.ViewModels.Customers;
using SmartLanche.Presentation.ViewModels.Orders;
using SmartLanche.Presentation.ViewModels.OrderPreparation;
using SmartLanche.Presentation.ViewModels.Inventory;
using SmartLanche.Presentation.ViewModels.Payments;
using SmartLanche.Presentation.ViewModels.Products;
using SmartLanche.Presentation.ViewModels.Promotions;

namespace SmartLanche.Presentation.ViewModels;

public sealed class HomeViewModel : INotifyPropertyChanged
{
    private readonly IOrderService _orderService;

    private int _openOrdersCount;
    private decimal _todayRevenue;
    private IReadOnlyList<ProductSalesDto> _topProducts = [];

    public HomeViewModel(IOrderService orderService)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
    }

    public int OpenOrdersCount
    {
        get => _openOrdersCount;
        private set => SetProperty(ref _openOrdersCount, value);
    }

    public decimal TodayRevenue
    {
        get => _todayRevenue;
        private set => SetProperty(ref _todayRevenue, value);
    }

    public IReadOnlyList<ProductSalesDto> TopProducts
    {
        get => _topProducts;
        private set => SetProperty(ref _topProducts, value);
    }

    public ProductSalesDto? FirstTopProduct => _topProducts.Count > 0 ? _topProducts[0] : null;

    public event PropertyChangedEventHandler? PropertyChanged;

    public async Task LoadDashboardDataAsync()
    {
        try
        {
            // Get all orders to compute metrics
            var orders = await _orderService.GetAllAsync(null);

            // Open orders count (EmPreparacao + Pronto)
            OpenOrdersCount = orders.Count(o => o.Status == OrderStatus.EmPreparacao || o.Status == OrderStatus.Pronto);

            // Today's revenue (finalized orders today)
            var today = DateTime.Today;
            TodayRevenue = orders
                .Where(o => o.Status == OrderStatus.Finalizado && o.OrderDate.Date == today)
                .Sum(o => o.TotalAmount);

            // Top products (last 7 days, top 5)
            TopProducts = await _orderService.GetTopProductsAsync(5, 7);
        }
        catch (Exception)
        {
            // Ignore - show zeros/empty
            OpenOrdersCount = 0;
            TodayRevenue = 0;
            TopProducts = [];
        }
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly IServiceProvider _services;
    private readonly IToastNotificationService _toastService;

    private object _currentViewModel;
    private string _currentPageTitle = "Painel de gestão";

    private ProductListViewModel? _productListViewModel;
    private CategoryListViewModel? _categoryListViewModel;
    private CustomerListViewModel? _customerListViewModel;
    private OrderListViewModel? _orderListViewModel;
    private PromotionListViewModel? _promotionListViewModel;
    private ComboListViewModel? _comboListViewModel;
    private PendingPaymentListViewModel? _pendingPaymentListViewModel;
    private PaymentListViewModel? _paymentListViewModel;
    private DevedoresViewModel? _devedoresViewModel;
    private OrderPreparationListViewModel? _orderPreparationListViewModel;
    private InventoryListViewModel? _inventoryListViewModel;
    private InventoryMovementsViewModel? _inventoryMovementsViewModel;
    private HomeViewModel? _homeViewModel;

    public IToastNotificationService ToastService => _toastService;

    public MainViewModel(IServiceProvider services, IToastNotificationService toastService)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        NavigateHomeCommand = new RelayCommand(async _ => await NavigateHomeAsync());

        NavigateProductsCommand = new RelayCommand(async _ => await NavigateProductsAsync());

        NavigateCategoriesCommand = new RelayCommand(async _ => await NavigateCategoriesAsync());

        NavigateCustomersCommand = new RelayCommand(async _ => await NavigateCustomersAsync());

        NavigateOrdersCommand = new RelayCommand(async _ => await NavigateOrdersAsync());

        NavigatePromotionsCommand = new RelayCommand(async _ => await NavigatePromotionsAsync());

        NavigateCombosCommand = new RelayCommand(async _ => await NavigateCombosAsync());

        NavigatePaymentsCommand = new RelayCommand(async _ => await NavigatePaymentsAsync());

        NavigatePaymentListCommand = new RelayCommand(async _ => await NavigatePaymentListAsync());

        NavigateDevedoresCommand = new RelayCommand(async _ => await NavigateDevedoresAsync());

        NavigateOrderPreparationCommand = new RelayCommand(async _ => await NavigateOrderPreparationAsync());

        NavigateInventoryCommand = new RelayCommand(async _ => await NavigateInventoryAsync());

        NavigateInventoryMovementsCommand = new RelayCommand(async _ => await NavigateInventoryMovementsAsync());

        BackCommand = new RelayCommand(async _ => await GoBackAsync(), _ => CurrentViewModel is ProductFormViewModel or CategoryFormViewModel or CustomerFormViewModel or OrderFormViewModel or PromotionFormViewModel or ComboFormViewModel);

        InitializeListViewModels();

        _currentViewModel = _homeViewModel!;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            if (SetProperty(ref _currentViewModel, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string CurrentPageTitle
    {
        get => _currentPageTitle;
        private set => SetProperty(ref _currentPageTitle, value);
    }

    public ICommand NavigateHomeCommand { get; }

    public ICommand NavigateProductsCommand { get; }

    public ICommand NavigateCategoriesCommand { get; }

    public ICommand NavigateCustomersCommand { get; }

    public ICommand NavigateOrdersCommand { get; }

    public ICommand NavigatePromotionsCommand { get; }

    public ICommand NavigateCombosCommand { get; }

    public ICommand NavigatePaymentsCommand { get; }

    public ICommand NavigatePaymentListCommand { get; }

    public ICommand NavigateDevedoresCommand { get; }

    public ICommand NavigateOrderPreparationCommand { get; }

    public ICommand NavigateInventoryCommand { get; }

    public ICommand NavigateInventoryMovementsCommand { get; }

    public ICommand BackCommand { get; }

    private void InitializeListViewModels()
    {
        _homeViewModel = _services.GetRequiredService<HomeViewModel>();

        _productListViewModel = _services.GetRequiredService<ProductListViewModel>();

        _productListViewModel.RequestNewItem += OnRequestNewProduct;

        _productListViewModel.RequestEditItem += OnRequestEditProduct;

        _categoryListViewModel = _services.GetRequiredService<CategoryListViewModel>();

        _categoryListViewModel.RequestNewItem += OnRequestNewCategory;

        _categoryListViewModel.RequestEditItem += OnRequestEditCategory;

        _customerListViewModel = _services.GetRequiredService<CustomerListViewModel>();

        _customerListViewModel.RequestNewItem += OnRequestNewCustomer;

        _customerListViewModel.RequestEditItem += OnRequestEditCustomer;

        _orderListViewModel = _services.GetRequiredService<OrderListViewModel>();

        _orderListViewModel.RequestNewOrder += OnRequestNewOrder;

        _orderListViewModel.RequestViewOrder += OnRequestViewOrder;

        _promotionListViewModel = _services.GetRequiredService<PromotionListViewModel>();

        _promotionListViewModel.RequestNewItem += OnRequestNewPromotion;

        _promotionListViewModel.RequestEditItem += OnRequestEditPromotion;

        _comboListViewModel = _services.GetRequiredService<ComboListViewModel>();

        _comboListViewModel.RequestNewItem += OnRequestNewCombo;

        _comboListViewModel.RequestEditItem += OnRequestEditCombo;

        _pendingPaymentListViewModel = _services.GetRequiredService<PendingPaymentListViewModel>();

        _paymentListViewModel = _services.GetRequiredService<PaymentListViewModel>();

        _devedoresViewModel = _services.GetRequiredService<DevedoresViewModel>();

        _orderPreparationListViewModel = _services.GetRequiredService<OrderPreparationListViewModel>();
        _orderPreparationListViewModel.RequestViewOrder += OnRequestViewOrder;

        _inventoryListViewModel = _services.GetRequiredService<InventoryListViewModel>();
        _inventoryListViewModel.RequestAddStock += OnRequestAddStock;
        _inventoryListViewModel.RequestRemoveStock += OnRequestRemoveStock;
        _inventoryListViewModel.RequestViewHistory += OnRequestViewHistory;
        _inventoryListViewModel.RequestEditMinStock += OnRequestEditMinStock;

        _inventoryMovementsViewModel = _services.GetRequiredService<InventoryMovementsViewModel>();
    }

    private async Task NavigateHomeAsync()
    {
        if (_homeViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Painel de gestão";
        CurrentViewModel = _homeViewModel;

        await _homeViewModel.LoadDashboardDataAsync();
    }

    private async Task NavigateProductsAsync()
    {
        if (_productListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Produtos";
        CurrentViewModel = _productListViewModel;

        await _productListViewModel.LoadAsync();
    }

    private async Task NavigateCategoriesAsync()
    {
        if (_categoryListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Categorias";
        CurrentViewModel = _categoryListViewModel;

        await _categoryListViewModel.LoadAsync();
    }

    private async Task NavigateCustomersAsync()
    {
        if (_customerListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Clientes";
        CurrentViewModel = _customerListViewModel;

        await _customerListViewModel.LoadAsync();
    }

    private async Task NavigateOrdersAsync()
    {
        if (_orderListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Pedidos";
        CurrentViewModel = _orderListViewModel;

        await _orderListViewModel.LoadAsync();
    }

    private async Task NavigatePromotionsAsync()
    {
        if (_promotionListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Promoções";
        CurrentViewModel = _promotionListViewModel;

        await _promotionListViewModel.LoadAsync();
    }

    private async Task NavigateCombosAsync()
    {
        if (_comboListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Combos";
        CurrentViewModel = _comboListViewModel;

        await _comboListViewModel.LoadAsync();
    }

    private async Task NavigatePaymentsAsync()
    {
        if (_pendingPaymentListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Pendências (Fiado)";
        CurrentViewModel = _pendingPaymentListViewModel;

        await _pendingPaymentListViewModel.LoadAsync();
    }

    private async Task NavigatePaymentListAsync()
    {
        if (_paymentListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Pagamentos";
        CurrentViewModel = _paymentListViewModel;

        await _paymentListViewModel.LoadAsync();
    }

    private async Task NavigateDevedoresAsync()
    {
        if (_devedoresViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Inadimplência (Fiado)";
        CurrentViewModel = _devedoresViewModel;

        await _devedoresViewModel.LoadAsync();
    }

    private async Task NavigateOrderPreparationAsync()
    {
        if (_orderPreparationListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Pedidos em Preparação";
        CurrentViewModel = _orderPreparationListViewModel;

        await _orderPreparationListViewModel.LoadAsync();
    }

    private async Task NavigateInventoryAsync()
    {
        if (_inventoryListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Estoque";
        CurrentViewModel = _inventoryListViewModel;

        await _inventoryListViewModel.LoadAsync();
    }

    private async Task NavigateInventoryMovementsAsync()
    {
        if (_inventoryMovementsViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Movimentações de Estoque";
        CurrentViewModel = _inventoryMovementsViewModel;

        await _inventoryMovementsViewModel.LoadAsync();
    }

    private async void OnRequestNewProduct()
    {
        await OpenProductFormAsync(null);
    }

    private async void OnRequestEditProduct(ProductDto product)
    {
        await OpenProductFormAsync(product);
    }

    private async Task OpenProductFormAsync(ProductDto? product)
    {
        var viewModel = _services.GetRequiredService<ProductFormViewModel>();

        viewModel.PropertyChanged += OnProductFormPropertyChanged;

        CurrentPageTitle = product is null
            ? "Novo produto"
            : "Editar produto";

        CurrentViewModel = viewModel;

        await viewModel.InitializeAsync(product);
    }

    private async void OnRequestNewPromotion()
    {
        await OpenPromotionFormAsync(null);
    }

    private async void OnRequestEditPromotion(PromotionDto promotion)
    {
        await OpenPromotionFormAsync(promotion);
    }

    private async Task OpenPromotionFormAsync(PromotionDto? promotion)
    {
        var viewModel = _services.GetRequiredService<PromotionFormViewModel>();

        viewModel.PropertyChanged += OnPromotionFormPropertyChanged;

        CurrentPageTitle = promotion is null
            ? "Nova promoção"
            : "Editar promoção";

        CurrentViewModel = viewModel;

        await viewModel.InitializeAsync(promotion);
    }

    private async void OnRequestNewCombo()
    {
        await OpenComboFormAsync(null);
    }

    private async void OnRequestEditCombo(ComboDto combo)
    {
        await OpenComboFormAsync(combo);
    }

    private async Task OpenComboFormAsync(ComboDto? combo)
    {
        var viewModel = _services.GetRequiredService<ComboFormViewModel>();

        viewModel.PropertyChanged += OnComboFormPropertyChanged;

        CurrentPageTitle = combo is null
            ? "Novo combo"
            : "Editar combo";

        CurrentViewModel = viewModel;

        await viewModel.InitializeAsync(combo);
    }

    private async void OnRequestNewCategory()
    {
        await OpenCategoryFormAsync(null);
    }

    private async void OnRequestEditCategory(CategoryDto category)
    {
        await OpenCategoryFormAsync(category);
    }

    private async Task OpenCategoryFormAsync(CategoryDto? category)
    {
        var viewModel = _services.GetRequiredService<CategoryFormViewModel>();

        viewModel.PropertyChanged += OnCategoryFormPropertyChanged;

        CurrentPageTitle = category is null
            ? "Nova categoria"
            : "Editar categoria";

        CurrentViewModel = viewModel;

        await viewModel.InitializeAsync(category);
    }

    private async void OnRequestNewCustomer()
    {
        await OpenCustomerFormAsync(null);
    }

    private async void OnRequestEditCustomer(CustomerDto customer)
    {
        await OpenCustomerFormAsync(customer);
    }

    private async Task OpenCustomerFormAsync(CustomerDto? customer)
    {
        var viewModel = _services.GetRequiredService<CustomerFormViewModel>();

        viewModel.PropertyChanged += OnCustomerFormPropertyChanged;

        CurrentPageTitle = customer is null
            ? "Novo cliente"
            : "Editar cliente";

        CurrentViewModel = viewModel;

        await viewModel.InitializeAsync(customer);
    }

    private async void OnRequestNewOrder()
    {
        await OpenOrderFormAsync(null);
    }

    private async void OnRequestViewOrder(OrderDto order)
    {
        await OpenOrderDetailAsync(order);
    }

    private async Task OpenOrderDetailAsync(OrderDto order)
    {
        try
        {
            var viewModel = _services.GetRequiredService<OrderDetailViewModel>();

            viewModel.RequestClose += OnOrderDetailClose;

            CurrentPageTitle = "Detalhes do pedido";
            CurrentViewModel = viewModel;

            await viewModel.InitializeAsync(order.Id);
        }
        catch (Exception ex)
        {
            // Log to file
            try
            {
                var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "error.log");
                var logMsg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - OpenOrderDetailAsync ERROR:\n{ex}\n\n";
                System.IO.File.AppendAllText(logPath, logMsg);
            }
            catch { }

            System.Windows.MessageBox.Show($"Erro ao abrir detalhes do pedido:\n{ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private async void OnOrderDetailClose()
    {
        if (CurrentViewModel is OrderDetailViewModel viewModel)
        {
            viewModel.RequestClose -= OnOrderDetailClose;
            await ReturnToOrdersAsync();
        }
    }

    private async Task OpenOrderFormAsync(OrderDto? order)
    {
        try
        {
            var viewModel = _services.GetRequiredService<OrderFormViewModel>();

            viewModel.PropertyChanged += OnOrderFormPropertyChanged;

            CurrentPageTitle = order is null
                ? "Novo pedido"
                : "Detalhes do pedido";

            CurrentViewModel = viewModel;

            await viewModel.InitializeAsync(order);
        }
        catch (Exception ex)
        {
            // Log to file
            try
            {
                var logPath = System.IO.Path.Combine(AppContext.BaseDirectory, "error.log");
                var logMsg = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - OpenOrderFormAsync ERROR:\n{ex}\n\n";
                System.IO.File.AppendAllText(logPath, logMsg);
            }
            catch { }

            System.Windows.MessageBox.Show($"Erro ao abrir formulário de pedido:\n{ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnProductFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ProductFormViewModel.WasSaved))
        {
            return;
        }

        if (sender is ProductFormViewModel { WasSaved: true } viewModel)
        {
            viewModel.PropertyChanged -= OnProductFormPropertyChanged;

            _ = ReturnToProductsAsync();
        }
    }

    private void OnCategoryFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CategoryFormViewModel.WasSaved))
        {
            return;
        }

        if (sender is CategoryFormViewModel { WasSaved: true } viewModel)
        {
            viewModel.PropertyChanged -= OnCategoryFormPropertyChanged;

            _ = ReturnToCategoriesAsync();
        }
    }

    private void OnCustomerFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(CustomerFormViewModel.WasSaved))
        {
            return;
        }

        if (sender is CustomerFormViewModel { WasSaved: true } viewModel)
        {
            viewModel.PropertyChanged -= OnCustomerFormPropertyChanged;

            _ = ReturnToCustomersAsync();
        }
    }

    private void OnOrderFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(OrderFormViewModel.WasSaved))
        {
            return;
        }

        if (sender is OrderFormViewModel { WasSaved: true } viewModel)
        {
            viewModel.PropertyChanged -= OnOrderFormPropertyChanged;

            _ = ReturnToOrdersAsync();
        }
    }

    private void OnPromotionFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(PromotionFormViewModel.WasSaved))
        {
            return;
        }

        if (sender is PromotionFormViewModel { WasSaved: true } viewModel)
        {
            viewModel.PropertyChanged -= OnPromotionFormPropertyChanged;

            _ = ReturnToPromotionsAsync();
        }
    }

    private void OnComboFormPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ComboFormViewModel.WasSaved))
        {
            return;
        }

        if (sender is ComboFormViewModel { WasSaved: true } viewModel)
        {
            viewModel.PropertyChanged -= OnComboFormPropertyChanged;

            _ = ReturnToCombosAsync();
        }
    }

    private void OnRequestAddStock(object? sender, InventoryItemDto item)
    {
        OpenAddStockModal(item);
    }

    private void OnRequestRemoveStock(object? sender, InventoryItemDto item)
    {
        OpenRemoveStockModal(item);
    }

    private void OnRequestViewHistory(object? sender, InventoryItemDto item)
    {
        OpenInventoryMovementModal(item);
    }

    private void OnRequestEditMinStock(object? sender, InventoryItemDto item)
    {
        OpenEditMinStockModal(item);
    }

    private void OpenAddStockModal(InventoryItemDto item)
    {
        var viewModel = _services.GetRequiredService<AddStockViewModel>();
        viewModel.Initialize(item.Id, item.ProdutoNome ?? item.Nome, item.QuantidadeAtual, item.QuantidadeMinima);
        viewModel.RequestClose += () =>
        {
            viewModel.RequestClose -= () => { };
            _ = NavigateInventoryAsync();
        };
        CurrentPageTitle = $"Entrada - {item.ProdutoNome ?? item.Nome}";
        CurrentViewModel = viewModel;
    }

    private void OpenRemoveStockModal(InventoryItemDto item)
    {
        var viewModel = _services.GetRequiredService<RemoveStockViewModel>();
        viewModel.Initialize(item.Id, item.ProdutoNome ?? item.Nome, item.QuantidadeAtual, item.QuantidadeMinima);
        viewModel.RequestClose += () =>
        {
            viewModel.RequestClose -= () => { };
            _ = NavigateInventoryAsync();
        };
        CurrentPageTitle = $"Saída - {item.ProdutoNome ?? item.Nome}";
        CurrentViewModel = viewModel;
    }

    private void OpenInventoryMovementModal(InventoryItemDto item)
    {
        var viewModel = _services.GetRequiredService<InventoryMovementViewModel>();
        viewModel.Initialize(item.Id, item.ProdutoNome ?? item.Nome);
        viewModel.RequestClose += () =>
        {
            viewModel.RequestClose -= () => { };
            _ = NavigateInventoryAsync();
        };
        CurrentPageTitle = $"Histórico - {item.ProdutoNome ?? item.Nome}";
        CurrentViewModel = viewModel;
    }

    private void OpenEditMinStockModal(InventoryItemDto item)
    {
        var viewModel = _services.GetRequiredService<EditMinStockViewModel>();
        viewModel.Initialize(item.Id, item.ProdutoNome ?? item.Nome, item.QuantidadeAtual, item.QuantidadeMinima, item.ValorUnitario, item.UnidadeMedida, item.Observacoes);
        viewModel.RequestClose += () =>
        {
            viewModel.RequestClose -= () => { };
            _ = NavigateInventoryAsync();
        };
        CurrentPageTitle = $"Qtd. Mínima - {item.ProdutoNome ?? item.Nome}";
        CurrentViewModel = viewModel;
    }

    private async Task GoBackAsync()
    {
        if (CurrentViewModel is ProductFormViewModel productForm)
        {
            productForm.PropertyChanged -= OnProductFormPropertyChanged;
            await ReturnToProductsAsync();
            return;
        }

        if (CurrentViewModel is CategoryFormViewModel categoryForm)
        {
            categoryForm.PropertyChanged -= OnCategoryFormPropertyChanged;
            await ReturnToCategoriesAsync();
            return;
        }

        if (CurrentViewModel is CustomerFormViewModel customerForm)
        {
            customerForm.PropertyChanged -= OnCustomerFormPropertyChanged;
            await ReturnToCustomersAsync();
            return;
        }

        if (CurrentViewModel is OrderFormViewModel orderForm)
        {
            orderForm.PropertyChanged -= OnOrderFormPropertyChanged;
            await ReturnToOrdersAsync();
            return;
        }

        if (CurrentViewModel is PromotionFormViewModel promotionForm)
        {
            promotionForm.PropertyChanged -= OnPromotionFormPropertyChanged;
            await ReturnToPromotionsAsync();
            return;
        }

        if (CurrentViewModel is ComboFormViewModel comboForm)
        {
            comboForm.PropertyChanged -= OnComboFormPropertyChanged;
            await ReturnToCombosAsync();
        }
    }

    private async Task ReturnToProductsAsync()
    {
        if (_productListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Produtos";
        CurrentViewModel = _productListViewModel;

        await _productListViewModel.LoadAsync();
    }

    private async Task ReturnToCategoriesAsync()
    {
        if (_categoryListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Categorias";
        CurrentViewModel = _categoryListViewModel;

        await _categoryListViewModel.LoadAsync();
    }

    private async Task ReturnToCustomersAsync()
    {
        if (_customerListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Clientes";
        CurrentViewModel = _customerListViewModel;

        await _customerListViewModel.LoadAsync();
    }

    private async Task ReturnToOrdersAsync()
    {
        if (_orderListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Pedidos";
        CurrentViewModel = _orderListViewModel;

        await _orderListViewModel.LoadAsync();
    }

    private async Task ReturnToPromotionsAsync()
    {
        if (_promotionListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Promoções";
        CurrentViewModel = _promotionListViewModel;

        await _promotionListViewModel.LoadAsync();
    }

    private async Task ReturnToCombosAsync()
    {
        if (_comboListViewModel is null)
        {
            return;
        }

        CurrentPageTitle = "Combos";
        CurrentViewModel = _comboListViewModel;

        await _comboListViewModel.LoadAsync();
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