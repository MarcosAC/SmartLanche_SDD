using System.Globalization;
using System.IO;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Infrastructure.Data;
using SmartLanche.Infrastructure.Repositories;
using SmartLanche.Presentation.Services;
using SmartLanche.Presentation.ViewModels;
using SmartLanche.Presentation.ViewModels.Categories;
using SmartLanche.Presentation.ViewModels.Combos;
using SmartLanche.Presentation.ViewModels.Customers;
using SmartLanche.Presentation.ViewModels.Orders;
using SmartLanche.Presentation.ViewModels.Payments;
using SmartLanche.Presentation.ViewModels.Products;
using SmartLanche.Presentation.ViewModels.Promotions;
using SmartLanche.Presentation.ViewModels.OrderPreparation;
using SmartLanche.Presentation.ViewModels.Inventory;

namespace SmartLanche.Presentation;

public partial class App : System.Windows.Application
{
    private IHost? _host;
    private IServiceScope? _appScope;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Set Brazilian Portuguese culture for currency formatting
        var ptBrCulture = new CultureInfo("pt-BR");
        CultureInfo.DefaultThreadCurrentCulture = ptBrCulture;
        CultureInfo.DefaultThreadCurrentUICulture = ptBrCulture;

        // Configure Serilog
        var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "smartlanche-.log");
        Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: 31, fileSizeLimitBytes: 10_000_000)
            .WriteTo.Console()
            .WriteTo.Debug()
            .CreateLogger();

        // Global exception handler
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            _host = Host.CreateDefaultBuilder().UseContentRoot(AppContext.BaseDirectory)
                .UseSerilog()
                .ConfigureServices((context, services) =>
                {
                    var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                        ?? throw new InvalidOperationException("A connection string 'DefaultConnection' não foi encontrada no appsettings.json.");

                    // Entity Framework Core - Scoped to share DbContext across repositories in one operation
                    services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString), ServiceLifetime.Scoped);

                    // Repositories - Scoped to share DbContext within one operation
                    services.AddScoped<IProductRepository, ProductRepository>();
                    services.AddScoped<ICategoryRepository, CategoryRepository>();
                    services.AddScoped<ICustomerRepository, CustomerRepository>();
                    services.AddScoped<IOrderRepository, OrderRepository>();
                    services.AddScoped<IPromotionRepository, PromotionRepository>();
                    services.AddScoped<IComboRepository, ComboRepository>();
                    services.AddScoped<IPaymentRepository, PaymentRepository>();
                    services.AddScoped<IPendingPaymentRepository, PendingPaymentRepository>();
                    services.AddScoped<IInventoryRepository, InventoryRepository>();
                    services.AddScoped<IInventoryMovementRepository, InventoryMovementRepository>();

                    // Application services - Scoped to share DbContext within one operation
                    services.AddScoped<IProductService, ProductService>();
                    services.AddScoped<ICategoryService, CategoryService>();
                    services.AddScoped<ICustomerService, CustomerService>();
                    services.AddScoped<IOrderService, OrderService>();
                    services.AddScoped<IOrderPreparationService, OrderPreparationService>();
                    services.AddScoped<IPromotionService, PromotionService>();
                    services.AddScoped<IComboService, ComboService>();
                    services.AddScoped<IPaymentService, PaymentService>();
                    services.AddScoped<IPendingPaymentService, PendingPaymentService>();
                    services.AddScoped<IInventoryService, InventoryService>();
                    services.AddScoped<IInventoryMovementService, InventoryMovementService>();
                    services.AddScoped<IOrderPromotionService, OrderPromotionService>();

                    // ViewModels
                    services.AddTransient<MainViewModel>();

                    services.AddTransient<HomeViewModel>();

                    services.AddTransient<ProductListViewModel>();
                    services.AddTransient<ProductFormViewModel>();

                    services.AddTransient<CategoryListViewModel>();
                    services.AddTransient<CategoryFormViewModel>();

                    services.AddTransient<CustomerListViewModel>();
                    services.AddTransient<CustomerFormViewModel>();

                    services.AddTransient<OrderListViewModel>();
                    services.AddTransient<OrderFormViewModel>();
                    services.AddTransient<OrderDetailViewModel>();
                    services.AddTransient<ProductPickerViewModel>();
                    services.AddTransient<OrderPreparationListViewModel>();

                    services.AddTransient<InventoryListViewModel>();
                    services.AddTransient<AddStockViewModel>();
                    services.AddTransient<RemoveStockViewModel>();
                    services.AddTransient<InventoryMovementViewModel>();
                    services.AddTransient<EditMinStockViewModel>();
                    services.AddTransient<InventoryMovementsViewModel>();

                    services.AddTransient<PromotionListViewModel>();
                    services.AddTransient<PromotionFormViewModel>();

                    services.AddTransient<ComboListViewModel>();
                    services.AddTransient<ComboFormViewModel>();

                    services.AddTransient<PendingPaymentListViewModel>();

                    services.AddTransient<PaymentListViewModel>();

                    services.AddTransient<PendingPaymentHistoryViewModel>();
                    services.AddTransient<DevedoresViewModel>();

                    // Main window
                    services.AddTransient<MainWindow>();

                    // Toast notification service
                    services.AddSingleton<IToastNotificationService, ToastNotificationService>();
                }).Build();

            await _host.StartAsync();

            _appScope = _host.Services.CreateScope();

            // Apply migrations on startup
            using (var scope = _host.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                await dbContext.Database.MigrateAsync();
            }

            MainWindow = _appScope.ServiceProvider.GetRequiredService<MainWindow>();

            MainWindow.Show();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Falha ao iniciar o SmartLanche");
            MessageBox.Show(
                $"Não foi possível iniciar o SmartLanche.\n\n{ex.Message}",
                "Erro na inicialização",
                MessageBoxButton.OK,
                MessageBoxImage.Error);

            Shutdown(-1);
        }
    }

    private void OnDispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
    {
        var ex = e.Exception;

        // Log to file via Serilog
        Log.Fatal(ex, "UNHANDLED EXCEPTION");

        // Try MessageBox but don't recurse if it fails
        try
        {
            MessageBox.Show(
                $"Erro não tratado:\n{ex.Message}",
                "Erro Fatal",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch { /* ignore MessageBox errors to prevent StackOverflow */ }

        e.Handled = true;
    }
}
