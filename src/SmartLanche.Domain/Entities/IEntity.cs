namespace SmartLanche.Domain.Entities;

/// <summary>
/// Interface for entities that have an Id property.
/// </summary>
public interface IEntityId
{
    int Id { get; set; }
}

/// <summary>
/// Interface for entities that have an Id and IsActive property (soft delete support).
/// </summary>
public interface IEntity : IEntityId
{
    bool IsActive { get; set; }
}