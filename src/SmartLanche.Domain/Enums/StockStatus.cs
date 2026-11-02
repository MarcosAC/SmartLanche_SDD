using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum StockStatus
{
    [Description("Normal")]
    Normal = 0,

    [Description("Baixo")]
    Low = 1,

    [Description("Zerado")]
    OutOfStock = 2
}