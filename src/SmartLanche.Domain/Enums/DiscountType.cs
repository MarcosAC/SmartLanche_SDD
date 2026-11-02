using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum DiscountType
{
    [Description("Percentual")]
    Percentage = 1,

    [Description("Valor Fixo")]
    FixedAmount = 2
}