using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public sealed record InventoryMovementDto(
    int Id,
    int InventoryItemId,
    string ItemNome,
    InventoryMovementType Tipo,
    InventoryMovementReason Motivo,
    decimal Quantidade,
    decimal ValorUnitario,
    decimal ValorTotal,
    DateTime DataHora,
    int? OrderId,
    int? ComboId,
    string? Fornecedor,
    string? Notes
)
{

    public DateTime CreatedAt => DataHora;
    public InventoryMovementType Type => Tipo;
    public string Origin => Motivo.ToString();
    public decimal Quantity => Quantidade;
    public decimal PreviousQuantity => 0;
    public decimal NewQuantity => 0;
    public string Reason => Notes ?? Motivo.ToString();
    public string ReferenceId => OrderId.HasValue ? $"Pedido #{OrderId}" : (ComboId.HasValue ? $"Combo #{ComboId}" : (Fornecedor ?? string.Empty));
}