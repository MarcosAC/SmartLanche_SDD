using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum MovementOrigin
{
    [Description("Manual")]
    Manual = 1,

    [Description("Pedido")]
    Pedido = 2,

    [Description("Ajuste")]
    Ajuste = 3
}