using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Categories;

public class CategoryListViewModel : CrudListViewModelBase<CategoryDto, ICategoryService>
{
    public CategoryListViewModel(ICategoryService categoryService)
        : base(categoryService)
    {
    }

    protected override string GetEntityName() => "Categoria";
}