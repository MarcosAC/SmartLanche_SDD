using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Combos;

public class ComboListViewModel : CrudListViewModelBase<ComboDto, IComboService>
{
    public ComboListViewModel(IComboService comboService)
        : base(comboService)
    {
    }

    protected override string GetEntityName() => "Combo";

    protected override IReadOnlyList<ComboDto> ApplySearchFilter(IReadOnlyList<ComboDto> items, string searchTerm)
    {
        return items
            .Where(c => c.Name.ToLowerInvariant().Contains(searchTerm)
                || (c.Description?.ToLowerInvariant().Contains(searchTerm) ?? false))
            .ToList();
    }

    protected override IReadOnlyList<ComboDto> ApplySort(IReadOnlyList<ComboDto> items)
    {
        if (string.IsNullOrWhiteSpace(SortColumn))
        {
            return items.OrderBy(c => c.Name).ToList();
        }

        var sorted = SortColumn.ToLowerInvariant() switch
        {
            "name" or "nome" => items.OrderBy(c => c.Name),
            "price" or "preco" => items.OrderBy(c => c.Price),
            "itemscount" or "itenscount" or "quantidadeitens" => items.OrderBy(c => c.Items.Count),
            "isactive" or "ativo" or "isavailable" or "disponivel" => items.OrderBy(c => c.IsActive),
            _ => items.OrderBy(c => c.Name)
        };

        return SortDescending ? sorted.Reverse().ToList() : sorted.ToList();
    }
}