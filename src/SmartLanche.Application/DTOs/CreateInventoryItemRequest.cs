namespace SmartLanche.Application.DTOs;

public sealed record CreateInventoryItemRequest(
    int? ProductId,
    string Nome,
    decimal QuantidadeAtual,
    decimal QuantidadeMinima,
    decimal ValorUnitario,
    string UnidadeMedida,
    string? Notes
);