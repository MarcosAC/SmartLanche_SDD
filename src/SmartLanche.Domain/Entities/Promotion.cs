using SmartLanche.Domain.Enums;

namespace SmartLanche.Domain.Entities;

public class Promotion : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    public decimal DiscountValue { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool IsActive { get; set; } = true;

    public int Priority { get; set; }

    public ICollection<Product> ApplicableProducts { get; set; } = new List<Product>();

    public ICollection<Category> ApplicableCategories { get; set; } = new List<Category>();

    public bool IsGlobal => !ApplicableProducts.Any() && !ApplicableCategories.Any();

    public bool IsValidOn(DateTime date)
    {
        if (!IsActive)
        {
            return false;
        }

        if (date < StartDate.Date)
        {
            return false;
        }

        if (EndDate.HasValue && date > EndDate.Value.Date)
        {
            return false;
        }

        return true;
    }

    public decimal CalculateDiscount(decimal amount)
    {
        if (DiscountType == DiscountType.Percentage)
        {
            var discount = amount * DiscountValue / 100m;
            return Math.Round(Math.Min(discount, amount), 2, MidpointRounding.AwayFromZero);
        }
        else // FixedAmount
        {
            return Math.Min(DiscountValue, amount);
        }
    }

    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Nome da promoção é obrigatório");
        }

        if (DiscountValue <= 0)
        {
            throw new ArgumentException("Valor do desconto deve ser maior que zero");
        }

        if (DiscountType == DiscountType.Percentage && DiscountValue > 100)
        {
            throw new ArgumentException("Desconto percentual não pode exceder 100%");
        }

        if (EndDate.HasValue && EndDate.Value.Date < StartDate.Date)
        {
            throw new ArgumentException("Data de fim não pode ser anterior à data de início");
        }

        if (Priority < 0)
        {
            throw new ArgumentException("Prioridade não pode ser negativa");
        }
    }
}