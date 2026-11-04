using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum OrderStatus
{
    [Description("Pendente")]
    Pendente = 1,

    [Description("Em Preparo")]
    EmPreparacao = 2,

    [Description("Pronto")]
    Pronto = 3,

    [Description("Finalizado")]
    Finalizado = 4,

    [Description("Cancelado")]
    Cancelado = 5
}