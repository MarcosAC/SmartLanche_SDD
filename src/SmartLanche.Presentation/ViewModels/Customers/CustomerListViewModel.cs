using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation.ViewModels.Customers;

public class CustomerListViewModel : CrudListViewModelBase<CustomerDto, ICustomerService>
{
    public CustomerListViewModel(ICustomerService customerService)
        : base(customerService)
    {
    }

    protected override string GetEntityName() => "Cliente";
}