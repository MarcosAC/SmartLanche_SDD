using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public sealed record InventoryMovementFilter(
    int? ItemId = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null,
    InventoryMovementType? Tipo = null,
    InventoryMovementReason? Motivo = null,
    int? OrderId = null,
    int? ComboId = null,
    string? SearchTerm = null,
    int Page = 1,
    int PageSize = 50
)
{
    public int Skip => (Page - 1) * PageSize;
    public int Take => PageSize;
}