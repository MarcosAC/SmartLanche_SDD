using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Commands;

namespace SmartLanche.Presentation.ViewModels.Orders;

public sealed class OrderFormViewModel : INotifyPropertyChanged
{
    private readonly IOrderService _orderService;
    private readonly IProductService _productService;
    private readonly ICustomerService _customerService;
    private readonly ICategoryService _categoryService;
    private readonly IComboService _comboService;
    private readonly IServiceProvider _services;

    private OrderDto? _order;
    private ProductDto? _selectedProduct;
    private OrderItemDto? _selectedOrderItem;
    private CustomerDto? _selectedCustomer;
    private int _quantity = 1;
    private FormaPagamento _selectedFormaPagamento = FormaPagamento.Dinheiro;
    private decimal _valorPago;
    private int _parcelas = 1;
    private string _paymentObservacoes = string.Empty;
    private int? _selectedCategoryId;
    private bool _isLoading;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _wasSaved;

    private ObservableCollection<ComboDto> _suggestedCombos = [];

    public event Action? RequestClose;
    public event PropertyChangedEventHandler? PropertyChanged;

    public OrderFormViewModel(
        IOrderService orderService,
        IProductService productService,
        ICustomerService customerService,
        ICategoryService categoryService,
        IComboService comboService,
        IServiceProvider services)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
        _comboService = comboService ?? throw new ArgumentNullException(nameof(comboService));
        _services = services ?? throw new ArgumentNullException(nameof(services));

        // Initialize commands
        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        AddItemCommand = new RelayCommand(async p => await AddItemAsync(p), _ => CanAddItems && !IsLoading);
        RemoveItemCommand = new RelayCommand(async i => await RemoveItemAsync(i), _ => CanAddItems && !IsLoading);
        AddComboCommand = new RelayCommand(async id => await AddComboAsync(id), _ => CanAddItems && !IsLoading);
        RecalculateCommand = new RelayCommand(async _ => await RecalculateAsync(), _ => CanAddItems && !IsLoading);
        StartPreparationCommand = new RelayCommand(async _ => await StartPreparationAsync(), _ => CanStartPreparation && !IsLoading);
        MarkAsReadyCommand = new RelayCommand(async _ => await MarkAsReadyAsync(), _ => CanMarkAsReady && !IsLoading);
        FinalizeOrderCommand = new RelayCommand(async _ => await FinalizeOrderAsync(), _ => CanFinalizeOrder && !IsLoading);
        CloseOrderCommand = new RelayCommand(async _ => await CloseOrderAsync(), _ => CanAddItems && !IsLoading);
        CancelOrderCommand = new RelayCommand(async _ => await CancelOrderAsync(), _ => CanCancelOrder && !IsLoading);
        CloseWithPaymentCommand = new RelayCommand(async _ => await CloseWithPaymentAsync(), _ => CanCloseWithPayment && !IsLoading);
        ClearFormCommand = new RelayCommand(_ => ClearForm());
    }

    public ICommand LoadCommand { get; }
    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }
    public ICommand AddComboCommand { get; }
    public ICommand RecalculateCommand { get; }
    public ICommand StartPreparationCommand { get; }
    public ICommand MarkAsReadyCommand { get; }
    public ICommand FinalizeOrderCommand { get; }
    public ICommand CloseOrderCommand { get; }
    public ICommand CancelOrderCommand { get; }
    public ICommand CloseWithPaymentCommand { get; }
    public ICommand ClearFormCommand { get; }

    public ObservableCollection<OrderItemDto> OrderItems { get; } = [];
    public ObservableCollection<CustomerDto> Customers { get; } = [];
    public ObservableCollection<ProductDto> Products { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];
    public ObservableCollection<FormaPagamento> FormasPagamento { get; } = new(Enum.GetValues<FormaPagamento>());
    public ObservableCollection<PromotionDto> AppliedPromotions { get; } = [];
    public ObservableCollection<ComboDto> SuggestedCombos => _suggestedCombos;

    public OrderDto? Order => _order;

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set => SetProperty(ref _selectedProduct, value);
    }

    public OrderItemDto? SelectedOrderItem
    {
        get => _selectedOrderItem;
        set
        {
            if (SetProperty(ref _selectedOrderItem, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public CustomerDto? SelectedCustomer
    {
        get => _selectedCustomer;
        set => SetProperty(ref _selectedCustomer, value);
    }

    public int Quantity
    {
        get => _quantity;
        set => SetProperty(ref _quantity, Math.Max(1, value));
    }

    public FormaPagamento SelectedFormaPagamento
    {
        get => _selectedFormaPagamento;
        set
        {
            if (SetProperty(ref _selectedFormaPagamento, value))
            {
                UpdatePaymentVisibility();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public decimal ValorPago
    {
        get => _valorPago;
        set
        {
            if (SetProperty(ref _valorPago, value))
            {
                UpdateTrocoAndParcela();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public int Parcelas
    {
        get => _parcelas;
        set
        {
            if (SetProperty(ref _parcelas, Math.Clamp(value, 1, 12)))
            {
                UpdateTrocoAndParcela();
            }
        }
    }

    public string PaymentObservacoes
    {
        get => _paymentObservacoes;
        set => SetProperty(ref _paymentObservacoes, value);
    }

    public int? SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (SetProperty(ref _selectedCategoryId, value))
            {
                OnPropertyChanged(nameof(FilteredProducts));
            }
        }
    }

    public ObservableCollection<ProductDto> FilteredProducts
    {
        get
        {
            var query = Products.AsEnumerable();

            if (SelectedCategoryId.HasValue && SelectedCategoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == SelectedCategoryId.Value);
            }

            return new ObservableCollection<ProductDto>(query.Where(p => p.IsActive));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public string? SuccessMessage
    {
        get => _successMessage;
        private set => SetProperty(ref _successMessage, value);
    }

    public bool WasSaved
    {
        get => _wasSaved;
        private set => SetProperty(ref _wasSaved, value);
    }

    public decimal TotalAmount => _order?.TotalAmount ?? 0;

    public string Status => _order?.Status.ToString() ?? "Novo";

    public bool CanAddItems => _order is not null && _order.Status == OrderStatus.Pendente;

    public bool CanStartPreparation => _order is not null && _order.Status == OrderStatus.Pendente && OrderItems.Count > 0;

    public bool CanMarkAsReady => _order is not null && _order.Status == OrderStatus.EmPreparacao;

    public bool CanFinalizeOrder => _order is not null && _order.Status == OrderStatus.Pronto;

    public bool CanCancelOrder => _order is not null &&
        (_order.Status == OrderStatus.Pendente || _order.Status == OrderStatus.EmPreparacao || _order.Status == OrderStatus.Pronto);

    public bool ShowParcelas => _order is not null && _order.Status == OrderStatus.Pendente && SelectedFormaPagamento == FormaPagamento.Cartao;

    public bool ShowTroco => _order is not null && _order.Status == OrderStatus.Pendente && SelectedFormaPagamento == FormaPagamento.Dinheiro;

    public bool ShowCustomerSelection => _order is not null && _order.Status == OrderStatus.Pendente && SelectedFormaPagamento == FormaPagamento.Fiado;

    public decimal Troco => ShowTroco ? Math.Max(0, ValorPago - TotalAmount) : 0;

    public decimal ValorParcela => ShowParcelas && Parcelas > 0 ? Math.Round(TotalAmount / Parcelas, 2) : 0;

    public bool CanCloseWithPayment =>
        _order is not null &&
        _order.Status == OrderStatus.Pendente &&
        OrderItems.Count > 0 &&
        ((ShowCustomerSelection && SelectedCustomer is not null) || !ShowCustomerSelection);

    public async Task InitializeAsync(OrderDto? existingOrder)
    {
        WasSaved = false;
        ClearMessages();
        OrderItems.Clear();
        AppliedPromotions.Clear();
        _suggestedCombos.Clear();
        Products.Clear();
        Categories.Clear();

        // Load customers
        var customers = await _customerService.GetAllAsync(true);
        foreach (var c in customers)
        {
            Customers.Add(c);
        }

        // Load categories
        var categories = await _categoryService.GetAllAsync(true);
        Categories.Add(new CategoryDto { Id = 0, Name = "Todos" }); // "Todos" option
        foreach (var c in categories)
        {
            Categories.Add(c);
        }
        SelectedCategoryId = 0; // Default to "Todos"

        // Load products
        await LoadProductsAsync();

        if (existingOrder is not null)
        {
            _order = await _orderService.GetByIdAsync(existingOrder.Id);
            if (_order is not null)
            {
                foreach (var item in _order.Items)
                {
                    OrderItems.Add(item);
                }

                // Load applied promotions
                var promotions = await _orderService.GetAppliedPromotionsAsync(_order.Id);
                foreach (var p in promotions)
                {
                    AppliedPromotions.Add(p);
                }

                // Load suggested combos
                await LoadSuggestedCombosAsync();
            }
        }
        else
        {
            // Create new order
            _order = await _orderService.CreateAsync(new CreateOrderRequest { CustomerId = null });
            if (_order is not null)
            {
                OnPropertyChanged(nameof(Order));
                OnPropertyChanged(nameof(TotalAmount));
                OnPropertyChanged(nameof(Status));
            }
        }

        UpdatePaymentVisibility();
        UpdateCommandStates();
    }

    private async Task LoadAsync()
    {
        // Called from LoadCommand - if order exists, reload it
        if (_order is not null)
        {
            await InitializeAsync(_order);
        }
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            var products = await _productService.GetAllAsync(true);
            Products.Clear();
            foreach (var p in products)
            {
                Products.Add(p);
            }
            OnPropertyChanged(nameof(FilteredProducts));
        }
        catch
        {
            ErrorMessage = "Não foi possível carregar os produtos.";
        }
    }

    private async Task LoadSuggestedCombosAsync()
    {
        if (_order is null) return;

        try
        {
            var combos = await _comboService.GetAllAsync(true);
            _suggestedCombos.Clear();
            foreach (var combo in combos.Where(c => c.Items.Count > 0))
            {
                _suggestedCombos.Add(combo);
            }
            OnPropertyChanged(nameof(SuggestedCombos));
        }
        catch
        {
            // Ignore errors loading combos
        }
    }

    private async Task AddItemAsync(object? parameter)
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        var product = parameter as ProductDto ?? SelectedProduct;
        if (product is null) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            var request = new AddOrderItemRequest { OrderId = _order.Id, ProductId = product.Id, Quantity = Quantity };
            _order = await _orderService.AddItemAsync(request);

            // Refresh items
            OrderItems.Clear();
            foreach (var item in _order.Items)
            {
                OrderItems.Add(item);
            }

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            // Check for suggested combos after adding item
            await LoadSuggestedCombosAsync();

            // Auto-recalculate with promotions
            await RecalculateAsync();

            SelectedProduct = null;
            Quantity = 1;

            SuccessMessage = "Item adicionado ao pedido.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RemoveItemAsync(object? parameter)
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        var item = parameter as OrderItemDto ?? SelectedOrderItem;
        if (item is null) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            _order = await _orderService.RemoveItemAsync(_order.Id, item.Id);

            OrderItems.Clear();
            foreach (var oi in _order.Items)
            {
                OrderItems.Add(oi);
            }

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            SuccessMessage = "Item removido do pedido.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task AddComboAsync(object? parameter)
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        if (parameter is not int comboId) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            var combo = await _comboService.GetByIdAsync(comboId);
            if (combo is null)
            {
                ErrorMessage = "Combo não encontrado.";
                return;
            }

            foreach (var comboItem in combo.Items)
            {
                var request = new AddOrderItemRequest { OrderId = _order.Id, ProductId = comboItem.ProductId, Quantity = comboItem.Quantity };
                _order = await _orderService.AddItemAsync(request);
            }

            OrderItems.Clear();
            foreach (var item in _order.Items)
            {
                OrderItems.Add(item);
            }

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            await RecalculateAsync();

            SuccessMessage = $"Combo '{combo.Name}' adicionado ao pedido.";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task RecalculateAsync()
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            _order = await _orderService.RecalculateWithPromotionsAsync(_order.Id);

            OrderItems.Clear();
            foreach (var item in _order.Items)
            {
                OrderItems.Add(item);
            }

            AppliedPromotions.Clear();
            var promotions = await _orderService.GetAppliedPromotionsAsync(_order.Id);
            foreach (var p in promotions)
            {
                AppliedPromotions.Add(p);
            }

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(ValorParcela));
            OnPropertyChanged(nameof(Troco));
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task StartPreparationAsync()
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            _order = await _orderService.StartPreparationAsync(_order.Id);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            SuccessMessage = "Preparação iniciada.";
            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task MarkAsReadyAsync()
    {
        if (_order is null || _order.Status != OrderStatus.EmPreparacao || IsLoading) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            _order = await _orderService.MarkAsReadyAsync(_order.Id);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            SuccessMessage = "Pedido marcado como pronto.";
            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task FinalizeOrderAsync()
    {
        if (_order is null || _order.Status != OrderStatus.Pronto || IsLoading) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            _order = await _orderService.FinalizeAsync(_order.Id);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            SuccessMessage = "Pedido finalizado com sucesso.";
            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CloseOrderAsync()
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            var request = new CloseOrderRequest { OrderId = _order.Id };
            _order = await _orderService.CloseAsync(request);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            SuccessMessage = "Pedido fechado com sucesso.";
            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CancelOrderAsync()
    {
        if (_order is null || CanCancelOrder == false || IsLoading) return;

        var result = MessageBox.Show(
            "Tem certeza que deseja cancelar este pedido?",
            "Confirmar cancelamento",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        try
        {
            IsLoading = true;
            ClearMessages();

            var request = new CancelOrderRequest { OrderId = _order.Id };
            _order = await _orderService.CancelAsync(request);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            SuccessMessage = "Pedido cancelado.";
            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task CloseWithPaymentAsync()
    {
        if (_order is null || _order.Status != OrderStatus.Pendente || IsLoading) return;

        if (ShowCustomerSelection && SelectedCustomer is null)
        {
            ErrorMessage = "Selecione um cliente para pagamento a prazo (Fiado).";
            return;
        }

        if (ValorPago < TotalAmount && SelectedFormaPagamento != FormaPagamento.Fiado)
        {
            var result = MessageBox.Show(
                $"Valor pago (R$ {ValorPago:N2}) é menor que o total (R$ {TotalAmount:N2}). Deseja continuar?",
                "Valor insuficiente",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;
        }

        try
        {
            IsLoading = true;
            ClearMessages();

            var request = new CloseOrderWithPaymentRequest(
                _order.Id,
                SelectedFormaPagamento,
                ValorPago,
                Parcelas,
                PaymentObservacoes,
                ShowCustomerSelection ? SelectedCustomer!.Id : null);

            var result = await _orderService.CloseWithPaymentAsync(request);

            if (!result.Success)
            {
                ErrorMessage = result.ErrorMessage ?? "Erro ao fechar pedido com pagamento.";
                return;
            }

            _order = await _orderService.GetByIdAsync(_order.Id);

            OnPropertyChanged(nameof(TotalAmount));
            OnPropertyChanged(nameof(Status));
            OnPropertyChanged(nameof(CanAddItems));
            OnPropertyChanged(nameof(CanCloseWithPayment));
            UpdateCommandStates();

            if (result.PendingPayment is not null)
            {
                SuccessMessage = $"Pedido fechado! Pendência (Fiado) criada: R$ {result.PendingPayment.SaldoPendente:N2}";
            }
            else
            {
                SuccessMessage = "Pedido fechado e pago com sucesso.";
            }

            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ClearForm()
    {
        SelectedProduct = null;
        Quantity = 1;
        SelectedCustomer = null;
        SelectedFormaPagamento = FormaPagamento.Dinheiro;
        ValorPago = 0;
        Parcelas = 1;
        PaymentObservacoes = string.Empty;
        ClearMessages();
    }

    private void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    private void UpdatePaymentVisibility()
    {
        OnPropertyChanged(nameof(ShowParcelas));
        OnPropertyChanged(nameof(ShowTroco));
        OnPropertyChanged(nameof(ShowCustomerSelection));
        OnPropertyChanged(nameof(Troco));
        OnPropertyChanged(nameof(ValorParcela));
        OnPropertyChanged(nameof(CanCloseWithPayment));
    }

    private void UpdateTrocoAndParcela()
    {
        OnPropertyChanged(nameof(Troco));
        OnPropertyChanged(nameof(ValorParcela));
        OnPropertyChanged(nameof(CanCloseWithPayment));
    }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(CanAddItems));
        OnPropertyChanged(nameof(CanStartPreparation));
        OnPropertyChanged(nameof(CanMarkAsReady));
        OnPropertyChanged(nameof(CanFinalizeOrder));
        OnPropertyChanged(nameof(CanCancelOrder));
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}