using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Entities;

public class InventoryItem
{
    public int Id { get; set; }

    public int? ProductId { get; set; }

    public Product? Produto { get; set; }

    public string Nome { get; set; } = string.Empty;

    public decimal QuantidadeAtual { get; set; }

    public decimal QuantidadeMinima { get; set; }

    public decimal ValorUnitario { get; set; }

    public string UnidadeMedida { get; set; } = "UN";

    public string? Notes { get; set; }

    public bool EstoqueBaixo => QuantidadeAtual <= QuantidadeMinima && QuantidadeAtual > 0;

    public bool EstoqueZerado => QuantidadeAtual <= 0;

    public decimal ValorTotalEstoque => QuantidadeAtual * ValorUnitario;

    public ICollection<InventoryMovement> Movements { get; set; } = [];

    public void ValidateForCreate()
    {
        if (string.IsNullOrWhiteSpace(Nome))
        {
            throw new ArgumentException("Nome é obrigatório.", nameof(Nome));
        }

        if (Nome.Length > 150)
        {
            throw new ArgumentException("Nome deve ter no máximo 150 caracteres.", nameof(Nome));
        }

        if (QuantidadeMinima < 0)
        {
            throw new ArgumentException("Quantidade mínima não pode ser negativa.", nameof(QuantidadeMinima));
        }

        if (ValorUnitario < 0)
        {
            throw new ArgumentException("Valor unitário não pode ser negativo.", nameof(ValorUnitario));
        }

        if (string.IsNullOrWhiteSpace(UnidadeMedida))
        {
            throw new ArgumentException("Unidade de medida é obrigatória.", nameof(UnidadeMedida));
        }

        if (UnidadeMedida.Length > 20)
        {
            throw new ArgumentException("Unidade de medida deve ter no máximo 20 caracteres.", nameof(UnidadeMedida));
        }

        if (Notes?.Length > 500)
        {
            throw new ArgumentException("Observações deve ter no máximo 500 caracteres.", nameof(Notes));
        }
    }

    public void ValidateForUpdate()
    {
        if (QuantidadeMinima < 0)
        {
            throw new ArgumentException("Quantidade mínima não pode ser negativa.", nameof(QuantidadeMinima));
        }

        if (ValorUnitario < 0)
        {
            throw new ArgumentException("Valor unitário não pode ser negativo.", nameof(ValorUnitario));
        }

        if (string.IsNullOrWhiteSpace(UnidadeMedida))
        {
            throw new ArgumentException("Unidade de medida é obrigatória.", nameof(UnidadeMedida));
        }

        if (UnidadeMedida.Length > 20)
        {
            throw new ArgumentException("Unidade de medida deve ter no máximo 20 caracteres.", nameof(UnidadeMedida));
        }

        if (Notes?.Length > 500)
        {
            throw new ArgumentException("Observações deve ter no máximo 500 caracteres.", nameof(Notes));
        }
    }

    public void ApplyEntrada(decimal quantidade, decimal valorUnitario, string? fornecedor, string? observacoes)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentException("Quantidade de entrada deve ser maior que zero.", nameof(quantidade));
        }

        QuantidadeAtual += quantidade;
        ValorUnitario = valorUnitario;

        var movement = new InventoryMovement
        {
            InventoryItemId = Id,
            Tipo = InventoryMovementType.Entrada,
            Motivo = InventoryMovementReason.Compra,
            Quantidade = quantidade,
            ValorUnitario = valorUnitario,
            DataHora = DateTime.UtcNow,
            Fornecedor = fornecedor,
            Notes = observacoes
        };

        Movements.Add(movement);
    }

    public void ApplySaida(decimal quantidade, InventoryMovementReason motivo, string? observacoes)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentException("Quantidade de saída deve ser maior que zero.", nameof(quantidade));
        }

        if (QuantidadeAtual < quantidade)
        {
            throw new InvalidOperationException($"Estoque insuficiente. Disponível: {QuantidadeAtual}, Solicitado: {quantidade}");
        }

        QuantidadeAtual -= quantidade;

        var movement = new InventoryMovement
        {
            InventoryItemId = Id,
            Tipo = InventoryMovementType.Saida,
            Motivo = motivo,
            Quantidade = quantidade,
            ValorUnitario = ValorUnitario,
            DataHora = DateTime.UtcNow,
            Notes = observacoes
        };

        Movements.Add(movement);
    }
}