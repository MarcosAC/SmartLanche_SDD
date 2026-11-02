using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public sealed record RegistrarEntradaRequest(
    int InventoryItemId,
    decimal Quantidade,
    decimal ValorUnitario,
    string? Fornecedor,
    string? Observacoes
);