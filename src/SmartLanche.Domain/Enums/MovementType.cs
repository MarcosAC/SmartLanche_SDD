using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum MovementType
{
    [Description("Entrada")]
    Entrada = 1,

    [Description("Saída")]
    Saida = 2
}