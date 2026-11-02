using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Customers;

public class CustomerFormViewModel : FormViewModelBase<CustomerDto, CreateCustomerRequest, UpdateCustomerRequest, ICustomerService>
{
    private string _name = string.Empty;
    private string? _phone;
    private string? _notes;
    private decimal _creditLimit;
    private bool _isActive = true;

    public CustomerFormViewModel(
        ICustomerService customerService,
        IToastNotificationService toastService)
        : base(customerService, toastService)
    {
    }

    public string Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? Phone
    {
        get => _phone;
        set => SetProperty(ref _phone, value);
    }

    public string? Notes
    {
        get => _notes;
        set => SetProperty(ref _notes, value);
    }

    public decimal CreditLimit
    {
        get => _creditLimit;
        set => SetProperty(ref _creditLimit, value);
    }

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    protected override Task OnInitializeAsync(CustomerDto? customer)
    {
        Name = customer?.Name ?? string.Empty;
        Phone = customer?.Phone;
        Notes = customer?.Notes;
        CreditLimit = customer?.CreditLimit ?? 0m;
        IsActive = customer?.IsActive ?? true;
        return Task.CompletedTask;
    }

    protected override CreateCustomerRequest BuildCreateRequest()
    {
        return new CreateCustomerRequest
        {
            Name = NormalizeStringRequired(Name),
            Phone = NormalizeString(Phone),
            Notes = NormalizeString(Notes),
            CreditLimit = CreditLimit
        };
    }

    protected override UpdateCustomerRequest BuildUpdateRequest(int entityId)
    {
        return new UpdateCustomerRequest
        {
            Id = entityId,
            Name = NormalizeStringRequired(Name),
            Phone = NormalizeString(Phone),
            Notes = NormalizeString(Notes),
            CreditLimit = CreditLimit,
            IsActive = IsActive
        };
    }

    protected override Task CallCreateAsync(CreateCustomerRequest request)
    {
        return Service.CreateAsync(request);
    }

    protected override Task CallUpdateAsync(UpdateCustomerRequest request)
    {
        return Service.UpdateAsync(request);
    }

    protected override string GetCreateSuccessMessage() => "Cliente cadastrado com sucesso.";

    protected override string GetUpdateSuccessMessage() => "Cliente atualizado com sucesso.";

    protected override bool ValidateForm()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ToastService.ShowWarning("Informe o nome do cliente.");
            return false;
        }

        if (Name.Trim().Length > 150)
        {
            ToastService.ShowWarning("O nome do cliente deve ter no máximo 150 caracteres.");
            return false;
        }

        if (Phone is not null && Phone.Length > 20)
        {
            ToastService.ShowWarning("O telefone do cliente deve ter no máximo 20 caracteres.");
            return false;
        }

        if (Notes is not null && Notes.Length > 500)
        {
            ToastService.ShowWarning("As observações do cliente devem ter no máximo 500 caracteres.");
            return false;
        }

        if (CreditLimit < 0)
        {
            ToastService.ShowWarning("O limite de crédito não pode ser negativo.");
            return false;
        }

        return true;
    }
}