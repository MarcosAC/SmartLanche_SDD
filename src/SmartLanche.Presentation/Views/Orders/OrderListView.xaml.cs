using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using SmartLanche.Presentation.ViewModels.Orders;

namespace SmartLanche.Presentation.Views.Orders;

public partial class OrderListView : UserControl
{
    public OrderListView()
    {
        InitializeComponent();
    }

    private void DataGrid_Sorting(object sender, DataGridSortingEventArgs e)
    {
        if (DataContext is not OrderListViewModel viewModel)
        {
            return;
        }

        var column = e.Column;
        var sortDirection = column.SortDirection != ListSortDirection.Descending
            ? ListSortDirection.Descending
            : ListSortDirection.Ascending;

        column.SortDirection = sortDirection;

        var propertyName = column.SortMemberPath switch
        {
            "Id" => "id",
            "OrderDate" => "orderdate",
            "CustomerName" => "customer",
            "TotalAmount" => "total",
            "Status" => "status",
            _ => "orderdate"
        };

        viewModel.SortColumn = propertyName;
        viewModel.SortDescending = sortDirection == ListSortDirection.Descending;

        e.Handled = true;
    }
}