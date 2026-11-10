using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Entities;

public class InventoryMovement
{
    public int Id { get; set; }

    public int InventoryItemId { get; set; }

    public InventoryItem InventoryItem { get; set; } = null!;

    public InventoryMovementType Tipo { get; set; }

    public InventoryMovementReason Motivo { get; set; }

    public decimal Quantidade { get; set; }

    public decimal ValorUnitario { get; set; }

    public decimal ValorTotal => Quantidade * ValorUnitario;

    public DateTime DataHora { get; set; }

    public int? OrderId { get; set; }

    public int? ComboId { get; set; }

    public string? Fornecedor { get; set; }

    public string? Notes { get; set; }
}