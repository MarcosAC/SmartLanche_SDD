using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum OrderStatus
{
    [Description("Aberto")]
    Aberto = 1,

    [Description("Em Preparação")]
    EmPreparacao = 2,

    [Description("Entregue")]
    Entregue = 3,

    [Description("Fechado")]
    Fechado = 4,

    [Description("Cancelado")]
    Cancelado = 5
}