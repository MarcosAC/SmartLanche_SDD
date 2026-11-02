using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Categories;

public class CategoryFormViewModel : FormViewModelBase<CategoryDto, CreateCategoryRequest, UpdateCategoryRequest, ICategoryService>
{
    private string _name = string.Empty;
    private string? _description;
    private bool _isActive = true;

    public CategoryFormViewModel(
        ICategoryService categoryService,
        IToastNotificationService toastService)
        : base(categoryService, toastService)
    {
    }

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

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    protected override Task OnInitializeAsync(CategoryDto? category)
    {
        Name = category?.Name ?? string.Empty;
        Description = category?.Description;
        IsActive = category?.IsActive ?? true;
        return Task.CompletedTask;
    }

    protected override CreateCategoryRequest BuildCreateRequest()
    {
        return new CreateCategoryRequest
        {
            Name = NormalizeStringRequired(Name),
            Description = NormalizeString(Description)
        };
    }

    protected override UpdateCategoryRequest BuildUpdateRequest(int entityId)
    {
        return new UpdateCategoryRequest
        {
            Id = entityId,
            Name = NormalizeStringRequired(Name),
            Description = NormalizeString(Description),
            IsActive = IsActive
        };
    }

    protected override Task CallCreateAsync(CreateCategoryRequest request)
    {
        return Service.CreateAsync(request);
    }

    protected override Task CallUpdateAsync(UpdateCategoryRequest request)
    {
        return Service.UpdateAsync(request);
    }

    protected override string GetCreateSuccessMessage() => "Categoria cadastrada com sucesso.";

    protected override string GetUpdateSuccessMessage() => "Categoria atualizada com sucesso.";

    protected override bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ToastService.ShowWarning("Informe o nome da categoria.");
            return false;
        }
        return true;
    }
}