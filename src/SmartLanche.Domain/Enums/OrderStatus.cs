using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum OrderStatus
{
    [Description("Em Preparo")]
    EmPreparacao = 1,

    [Description("Pronto")]
    Pronto = 2,

    [Description("Finalizado")]
    Finalizado = 3,

    [Description("Cancelado")]
    Cancelado = 4
}