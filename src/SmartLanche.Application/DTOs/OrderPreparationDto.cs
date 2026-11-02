using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public sealed record OrderPreparationDto(
    int Id,
    DateTime OrderDate,
    DateTime? PreparationStartedAt,
    TimeSpan? ElapsedTime,
    string? CustomerName,
    decimal TotalAmount,
    string ItemsSummary,
    OrderStatus Status
);