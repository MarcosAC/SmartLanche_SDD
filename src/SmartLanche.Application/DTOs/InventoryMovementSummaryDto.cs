namespace SmartLanche.Application.DTOs;

public sealed record InventoryMovementSummaryDto(
    int TotalEntries,
    int TotalExits,
    int NetQuantity,
    decimal TotalEntryValue,
    decimal TotalExitValue
);