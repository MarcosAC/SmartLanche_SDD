using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Services;

/// <summary>
/// Base class for CRUD services that operate on entities implementing IEntityDto.
/// Provides common implementations for GetAllAsync, CreateAsync, UpdateAsync, DeactivateAsync, ReactivateAsync.
/// Each public method creates its own scope to ensure proper DbContext lifetime management.
/// Derived classes must implement validation, mapping, and entity creation/update logic.
/// </summary>
/// <typeparam name="TEntity">The domain entity type</typeparam>
/// <typeparam name="TDto">The DTO type (must implement IEntityDto)</typeparam>
/// <typeparam name="TCreateRequest">The create request type</typeparam>
/// <typeparam name="TUpdateRequest">The update request type (must have Id property)</typeparam>
/// <typeparam name="TRepository">The repository type</typeparam>
public abstract class CrudServiceBase<TEntity, TDto, TCreateRequest, TUpdateRequest, TRepository>
    where TEntity : class, SmartLanche.Domain.Entities.IEntity
    where TDto : class, IEntityDto
    where TUpdateRequest : class, SmartLanche.Domain.Entities.IEntityId
    where TRepository : class
{
    private readonly IServiceScopeFactory _scopeFactory;

    protected CrudServiceBase(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
    }

    /// <summary>
    /// Executes an operation within a new scope, resolving the repository from DI.
    /// </summary>
    protected async Task<TResult> ExecuteInScopeAsync<TResult>(
        Func<TRepository, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
        return await operation(repository, cancellationToken);
    }

    /// <summary>
    /// Executes an operation within a new scope with access to the full service provider.
    /// </summary>
    protected async Task<TResult> ExecuteInScopeAsync<TResult>(
        Func<IServiceScope, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        return await operation(scope, cancellationToken);
    }

    /// <summary>
    /// Executes an operation within a new scope, resolving the repository from DI (void return).
    /// </summary>
    protected async Task ExecuteInScopeAsync(
        Func<TRepository, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<TRepository>();
        await operation(repository, cancellationToken);
    }

    /// <summary>
    /// Gets all entities with optional active filter.
    /// </summary>
    public virtual async Task<IReadOnlyList<TDto>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
    {
        return await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entities = await GetAllEntitiesAsync(repository, isActive, ct);
            return entities.Select(MapToDto).ToList();
        }, cancellationToken);
    }

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TCreateRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateCreateRequestAsync(request, cancellationToken);

        return await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entity = await CreateEntityAsync(repository, request, ct);

            await AddEntityAsync(repository, entity, ct);

            return MapToDto(entity);
        }, cancellationToken);
    }

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    public virtual async Task<TDto> UpdateAsync(TUpdateRequest request, CancellationToken cancellationToken = default)
    {
        await ValidateUpdateRequestAsync(request, cancellationToken);

        return await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entity = await GetEntityByIdAsync(repository, request.Id, ct);

            if (entity is null)
            {
                throw new InvalidOperationException($"{GetEntityName()} não encontrado.");
            }

            await UpdateEntityAsync(repository, entity, request, ct);

            await UpdateEntityAsync(repository, entity, ct);

            return MapToDto(entity);
        }, cancellationToken);
    }

    /// <summary>
    /// Deactivates (soft deletes) an entity.
    /// </summary>
    public virtual async Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entity = await GetEntityByIdAsync(repository, id, ct);

            if (entity is null)
            {
                throw new InvalidOperationException($"{GetEntityName()} não encontrado.");
            }

            entity.IsActive = false;

            await UpdateEntityAsync(repository, entity, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Reactivates a previously deactivated entity.
    /// </summary>
    public virtual async Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
    {
        await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entity = await GetEntityByIdAsync(repository, id, ct);

            if (entity is null)
            {
                throw new InvalidOperationException($"{GetEntityName()} não encontrado.");
            }

            entity.IsActive = true;

            await UpdateEntityAsync(repository, entity, ct);
        }, cancellationToken);
    }

    /// <summary>
    /// Gets an entity by ID and returns its DTO representation.
    /// </summary>
    public virtual async Task<TDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var entity = await GetEntityByIdAsync(repository, id, ct);
            return entity != null ? MapToDto(entity) : null;
        }, cancellationToken);
    }

    // Abstract methods that derived classes must implement
    // Each receives the repository as a parameter (resolved per scope)

    /// <summary>
    /// Gets the entity name for error messages.
    /// </summary>
    protected abstract string GetEntityName();

    /// <summary>
    /// Gets all entities from the repository.
    /// </summary>
    protected abstract Task<IReadOnlyList<TEntity>> GetAllEntitiesAsync(TRepository repository, bool? isActive, CancellationToken cancellationToken);

    /// <summary>
    /// Gets an entity by ID from the repository.
    /// </summary>
    protected abstract Task<TEntity?> GetEntityByIdAsync(TRepository repository, int id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    protected abstract Task AddEntityAsync(TRepository repository, TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    protected abstract Task UpdateEntityAsync(TRepository repository, TEntity entity, CancellationToken cancellationToken);

    /// <summary>
    /// Maps a domain entity to a DTO.
    /// </summary>
    protected abstract TDto MapToDto(TEntity entity);

    /// <summary>
    /// Creates a new entity from the create request (async to allow duplicate checks).
    /// </summary>
    protected abstract Task<TEntity> CreateEntityAsync(TRepository repository, TCreateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Updates an entity from the update request (async to allow duplicate checks).
    /// </summary>
    protected abstract Task UpdateEntityAsync(TRepository repository, TEntity entity, TUpdateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the create request. Throws ArgumentException or InvalidOperationException if invalid.
    /// </summary>
    protected abstract Task ValidateCreateRequestAsync(TCreateRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the update request. Throws ArgumentException or InvalidOperationException if invalid.
    /// </summary>
    protected abstract Task ValidateUpdateRequestAsync(TUpdateRequest request, CancellationToken cancellationToken);
}