using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum InventoryMovementType
{
    [Description("Entrada")]
    Entrada = 1,

    [Description("Saída")]
    Saida = 2
}