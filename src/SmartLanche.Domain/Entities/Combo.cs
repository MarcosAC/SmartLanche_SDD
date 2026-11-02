namespace SmartLanche.Domain.Entities;

public class Combo : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public bool IsActive { get; set; } = true;

    public ICollection<ComboItem> ComboItems { get; set; } = new List<ComboItem>();

    public bool IsAvailable => ComboItems.All(ci => ci.Product?.IsActive == true);

    public IEnumerable<(Product Product, int Quantity)> GetComponents()
    {
        return ComboItems
            .Where(ci => ci.Product != null)
            .Select(ci => (ci.Product!, ci.Quantity));
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Nome do combo é obrigatório");
        }

        if (Price <= 0)
        {
            throw new ArgumentException("Preço do combo deve ser maior que zero");
        }

        if (ComboItems.Count < 2)
        {
            throw new ArgumentException("Combo deve ter pelo menos 2 itens componentes");
        }

        foreach (var item in ComboItems)
        {
            if (item.Product == null)
            {
                throw new ArgumentException("Todos os itens do combo devem referenciar um produto válido");
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException("Quantidade de cada item deve ser maior que zero");
            }
        }
    }
}