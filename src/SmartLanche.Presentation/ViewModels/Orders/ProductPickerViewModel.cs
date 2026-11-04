using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;

namespace SmartLanche.Presentation.ViewModels.Orders;

public sealed class ProductPickerViewModel : INotifyPropertyChanged
{
    private readonly IProductService _productService;
    private readonly ICategoryService _categoryService;

    private string _searchText = string.Empty;
    private int? _selectedCategoryId;
    private ProductDto? _selectedProduct;
    private bool _isLoading;
    private string? _errorMessage;

    public event Action? ConfirmCommand;
    public event Action? CancelCommand;
    public event PropertyChangedEventHandler? PropertyChanged;

    public ProductPickerViewModel(IProductService productService, ICategoryService categoryService)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        ConfirmCommandAction = new RelayCommand(_ =>
        {
            if (SelectedProduct is not null) ConfirmCommand?.Invoke();
        }, _ => SelectedProduct is not null && !IsLoading);

        CancelCommandAction = new RelayCommand(_ => CancelCommand?.Invoke());
    }

    public ICommand LoadCommand { get; }
    public ICommand ConfirmCommandAction { get; }
    public ICommand CancelCommandAction { get; }

    public ObservableCollection<ProductDto> AllProducts { get; } = [];
    public ObservableCollection<CategoryDto> Categories { get; } = [];

    public ObservableCollection<ProductDto> FilteredProducts
    {
        get
        {
            var query = AllProducts.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var term = SearchText.ToLowerInvariant();
                query = query.Where(p => p.Name.ToLowerInvariant().Contains(term));
            }

            if (SelectedCategoryId.HasValue && SelectedCategoryId.Value > 0)
            {
                query = query.Where(p => p.CategoryId == SelectedCategoryId.Value);
            }

            return new ObservableCollection<ProductDto>(query.Where(p => p.IsActive));
        }
    }

    public ProductDto? SelectedProduct
    {
        get => _selectedProduct;
        set
        {
            if (SetProperty(ref _selectedProduct, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
            {
                OnPropertyChanged(nameof(FilteredProducts));
            }
        }
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

    public async Task LoadAsync()
    {
        if (IsLoading) return;

        try
        {
            IsLoading = true;
            ErrorMessage = null;

            var products = await _productService.GetAllAsync(true);
            AllProducts.Clear();
            foreach (var p in products)
            {
                AllProducts.Add(p);
            }

            var categories = await _categoryService.GetAllAsync(true);
            Categories.Clear();
            Categories.Add(new CategoryDto { Id = 0, Name = "Todos" }); // "Todos" option
            foreach (var c in categories)
            {
                Categories.Add(c);
            }
            SelectedCategoryId = 0; // Default to "Todos"

            OnPropertyChanged(nameof(FilteredProducts));
        }
        catch (Exception)
        {
            ErrorMessage = "Não foi possível carregar os produtos.";
        }
        finally
        {
            IsLoading = false;
        }
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