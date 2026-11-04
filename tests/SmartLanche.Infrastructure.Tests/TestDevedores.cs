using Microsoft.EntityFrameworkCore;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Infrastructure.Data;
using SmartLanche.Infrastructure.Repositories;

public class TestDevedores
{
    [Fact]
    public async Task ShouldGetDevedoresAsync()
    {
        var databaseName = $"SmartLancheDb_TestDevedores_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();

                var categoryRepository = new CategoryRepository(context);
                var category = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(category);

                var productRepository = new ProductRepository(context);
                var product = new Product
                {
                    Name = "X-Burger",
                    CategoryId = category.Id,
                    Price = 25.50m,
                    IsActive = true
                };
                await productRepository.AddAsync(product);

                var customerRepository = new CustomerRepository(context);
                var customer = new Customer { Name = "João Silva", IsActive = true, CreditLimit = 500m };
                await customerRepository.AddAsync(customer);

                var orderRepository = new OrderRepository(context);
                var order = new Order
                {
                    CustomerId = customer.Id,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.Pendente,
                    TotalAmount = 51.00m,
                    Notes = string.Empty
                };
                var orderItem = new OrderItem
                {
                    ProductId = product.Id,
                    Quantity = 2,
                    UnitPrice = 25.50m
                };
                order.OrderItems.Add(orderItem);
                await orderRepository.AddAsync(order);

                var pendingPaymentRepository = new PendingPaymentRepository(context);
                var pendingPayment = new PendingPayment
                {
                    OrderId = order.Id,
                    CustomerId = customer.Id,
                    ValorOriginal = 51.00m,
                    ValorPago = 0m,
                    DataHora = DateTime.Now,
                    StatusPagamento = StatusPagamentoPendente.NaoPago,
                    Observacoes = "Teste"
                };
                await pendingPaymentRepository.AddAsync(pendingPayment);
            }

            // Test GetDevedoresAsync in a new context
            await using (var context = new ApplicationDbContext(options))
            {
                var pendingPaymentRepository = new PendingPaymentRepository(context);
                var devedores = await pendingPaymentRepository.GetDevedoresAsync();

                Console.WriteLine($"Found {devedores.Count} devedores");
                foreach (var d in devedores)
                {
                    Console.WriteLine($"  {d.CustomerId} - {d.CustomerName} - {d.SaldoDevedor:C}");
                }

                Assert.Single(devedores);
                Assert.Equal("João Silva", devedores[0].CustomerName);
                Assert.Equal(51.00m, devedores[0].SaldoDevedor);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }
}
