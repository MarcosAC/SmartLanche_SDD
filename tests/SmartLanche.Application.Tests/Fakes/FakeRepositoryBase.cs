using System.Collections.Concurrent;

namespace SmartLanche.Application.Tests.Fakes;

public abstract class FakeRepositoryBase<TEntity>
    where TEntity : class
{
    protected readonly ConcurrentDictionary<int, TEntity> _entities = new();
    protected int _nextId = 1;

    public virtual Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<TEntity>>(_entities.Values.OrderBy(GetDefaultSortKey).ToList());
    }

    public virtual Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public virtual Task AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty != null && idProperty.CanWrite)
        {
            var currentId = (int?)idProperty.GetValue(entity);
            if (currentId == null || currentId == 0)
            {
                idProperty.SetValue(entity, _nextId++);
            }
        }
        _entities.TryAdd(GetEntityId(entity), entity);
        return Task.CompletedTask;
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        var id = GetEntityId(entity);
        _entities[id] = entity;
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        _entities.TryRemove(GetEntityId(entity), out _);
        return Task.CompletedTask;
    }

    protected virtual int GetEntityId(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        return (int?)(idProperty?.GetValue(entity)) ?? 0;
    }

    protected virtual object GetDefaultSortKey(TEntity entity)
    {
        var nameProperty = typeof(TEntity).GetProperty("Name");
        if (nameProperty != null)
        {
            return nameProperty.GetValue(entity) ?? string.Empty;
        }
        return GetEntityId(entity);
    }
}