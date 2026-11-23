using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum StatusPagamentoPendente
{
    [Description("Não Pago")]
    NaoPago = 1,

    [Description("Pago")]
    Pago = 2
}