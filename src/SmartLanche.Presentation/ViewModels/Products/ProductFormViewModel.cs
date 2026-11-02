using System.Collections.ObjectModel;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Products;

public class ProductFormViewModel : FormViewModelBase<ProductDto, CreateProductRequest, UpdateProductRequest, IProductService>
{
    private readonly ICategoryService _categoryService;

    private string _name = string.Empty;
    private CategoryDto? _selectedCategory;
    private decimal _price;
    private bool _isCombo;
    private string? _description;
    private bool _isActive = true;
    private bool _isLoadingCategories;

    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public ProductFormViewModel(
        IProductService productService,
        ICategoryService categoryService,
        IToastNotificationService toastService)
        : base(productService, toastService)
    {
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public CategoryDto? SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    public decimal Price
    {
        get => _price;
        set => SetProperty(ref _price, value);
    }

    public bool IsCombo
    {
        get => _isCombo;
        set => SetProperty(ref _isCombo, value);
    }

    public string? Description
    {
        get => _description;
        set => SetProperty(ref _description, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
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

    protected override async Task OnInitializeAsync(ProductDto? product)
    {
        Name = product?.Name ?? string.Empty;
        Price = product?.Price ?? 0m;
        IsCombo = product?.IsCombo ?? false;
        Description = product?.Description;
        IsActive = product?.IsActive ?? true;

        SelectedCategory = null;

        await LoadCategoriesAsync();

        if (product is not null)
        {
            SelectedCategory = Categories.FirstOrDefault(category => category.Id == product.CategoryId);
        }
    }

    private async Task LoadCategoriesAsync()
    {
        try
        {
            IsLoadingCategories = true;
            ErrorMessage = null;

            // New products can only use active categories.
            // During editing, load all categories to preserve
            // the current association if its category is inactive.
            bool? isActive = IsEditMode ? null : true;

            var categories = await _categoryService.GetAllAsync(isActive);

            Categories.Clear();

            foreach (var category in categories)
            {
                Categories.Add(category);
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

    protected override CreateProductRequest BuildCreateRequest()
    {
        return new CreateProductRequest
        {
            Name = NormalizeStringRequired(Name),
            CategoryId = SelectedCategory!.Id,
            Price = Price,
            IsCombo = IsCombo,
            Description = NormalizeString(Description)
        };
    }

    protected override UpdateProductRequest BuildUpdateRequest(int entityId)
    {
        return new UpdateProductRequest
        {
            Id = entityId,
            Name = NormalizeStringRequired(Name),
            CategoryId = SelectedCategory!.Id,
            Price = Price,
            IsCombo = IsCombo,
            Description = NormalizeString(Description)
        };
    }

    protected override Task CallCreateAsync(CreateProductRequest request)
    {
        return Service.CreateAsync(request);
    }

    protected override Task CallUpdateAsync(UpdateProductRequest request)
    {
        return Service.UpdateAsync(request);
    }

    protected override string GetCreateSuccessMessage() => "Produto cadastrado com sucesso.";

    protected override string GetUpdateSuccessMessage() => "Produto atualizado com sucesso.";

    protected override bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ToastService.ShowWarning("Informe o nome do produto.");
            return false;
        }

        if (Name.Trim().Length > 150)
        {
            ToastService.ShowWarning("O nome do produto deve ter no máximo 150 caracteres.");
            return false;
        }

        if (SelectedCategory is null)
        {
            ToastService.ShowWarning("Selecione uma categoria.");
            return false;
        }

        if (Price < 0)
        {
            ToastService.ShowWarning("O preço não pode ser negativo.");
            return false;
        }

        if (Description?.Length > 500)
        {
            ToastService.ShowWarning("A descrição deve ter no máximo 500 caracteres.");
            return false;
        }

        if (!IsEditMode && !SelectedCategory.IsActive)
        {
            ToastService.ShowWarning("Selecione uma categoria ativa.");
            return false;
        }

        return true;
    }
}