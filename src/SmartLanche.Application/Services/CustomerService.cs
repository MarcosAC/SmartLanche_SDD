using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Services;

public class CustomerService : CrudServiceBase<Customer, CustomerDto, CreateCustomerRequest, UpdateCustomerRequest, ICustomerRepository>, ICustomerService
{
    private readonly IPendingPaymentRepository _pendingPaymentRepository;

    public CustomerService(IServiceScopeFactory scopeFactory, IPendingPaymentRepository pendingPaymentRepository)
        : base(scopeFactory)
    {
        _pendingPaymentRepository = pendingPaymentRepository ?? throw new ArgumentNullException(nameof(pendingPaymentRepository));
    }

    protected override string GetEntityName() => "Cliente";

    protected override async Task<IReadOnlyList<Customer>> GetAllEntitiesAsync(ICustomerRepository repository, bool? isActive, CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(isActive, cancellationToken);
    }

    protected override async Task<Customer?> GetEntityByIdAsync(ICustomerRepository repository, int id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    protected override async Task AddEntityAsync(ICustomerRepository repository, Customer entity, CancellationToken cancellationToken)
    {
        await repository.AddAsync(entity, cancellationToken);
    }

    protected override async Task UpdateEntityAsync(ICustomerRepository repository, Customer entity, CancellationToken cancellationToken)
    {
        await repository.UpdateAsync(entity, cancellationToken);
    }

    protected override CustomerDto MapToDto(Customer entity)
    {
        return new CustomerDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Phone = entity.Phone,
            Notes = entity.Notes,
            CreditLimit = entity.CreditLimit,
            IsActive = entity.IsActive
        };
    }

    protected override async Task<Customer> CreateEntityAsync(ICustomerRepository repository, CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        return new Customer
        {
            Name = request.Name.Trim(),
            Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim(),
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            CreditLimit = request.CreditLimit,
            IsActive = true
        };
    }

    protected override async Task UpdateEntityAsync(ICustomerRepository repository, Customer entity, UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        entity.Name = request.Name.Trim();
        entity.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
        entity.Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim();
        entity.CreditLimit = request.CreditLimit;
        entity.IsActive = request.IsActive;
    }

    protected override async Task ValidateCreateRequestAsync(CreateCustomerRequest request, CancellationToken cancellationToken)
    {
        await ValidateCustomerRequestAsync(request.Name, request.Phone, request.Notes, request.CreditLimit, cancellationToken);
    }

    protected override async Task ValidateUpdateRequestAsync(UpdateCustomerRequest request, CancellationToken cancellationToken)
    {
        await ValidateCustomerRequestAsync(request.Name, request.Phone, request.Notes, request.CreditLimit, cancellationToken);
    }

    private static Task ValidateCustomerRequestAsync(string name, string? phone, string? notes, decimal creditLimit, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome do cliente é obrigatório.", nameof(name));
        }

        if (name.Trim().Length > 150)
        {
            throw new ArgumentException("O nome do cliente deve possuir no máximo 150 caracteres.", nameof(name));
        }

        if (phone is not null && phone.Length > 20)
        {
            throw new ArgumentException("O telefone do cliente deve possuir no máximo 20 caracteres.", nameof(phone));
        }

        if (notes is not null && notes.Length > 500)
        {
            throw new ArgumentException("As observações do cliente devem possuir no máximo 500 caracteres.", nameof(notes));
        }

        if (creditLimit < 0)
        {
            throw new ArgumentException("O limite de crédito não pode ser negativo.", nameof(creditLimit));
        }

        return Task.CompletedTask;
    }

    // Pending-orders specific
    public async Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _pendingPaymentRepository.GetSaldoDevedorAsync(customerId, cancellationToken);
    }

    public async Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
    {
        return await _pendingPaymentRepository.GetDevedoresAsync(cancellationToken);
    }
}