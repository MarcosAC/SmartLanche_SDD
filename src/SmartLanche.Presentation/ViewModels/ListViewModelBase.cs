using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;

namespace SmartLanche.Presentation.ViewModels;

/// <summary>
/// Enhanced base class for list view models with support for complex filtering,
/// custom actions, and flexible service interactions.
/// </summary>
/// <typeparam name="TDto">The DTO type for the entity</typeparam>
/// <typeparam name="TService">The service type</typeparam>
public abstract class ListViewModelBase<TDto, TService> : ViewModelBase
    where TDto : class
    where TService : class
{
    protected readonly TService Service;

    private TDto? _selectedItem;
    private string _searchText = string.Empty;
    private string? _sortColumn;
    private bool _sortDescending;

    public event Action? RequestNewItem;
    public event Action<TDto>? RequestEditItem;

    public ObservableCollection<TDto> Items { get; } = [];

    public ICommand LoadCommand { get; }
    public ICommand NewItemCommand { get; }
    public ICommand EditItemCommand { get; }

    public TDto? SelectedItem
    {
        get => _selectedItem;
        set
        {
            if (SetProperty(ref _selectedItem, value))
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
                OnSearchTextChanged();
            }
        }
    }

    public string? SortColumn
    {
        get => _sortColumn;
        set
        {
            if (SetProperty(ref _sortColumn, value))
            {
                _ = LoadAsync();
            }
        }
    }

    public bool SortDescending
    {
        get => _sortDescending;
        set
        {
            if (SetProperty(ref _sortDescending, value))
            {
                _ = LoadAsync();
            }
        }
    }

    protected ListViewModelBase(TService service)
    {
        Service = service ?? throw new ArgumentNullException(nameof(service));

        LoadCommand = new RelayCommand(async _ => await LoadAsync());
        NewItemCommand = new RelayCommand(_ => { ClearMessages(); RequestNewItem?.Invoke(); });
        EditItemCommand = new RelayCommand(_ =>
        {
            if (SelectedItem is null) return;
            ClearMessages();
            RequestEditItem?.Invoke(SelectedItem!);
        }, _ => SelectedItem is not null && !IsLoading);
    }

    /// <summary>
    /// Called when SearchText changes. Override to implement debounced search or immediate reload.
    /// </summary>
    protected virtual void OnSearchTextChanged()
    {
        _ = LoadAsync();
    }

    /// <summary>
    /// Loads the items. Override to implement custom loading logic.
    /// </summary>
    public virtual async Task LoadAsync()
    {
        if (IsLoading)
            return;

        await ExecuteWithLoadingAsync(async () =>
        {
            var items = await GetItemsAsync();

            // Apply search filter (client-side by default)
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var searchTerm = SearchText.ToLowerInvariant();
                items = ApplySearchFilter(items, searchTerm);
            }

            // Apply sorting
            items = ApplySort(items);

            Items.Clear();
            foreach (var item in items)
            {
                Items.Add(item);
            }
        });
    }

    /// <summary>
    /// Gets the items from the service. Override to implement custom loading logic.
    /// </summary>
    protected virtual Task<IReadOnlyList<TDto>> GetItemsAsync()
    {
        // Default implementation assumes service has GetAllAsync(bool? isActive)
        // Override in derived classes for services with different method signatures
        return Task.FromResult<IReadOnlyList<TDto>>(Array.Empty<TDto>());
    }

    /// <summary>
    /// Applies search filter to items. Override for entity-specific search logic.
    /// </summary>
    protected virtual IReadOnlyList<TDto> ApplySearchFilter(IReadOnlyList<TDto> items, string searchTerm)
    {
        return items;
    }

    /// <summary>
    /// Applies sorting to items. Override for entity-specific sort logic.
    /// </summary>
    protected virtual IReadOnlyList<TDto> ApplySort(IReadOnlyList<TDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.ToList();
        }

        // Derived classes must override to provide actual sorting logic
        return items.ToList();
    }

    /// <summary>
    /// Loads additional data needed by the view (e.g., dropdown options).
    /// Override to load related entities.
    /// </summary>
    protected virtual Task LoadAdditionalDataAsync()
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Specialized base class for list view models that support activate/deactivate operations.
/// </summary>
public abstract class CrudListViewModelBase<TDto, TService> : ListViewModelBase<TDto, TService>
    where TDto : class, IEntityDto
    where TService : class, IEntityService<TDto>
{
    public ICommand DeactivateItemCommand { get; }
    public ICommand ReactivateItemCommand { get; }

    private string? _selectedFilter = "Todos";

    /// <summary>
    /// Gets or sets the selected filter from the status ComboBox.
    /// Values: "Todos", "Ativos", "Inativos"
    /// </summary>
    public string? SelectedFilter
    {
        get => _selectedFilter;
        set
        {
            if (SetProperty(ref _selectedFilter, value))
            {
                _ = LoadAsync(); // Reload when filter changes
            }
        }
    }

    protected CrudListViewModelBase(TService service) : base(service)
    {
        DeactivateItemCommand = new AsyncRelayCommand(
            async _ => await ChangeItemStatusAsync(false),
            _ => SelectedItem is { IsActive: true } && !IsLoading);

        ReactivateItemCommand = new AsyncRelayCommand(
            async _ => await ChangeItemStatusAsync(true),
            _ => SelectedItem is { IsActive: false } && !IsLoading);
    }

    protected override async Task<IReadOnlyList<TDto>> GetItemsAsync()
    {
        bool? isActive = GetActiveFilter();

        var items = await Service.GetAllAsync(isActive);
        return items;
    }

    /// <summary>
    /// Returns the active filter based on SelectedFilter.
    /// Override for custom filter logic.
    /// </summary>
    protected virtual bool? GetActiveFilter()
    {
        return SelectedFilter switch
        {
            "Ativos" => true,
            "Inativos" => false,
            _ => null // "Todos" or null
        };
    }

    protected virtual async Task ChangeItemStatusAsync(bool activate)
    {
        if (SelectedItem is null || IsLoading)
        {
            return;
        }

        await ExecuteWithLoadingAsync(async () =>
        {
            var itemId = SelectedItem.Id;

            if (activate)
            {
                await Service.ReactivateAsync(itemId);
                SuccessMessage = $"{GetEntityName()} reativado com sucesso.";
            }
            else
            {
                await Service.DeactivateAsync(itemId);
                SuccessMessage = $"{GetEntityName()} desativado com sucesso.";
            }

            // Load items directly without ExecuteWithLoadingAsync to avoid nested loading guard
            await LoadItemsAsync();
        });
    }

    /// <summary>
    /// Loads items without the ExecuteWithLoadingAsync wrapper (for nested calls).
    /// </summary>
    protected async Task LoadItemsAsync()
    {
        var items = await GetItemsAsync();

        // Apply search filter (client-side by default)
        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var searchTerm = SearchText.ToLowerInvariant();
            items = ApplySearchFilter(items, searchTerm);
        }

        // Apply sorting
        items = ApplySort(items);

        Items.Clear();
        foreach (var item in items)
        {
            Items.Add(item);
        }
    }

    protected abstract string GetEntityName();
}