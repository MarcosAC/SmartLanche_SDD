using System.Windows;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;

namespace SmartLanche.Presentation;

public partial class MainWindow : Window
{
    public IToastNotificationService ToastService { get; }

    public MainWindow(MainViewModel viewModel, IToastNotificationService toastService)
    {
        InitializeComponent();

        ToastService = toastService ?? throw new ArgumentNullException(nameof(toastService));
        DataContext = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
    }
}
