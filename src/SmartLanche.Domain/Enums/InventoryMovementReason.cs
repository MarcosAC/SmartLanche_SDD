using System.ComponentModel;

namespace SmartLanche.Domain.Enums;

public enum InventoryMovementReason
{
    [Description("Compra")]
    Compra = 1,

    [Description("Reposição")]
    Reposicao = 2,

    [Description("Venda")]
    Venda = 3,

    [Description("Perda")]
    Perda = 4,

    [Description("Ajuste")]
    Ajuste = 5,

    [Description("Consumo Interno")]
    ConsumoInterno = 6,

    [Description("Doação")]
    Doacao = 7,

    [Description("Devolução Fornecedor")]
    DevolucaoFornecedor = 8,

    [Description("Estorno de Venda")]
    EstornoVenda = 9
}