namespace SmartLanche.Application.DTOs;

public sealed record UpdateInventoryItemRequest(
    decimal QuantidadeMinima,
    decimal ValorUnitario,
    string UnidadeMedida,
    string? Observacoes
);