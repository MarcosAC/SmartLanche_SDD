using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public sealed record InventoryItemDto(
    int Id,
    int? ProductId,
    string? ProdutoNome,
    string Nome,
    decimal QuantidadeAtual,
    decimal QuantidadeMinima,
    decimal ValorUnitario,
    string UnidadeMedida,
    string? Observacoes,
    bool EstoqueBaixo,
    bool EstoqueZerado,
    decimal ValorTotalEstoque
)
{
    // XAML-friendly property aliases
    public string ProductName => ProdutoNome ?? Nome ?? string.Empty;
    public decimal CurrentQuantity => QuantidadeAtual;
    public decimal MinQuantity => QuantidadeMinima;
    public decimal UnitPrice => ValorUnitario;
    public decimal TotalValue => ValorTotalEstoque;
    public bool IsLowStock => EstoqueBaixo;
    public bool IsOutOfStock => EstoqueZerado;
    public string CategoryName => string.Empty; // Not available in DTO, would need join
    public StockStatus Status => EstoqueZerado ? StockStatus.OutOfStock : (EstoqueBaixo ? StockStatus.Low : StockStatus.Normal);
}