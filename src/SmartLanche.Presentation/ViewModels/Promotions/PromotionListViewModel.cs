using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Promotions;

public class PromotionListViewModel : CrudListViewModelBase<PromotionDto, IPromotionService>
{
    public PromotionListViewModel(IPromotionService promotionService)
        : base(promotionService)
    {
    }

    protected override string GetEntityName() => "Promoção";

    protected override IReadOnlyList<PromotionDto> ApplySearchFilter(IReadOnlyList<PromotionDto> items, string searchTerm)
    {
        return items
            .Where(p => p.Name.ToLowerInvariant().Contains(searchTerm)
                || (p.Description?.ToLowerInvariant().Contains(searchTerm) ?? false))
            .ToList();
    }

    protected override IReadOnlyList<PromotionDto> ApplySort(IReadOnlyList<PromotionDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.OrderBy(p => p.Priority).ThenBy(p => p.Name).ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "name" or "nome" => items.OrderBy(p => p.Name),
            "discounttype" or "tipodesconto" => items.OrderBy(p => p.DiscountType),
            "discountvalue" or "valordesconto" => items.OrderBy(p => p.DiscountValue),
            "startdate" or "datainicio" => items.OrderBy(p => p.StartDate),
            "enddate" or "datafim" => items.OrderBy(p => p.EndDate ?? DateTime.MaxValue),
            "priority" or "prioridade" => items.OrderBy(p => p.Priority),
            "isactive" or "ativo" => items.OrderBy(p => p.IsActive),
            _ => items.OrderBy(p => p.Priority).ThenBy(p => p.Name)
        };

        return SortDescending ? sorted.Reverse().ToList() : sorted.ToList();
    }
}