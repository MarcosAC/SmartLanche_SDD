using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Products;

public class ProductListViewModel : CrudListViewModelBase<ProductDto, IProductService>
{
    public ProductListViewModel(IProductService productService)
        : base(productService)
    {
    }

    protected override string GetEntityName() => "Produto";
}