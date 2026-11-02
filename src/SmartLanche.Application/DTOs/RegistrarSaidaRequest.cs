using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public sealed record RegistrarSaidaRequest(
    int InventoryItemId,
    decimal Quantidade,
    InventoryMovementReason Motivo,
    string? Observacoes
);