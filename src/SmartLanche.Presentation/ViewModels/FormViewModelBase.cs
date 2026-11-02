using System;
using System.Threading.Tasks;
using System.Windows.Input;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Commands;
using SmartLanche.Presentation.Services;

namespace SmartLanche.Presentation.ViewModels;

/// <summary>
/// Base class for all form view models (Create/Edit forms).
/// Provides common functionality: property change notification, save/cancel commands,
/// validation, toast notifications, loading state, and edit/create mode handling.
/// </summary>
/// <typeparam name="TDto">The DTO type for the entity (e.g., CategoryDto, ProductDto)</typeparam>
/// <typeparam name="TCreateRequest">The request type for creating a new entity</typeparam>
/// <typeparam name="TUpdateRequest">The request type for updating an existing entity</typeparam>
/// <typeparam name="TService">The service interface type (e.g., ICategoryService)</typeparam>
public abstract class FormViewModelBase<TDto, TCreateRequest, TUpdateRequest, TService> : ViewModelBase
    where TDto : class, IEntityDto
    where TCreateRequest : class
    where TUpdateRequest : class
    where TService : class
{
    private readonly TService _service;
    private readonly IToastNotificationService _toastService;

    private bool _isSaving;
    private bool _isEditMode;
    private bool _wasSaved;

    /// <summary>
    /// Gets the service instance for CRUD operations.
    /// </summary>
    protected TService Service => _service;

    /// <summary>
    /// Gets the toast notification service.
    /// </summary>
    protected IToastNotificationService ToastService => _toastService;

    protected FormViewModelBase(TService service, IToastNotificationService toastService)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _toastService = toastService ?? throw new ArgumentNullException(nameof(toastService));

        SaveCommand = new RelayCommand(async _ => await SaveAsync(), _ => !IsSaving);
        CancelCommand = new RelayCommand(_ => RequestClose?.Invoke());
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event Action? RequestClose;

    public bool IsSaving
    {
        get => _isSaving;
        protected set
        {
            if (SetProperty(ref _isSaving, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsEditMode
    {
        get => _isEditMode;
        protected set => SetProperty(ref _isEditMode, value);
    }

    public bool WasSaved
    {
        get => _wasSaved;
        protected set => SetProperty(ref _wasSaved, value);
    }

    /// <summary>
    /// The ID of the entity being edited (null for new entities).
    /// </summary>
    protected int? EntityId { get; private set; }

    /// <summary>
    /// Initializes the form for creating a new entity or editing an existing one.
    /// </summary>
    /// <param name="entity">The entity DTO to edit, or null for a new entity.</param>
    public async Task InitializeAsync(TDto? entity = null)
    {
        IsEditMode = entity is not null;
        EntityId = entity?.Id;

        WasSaved = false;
        ClearMessages();

        await OnInitializeAsync(entity);
    }

    /// <summary>
    /// Derived classes override this to perform entity-specific initialization.
    /// Called after base initialization sets IsEditMode and EntityId.
    /// </summary>
    protected abstract Task OnInitializeAsync(TDto? entity);

    /// <summary>
    /// Saves the entity (create or update).
    /// </summary>
    public async Task SaveAsync()
    {
        if (IsSaving)
        {
            return;
        }

        WasSaved = false;
        ClearMessages();

        if (!ValidateForm())
        {
            return;
        }

        try
        {
            IsSaving = true;

            if (IsEditMode)
            {
                if (EntityId is null)
                {
                    ToastService.ShowError("A entidade não foi identificada para edição.");
                    return;
                }

                var updateRequest = BuildUpdateRequest(EntityId.Value);
                await CallUpdateAsync(updateRequest);
                ToastService.ShowSuccess(GetUpdateSuccessMessage());
            }
            else
            {
                var createRequest = BuildCreateRequest();
                await CallCreateAsync(createRequest);
                ToastService.ShowSuccess(GetCreateSuccessMessage());
            }

            WasSaved = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            var errorMsg = ex.Message;
            if (ex.InnerException != null)
            {
                errorMsg += $"\nDetalhes: {ex.InnerException.Message}";
            }

            ToastService.ShowError(IsEditMode
                ? $"Não foi possível atualizar: {errorMsg}"
                : $"Não foi possível cadastrar: {errorMsg}");
        }
        finally
        {
            IsSaving = false;
        }
    }

    /// <summary>
    /// Builds the create request from form data.
    /// </summary>
    protected abstract TCreateRequest BuildCreateRequest();

    /// <summary>
    /// Builds the update request from form data.
    /// </summary>
    protected abstract TUpdateRequest BuildUpdateRequest(int entityId);

    /// <summary>
    /// Calls the service to create a new entity.
    /// </summary>
    protected abstract Task CallCreateAsync(TCreateRequest request);

    /// <summary>
    /// Calls the service to update an existing entity.
    /// </summary>
    protected abstract Task CallUpdateAsync(TUpdateRequest request);

    /// <summary>
    /// Gets the success message for create operations.
    /// </summary>
    protected abstract string GetCreateSuccessMessage();

    /// <summary>
    /// Gets the success message for update operations.
    /// </summary>
    protected abstract string GetUpdateSuccessMessage();

    /// <summary>
    /// Validates the form data. Returns true if valid, false otherwise.
    /// Should show toast warnings for validation errors.
    /// </summary>
    protected abstract bool ValidateForm();

    /// <summary>
    /// Normalizes a string by trimming whitespace, returning null if empty.
    /// </summary>
    protected static string? NormalizeString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    /// <summary>
    /// Normalizes a string by trimming whitespace, returning empty string if null/empty.
    /// </summary>
    protected static string NormalizeStringRequired(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}