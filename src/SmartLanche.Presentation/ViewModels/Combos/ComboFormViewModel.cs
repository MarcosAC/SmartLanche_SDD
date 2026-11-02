using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Combos;

public class ComboFormViewModel : FormViewModelBase<ComboDto, CreateComboRequest, UpdateComboRequest, IComboService>
{
    private readonly IProductService _productService;

    private string _name = string.Empty;
    private string? _description;
    private decimal _price;
    private bool _isLoadingProducts;

    public ComboFormViewModel(
        IComboService comboService,
        IProductService productService,
        IToastNotificationService toastService)
        : base(comboService, toastService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));

        AddItemCommand = new RelayCommand(_ => AddItem(), _ => !IsSaving);
        RemoveItemCommand = new RelayCommand(param => RemoveItem(param as ComboItemViewModel), _ => !IsSaving);
    }

    public ObservableCollection<ComboItemViewModel> Items { get; } = [];
    public ObservableCollection<SelectableProductViewModel> AvailableProducts { get; } = [];

    public ICommand AddItemCommand { get; }
    public ICommand RemoveItemCommand { get; }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public bool IsLoadingProducts
    {
        get => _isLoadingProducts;
        private set
        {
            if (SetProperty(ref _isLoadingProducts, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool HasMinimumItems => Items.Count >= 2;

    public decimal ItemsTotal => Items.Where(i => i.ProductId > 0).Sum(i => i.Subtotal);

    public decimal DiscountAmount => ItemsTotal - Price;

    protected override async Task OnInitializeAsync(ComboDto? combo)
    {
        Name = combo?.Name ?? string.Empty;
        Description = combo?.Description;
        Price = combo?.Price ?? 0m;

        WasSaved = false;

        Items.Clear();
        AvailableProducts.Clear();

        await LoadProductsAsync();

        if (combo is not null)
        {
            foreach (var item in combo.Items)
            {
                var product = AvailableProducts.FirstOrDefault(p => p.Id == item.ProductId);
                if (product is not null)
                {
                    var newItem = new ComboItemViewModel
                    {
                        ProductId = item.ProductId,
                        ProductName = product.Name,
                        Quantity = item.Quantity,
                        ProductPrice = product.Price
                    };

                    newItem.PropertyChanged += (_, e) =>
                    {
                        if (e.PropertyName == nameof(ComboItemViewModel.ProductId))
                        {
                            var p = AvailableProducts.FirstOrDefault(ap => ap.Id == newItem.ProductId);
                            if (p is not null)
                            {
                                newItem.ProductName = p.Name;
                                newItem.ProductPrice = p.Price;
                            }
                            else
                            {
                                newItem.ProductName = "";
                                newItem.ProductPrice = 0;
                            }
                        }

                        RaisePropertyChanged(nameof(ItemsTotal));
                        RaisePropertyChanged(nameof(DiscountAmount));
                        RaisePropertyChanged(nameof(HasMinimumItems));
                    };

                    Items.Add(newItem);
                }
            }
        }

        RaisePropertyChanged(nameof(HasMinimumItems));
        RaisePropertyChanged(nameof(ItemsTotal));
        RaisePropertyChanged(nameof(DiscountAmount));
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            IsLoadingProducts = true;

            bool? isActive = IsEditMode ? null : true;
            var products = await _productService.GetAllAsync(isActive);

            AvailableProducts.Clear();

            foreach (var product in products)
            {
                AvailableProducts.Add(new SelectableProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    IsActive = product.IsActive
                });
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Não foi possível carregar os produtos: {ex.Message}");
        }
        finally
        {
            IsLoadingProducts = false;
        }
    }

    private void AddItem()
    {
        var newItem = new ComboItemViewModel();

        newItem.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(ComboItemViewModel.ProductId))
            {
                var product = AvailableProducts.FirstOrDefault(p => p.Id == newItem.ProductId);
                if (product is not null)
                {
                    newItem.ProductName = product.Name;
                    newItem.ProductPrice = product.Price;
                }
                else
                {
                    newItem.ProductName = "";
                    newItem.ProductPrice = 0;
                }
            }

            RaisePropertyChanged(nameof(ItemsTotal));
            RaisePropertyChanged(nameof(DiscountAmount));
            RaisePropertyChanged(nameof(HasMinimumItems));
        };

        Items.Add(newItem);

        RaisePropertyChanged(nameof(ItemsTotal));
        RaisePropertyChanged(nameof(DiscountAmount));
        RaisePropertyChanged(nameof(HasMinimumItems));
    }

    private void RemoveItem(ComboItemViewModel? item)
    {
        if (item is not null)
        {
            Items.Remove(item);
            RaisePropertyChanged(nameof(HasMinimumItems));
            RaisePropertyChanged(nameof(ItemsTotal));
            RaisePropertyChanged(nameof(DiscountAmount));
        }
    }

    protected override CreateComboRequest BuildCreateRequest()
    {
        var items = Items
            .Where(i => i.ProductId > 0)
            .Select(i => new CreateComboItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            })
            .ToList();

        return new CreateComboRequest
        {
            Name = NormalizeStringRequired(Name),
            Description = NormalizeString(Description),
            Price = Price,
            Items = items
        };
    }

    protected override UpdateComboRequest BuildUpdateRequest(int entityId)
    {
        var items = Items
            .Where(i => i.ProductId > 0)
            .Select(i => new CreateComboItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            })
            .ToList();

        return new UpdateComboRequest
        {
            Id = entityId,
            Name = NormalizeStringRequired(Name),
            Description = NormalizeString(Description),
            Price = Price,
            IsActive = true,
            Items = items
        };
    }

    protected override Task CallCreateAsync(CreateComboRequest request)
    {
        return Service.CreateAsync(request);
    }

    protected override Task CallUpdateAsync(UpdateComboRequest request)
    {
        return Service.UpdateAsync(request);
    }

    protected override string GetCreateSuccessMessage() => "Combo cadastrado com sucesso.";

    protected override string GetUpdateSuccessMessage() => "Combo atualizado com sucesso.";

    protected override bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ToastService.ShowWarning("Informe o nome do combo.");
            return false;
        }

        if (Name.Trim().Length > 150)
        {
            ToastService.ShowWarning("O nome do combo deve ter no máximo 150 caracteres.");
            return false;
        }

        if (Price <= 0)
        {
            ToastService.ShowWarning("O preço do combo deve ser maior que zero.");
            return false;
        }

        var validItems = Items.Where(i => i.ProductId > 0).ToList();

        if (validItems.Count < 2)
        {
            ToastService.ShowWarning("O combo deve ter pelo menos 2 itens.");
            return false;
        }

        var duplicateProducts = validItems
            .GroupBy(i => i.ProductId)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateProducts.Any())
        {
            ToastService.ShowWarning("Não é permitido adicionar o mesmo produto mais de uma vez no combo.");
            return false;
        }

        var invalidQuantities = validItems.Where(i => i.Quantity <= 0).ToList();
        if (invalidQuantities.Any())
        {
            ToastService.ShowWarning("A quantidade de cada item deve ser maior que zero.");
            return false;
        }

        // For new combos, ensure selected products are active
        if (!IsEditMode)
        {
            var inactiveProducts = AvailableProducts
                .Where(p => validItems.Any(i => i.ProductId == p.Id) && !p.IsActive)
                .ToList();

            if (inactiveProducts.Any())
            {
                ToastService.ShowWarning("Selecione apenas produtos ativos.");
                return false;
            }
        }

        if (Description?.Length > 500)
        {
            ToastService.ShowWarning("A descrição deve ter no máximo 500 caracteres.");
            return false;
        }

        return true;
    }

    public class ComboItemViewModel : ViewModelBase
    {
        private int _productId;
        private string _productName = string.Empty;
        private int _quantity = 1;
        private decimal _productPrice;

        public int ProductId
        {
            get => _productId;
            set
            {
                if (SetProperty(ref _productId, value))
                {
                    RaisePropertyChanged(nameof(ProductId));
                    RaisePropertyChanged(nameof(IsValid));
                }
            }
        }

        public string ProductName
        {
            get => _productName;
            set
            {
                if (SetProperty(ref _productName, value))
                {
                    RaisePropertyChanged(nameof(ProductName));
                }
            }
        }

        public int Quantity
        {
            get => _quantity;
            set
            {
                if (SetProperty(ref _quantity, value))
                {
                    RaisePropertyChanged(nameof(Quantity));
                    RaisePropertyChanged(nameof(Subtotal));
                    RaisePropertyChanged(nameof(IsValid));
                }
            }
        }

        public decimal ProductPrice
        {
            get => _productPrice;
            set
            {
                if (SetProperty(ref _productPrice, value))
                {
                    RaisePropertyChanged(nameof(ProductPrice));
                    RaisePropertyChanged(nameof(Subtotal));
                }
            }
        }

        public decimal Subtotal => ProductPrice * Quantity;

        public bool IsValid => ProductId > 0 && Quantity > 0;
    }

    public class SelectableProductViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }
    }
}