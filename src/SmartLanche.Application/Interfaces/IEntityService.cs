namespace SmartLanche.Application.Interfaces;

public interface IEntityDto
{
    int Id { get; set; }
    bool IsActive { get; set; }
}

public interface IEntityService<TDto>
    where TDto : class, IEntityDto
{
    Task<IReadOnlyList<TDto>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default);
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);
    Task ReactivateAsync(int id, CancellationToken cancellationToken = default);
}