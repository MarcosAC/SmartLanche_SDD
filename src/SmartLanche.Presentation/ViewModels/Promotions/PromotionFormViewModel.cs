using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Promotions;

public class PromotionFormViewModel : FormViewModelBase<PromotionDto, CreatePromotionRequest, UpdatePromotionRequest, IPromotionService>
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    private string _name = string.Empty;
    private string? _description;
    private DiscountType _discountType = DiscountType.Percentage;
    private decimal _discountValue;
    private DateTime _startDate = DateTime.Today;
    private DateTime? _endDate = DateTime.Today.AddDays(30);
    private int _priority = 1;
    private bool _isLoadingProducts;
    private bool _isLoadingCategories;

    public PromotionFormViewModel(
        IPromotionService promotionService,
        IProductService productService,
        ICategoryService categoryService,
        IToastNotificationService toastService)
        : base(promotionService, toastService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    public ObservableCollection<SelectableProductViewModel> Products { get; } = [];
    public ObservableCollection<SelectableCategoryViewModel> Categories { get; } = [];

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

    public DiscountType DiscountType
    {
        get => _discountType;
        set => SetProperty(ref _discountType, value);
    }

    public decimal DiscountValue
    {
        get => _discountValue;
        set => SetProperty(ref _discountValue, value);
    }

    public DateTime StartDate
    {
        get => _startDate;
        set => SetProperty(ref _startDate, value);
    }

    public DateTime? EndDate
    {
        get => _endDate;
        set => SetProperty(ref _endDate, value);
    }

    public int Priority
    {
        get => _priority;
        set => SetProperty(ref _priority, value);
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

    public bool IsLoadingCategories
    {
        get => _isLoadingCategories;
        private set
        {
            if (SetProperty(ref _isLoadingCategories, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsGlobal => !Products.Any(p => p.IsSelected) && !Categories.Any(c => c.IsSelected);

    public DiscountType[] DiscountTypes => Enum.GetValues<DiscountType>();

    protected override async Task OnInitializeAsync(PromotionDto? promotion)
    {
        Name = promotion?.Name ?? string.Empty;
        Description = promotion?.Description;
        DiscountType = promotion?.DiscountType ?? DiscountType.Percentage;
        DiscountValue = promotion?.DiscountValue ?? 0m;
        StartDate = promotion?.StartDate ?? DateTime.Today;
        EndDate = promotion?.EndDate ?? DateTime.Today.AddDays(30);
        Priority = promotion?.Priority ?? 1;

        WasSaved = false;

        Products.Clear();
        Categories.Clear();

        await LoadProductsAsync();
        await LoadCategoriesAsync();

        if (promotion is not null)
        {
            foreach (var productId in promotion.ApplicableProductIds)
            {
                var product = Products.FirstOrDefault(p => p.Id == productId);
                if (product is not null)
                {
                    product.IsSelected = true;
                }
            }

            foreach (var categoryId in promotion.ApplicableCategoryIds)
            {
                var category = Categories.FirstOrDefault(c => c.Id == categoryId);
                if (category is not null)
                {
                    category.IsSelected = true;
                }
            }
        }

        RaisePropertyChanged(nameof(IsGlobal));
    }

    private async Task LoadProductsAsync()
    {
        try
        {
            IsLoadingProducts = true;

            bool? isActive = IsEditMode ? null : true;
            var products = await _productService.GetAllAsync(isActive);

            Products.Clear();

            foreach (var product in products)
            {
                Products.Add(new SelectableProductViewModel
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    IsActive = product.IsActive,
                    IsSelected = false
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

    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsLoadingCategories = true;

            bool? isActive = IsEditMode ? null : true;
            var categories = await _categoryService.GetAllAsync(isActive);

            Categories.Clear();

            foreach (var category in categories)
            {
                Categories.Add(new SelectableCategoryViewModel
                {
                    Id = category.Id,
                    Name = category.Name,
                    IsActive = category.IsActive,
                    IsSelected = false
                });
            }
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Não foi possível carregar as categorias: {ex.Message}");
        }
        finally
        {
            IsLoadingCategories = false;
        }
    }

    protected override CreatePromotionRequest BuildCreateRequest()
    {
        var applicableProductIds = Products.Where(p => p.IsSelected).Select(p => p.Id).ToList();
        var applicableCategoryIds = Categories.Where(c => c.IsSelected).Select(c => c.Id).ToList();

        return new CreatePromotionRequest
        {
            Name = NormalizeStringRequired(Name),
            Description = NormalizeString(Description),
            DiscountType = DiscountType,
            DiscountValue = DiscountValue,
            StartDate = StartDate.Date,
            EndDate = EndDate?.Date,
            Priority = Priority,
            ApplicableProductIds = applicableProductIds,
            ApplicableCategoryIds = applicableCategoryIds
        };
    }

    protected override UpdatePromotionRequest BuildUpdateRequest(int entityId)
    {
        var applicableProductIds = Products.Where(p => p.IsSelected).Select(p => p.Id).ToList();
        var applicableCategoryIds = Categories.Where(c => c.IsSelected).Select(c => c.Id).ToList();

        return new UpdatePromotionRequest
        {
            Id = entityId,
            Name = NormalizeStringRequired(Name),
            Description = NormalizeString(Description),
            DiscountType = DiscountType,
            DiscountValue = DiscountValue,
            StartDate = StartDate.Date,
            EndDate = EndDate?.Date,
            IsActive = true,
            Priority = Priority,
            ApplicableProductIds = applicableProductIds,
            ApplicableCategoryIds = applicableCategoryIds
        };
    }

    protected override Task CallCreateAsync(CreatePromotionRequest request)
    {
        return Service.CreateAsync(request);
    }

    protected override Task CallUpdateAsync(UpdatePromotionRequest request)
    {
        return Service.UpdateAsync(request);
    }

    protected override string GetCreateSuccessMessage() => "Promoção cadastrada com sucesso.";

    protected override string GetUpdateSuccessMessage() => "Promoção atualizada com sucesso.";

    protected override bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ToastService.ShowWarning("Informe o nome da promoção.");
            return false;
        }

        if (Name.Trim().Length > 150)
        {
            ToastService.ShowWarning("O nome da promoção deve ter no máximo 150 caracteres.");
            return false;
        }

        if (DiscountValue <= 0)
        {
            ToastService.ShowWarning("O valor do desconto deve ser maior que zero.");
            return false;
        }

        if (DiscountType == DiscountType.Percentage && DiscountValue > 100)
        {
            ToastService.ShowWarning("O desconto percentual não pode ser maior que 100%.");
            return false;
        }

        if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
        {
            ToastService.ShowWarning("A data de fim deve ser posterior à data de início.");
            return false;
        }

        if (Priority < 0)
        {
            ToastService.ShowWarning("A prioridade não pode ser negativa.");
            return false;
        }

        if (Description?.Length > 500)
        {
            ToastService.ShowWarning("A descrição deve ter no máximo 500 caracteres.");
            return false;
        }

        // For new promotions, ensure selected products/categories are active
        if (!IsEditMode)
        {
            var inactiveProducts = Products.Where(p => p.IsSelected && !p.IsActive).ToList();
            if (inactiveProducts.Any())
            {
                ToastService.ShowWarning("Selecione apenas produtos ativos.");
                return false;
            }

            var inactiveCategories = Categories.Where(c => c.IsSelected && !c.IsActive).ToList();
            if (inactiveCategories.Any())
            {
                ToastService.ShowWarning("Selecione apenas categorias ativas.");
                return false;
            }
        }

        return true;
    }

    public class SelectableProductViewModel : ViewModelBase
    {
        private bool _isSelected;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public bool IsActive { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    RaisePropertyChanged(nameof(IsSelected));
                }
            }
        }
    }

    public class SelectableCategoryViewModel : ViewModelBase
    {
        private bool _isSelected;

        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (SetProperty(ref _isSelected, value))
                {
                    RaisePropertyChanged(nameof(IsSelected));
                }
            }
        }
    }
}