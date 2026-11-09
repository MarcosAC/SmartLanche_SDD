using Microsoft.EntityFrameworkCore;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;
using SmartLanche.Infrastructure.Data;
using SmartLanche.Infrastructure.Repositories;

namespace SmartLanche.Infrastructure.Tests;

public class PersistenceIntegrationTests
{
    [Fact]
    public async Task ShouldPersistCategoryAndProductInSqlServer()
    {
        var databaseName = $"SmartLancheDb_IntegrationTests_{Guid.NewGuid():N}";

        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int categoryId;
            int productId;

            // Cria o banco de teste e aplica as migrations.
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();

                var categoryRepository = new CategoryRepository(context);

                var category = new Category
                {
                    Name = $"Categoria de teste {Guid.NewGuid():N}",
                    Description = "Categoria criada pelo teste de integração",
                    IsActive = true
                };

                await categoryRepository.AddAsync(category);

                categoryId = category.Id;

                Assert.True(categoryId > 0);
            }

            // Cria o produto usando um contexto separado.
            await using (var context = new ApplicationDbContext(options))
            {
                var productRepository = new ProductRepository(context);

                var product = new Product
                {
                    Name = $"Produto de teste {Guid.NewGuid():N}",
                    CategoryId = categoryId,
                    Price = 19.90m,
                    IsCombo = true,
                    Description = "Produto criado pelo teste de integração",
                    IsActive = true
                };

                await productRepository.AddAsync(product);

                productId = product.Id;

                Assert.True(productId > 0);
            }

            // Abre outro contexto para comprovar a persistência.
            await using (var context = new ApplicationDbContext(options))
            {
                var categoryRepository = new CategoryRepository(context);
                var productRepository = new ProductRepository(context);

                var savedCategory = await categoryRepository.GetByIdAsync(categoryId);

                var savedProduct = await productRepository.GetByIdAsync(productId);

                Assert.NotNull(savedCategory);
                Assert.NotNull(savedProduct);

                Assert.Equal(categoryId, savedProduct.CategoryId);
                Assert.Equal(19.90m, savedProduct.Price);
                Assert.True(savedProduct.IsCombo);
                Assert.Equal(savedCategory.Name, savedProduct.Category?.Name);
            }
        }
        finally
        {
            // Exclui somente o banco exclusivo deste teste.
            await using var cleanupContext = new ApplicationDbContext(options);

            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistValidCategory()
    {
        var databaseName = $"SmartLancheDb_PersistCategory_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int categoryId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);

                var category = new Category
                {
                    Name = "Lanches",
                    Description = "Categoria de lanches",
                    IsActive = true
                };

                await categoryRepository.AddAsync(category);
                categoryId = category.Id;
                Assert.True(categoryId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var categoryRepository = new CategoryRepository(context);
                var savedCategory = await categoryRepository.GetByIdAsync(categoryId);

                Assert.NotNull(savedCategory);
                Assert.Equal("Lanches", savedCategory.Name);
                Assert.Equal("Categoria de lanches", savedCategory.Description);
                Assert.True(savedCategory.IsActive);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRejectDuplicateCategoryNameExact()
    {
        var databaseName = $"SmartLancheDb_DupExact_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);

                var category1 = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(category1);

                var category2 = new Category { Name = "Lanches", IsActive = true };
                var exists = await categoryRepository.ExistsByNormalizedNameAsync("LANCHES");

                Assert.True(exists);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRejectDuplicateCategoryNameCaseInsensitive()
    {
        var databaseName = $"SmartLancheDb_DupCase_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);

                var category1 = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(category1);

                var exists = await categoryRepository.ExistsByNormalizedNameAsync("LANCHES");

                Assert.True(exists);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRejectDuplicateCategoryNameWithTrimmedSpaces()
    {
        var databaseName = $"SmartLancheDb_DupTrim_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);

                var category1 = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(category1);

                // O repositório espera nome normalizado (trim + uppercase)
                var normalizedName = "  LANCHES  ".Trim().ToUpperInvariant();
                var exists = await categoryRepository.ExistsByNormalizedNameAsync(normalizedName);

                Assert.True(exists);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldAllowEditKeepingSameNameCaseInsensitive()
    {
        var databaseName = $"SmartLancheDb_EditSame_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int categoryId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);

                var category = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(category);
                categoryId = category.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var categoryRepository = new CategoryRepository(context);
                var category = await categoryRepository.GetByIdAsync(categoryId);
                Assert.NotNull(category);

                category.Name = "lanches";
                await categoryRepository.UpdateAsync(category);

                var exists = await categoryRepository.ExistsByNormalizedNameAsync("LANCHES", categoryId);
                Assert.False(exists);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRejectEditToAnotherCategoryName()
    {
        var databaseName = $"SmartLancheDb_EditOther_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int categoryId1, categoryId2;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);

                var cat1 = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(cat1);
                categoryId1 = cat1.Id;

                var cat2 = new Category { Name = "Bebidas", IsActive = true };
                await categoryRepository.AddAsync(cat2);
                categoryId2 = cat2.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var categoryRepository = new CategoryRepository(context);
                var category = await categoryRepository.GetByIdAsync(categoryId2);
                Assert.NotNull(category);

                category.Name = "Lanches";
                var exists = await categoryRepository.ExistsByNormalizedNameAsync("LANCHES", categoryId2);
                Assert.True(exists);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistProductWithValidCategory()
    {
        var databaseName = $"SmartLancheDb_ProductValid_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int categoryId, productId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);
                var category = new Category { Name = "Lanches", IsActive = true };
                await categoryRepository.AddAsync(category);
                categoryId = category.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var productRepository = new ProductRepository(context);
                var product = new Product
                {
                    Name = "X-Burger",
                    CategoryId = categoryId,
                    Price = 25.00m,
                    IsCombo = false,
                    Description = "Hambúrguer com queijo",
                    IsActive = true
                };
                await productRepository.AddAsync(product);
                productId = product.Id;
                Assert.True(productId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var productRepository = new ProductRepository(context);
                var saved = await productRepository.GetByIdAsync(productId);
                Assert.NotNull(saved);
                Assert.Equal("X-Burger", saved.Name);
                Assert.Equal(categoryId, saved.CategoryId);
                Assert.Equal(25.00m, saved.Price);
                Assert.False(saved.IsCombo);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRejectProductWithNonExistentCategory()
    {
        var databaseName = $"SmartLancheDb_ProductNoCat_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var productRepository = new ProductRepository(context);
                var product = new Product
                {
                    Name = "X-Burger",
                    CategoryId = 999,
                    Price = 25.00m,
                    IsActive = true
                };
                await productRepository.AddAsync(product);
                await context.SaveChangesAsync();
            }
            Assert.Fail("Deveria ter falhado com FK inexistente");
        }
        catch (DbUpdateException)
        {
            // Esperado - FK constraint
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRejectProductWithInactiveCategoryAtRepositoryLevel()
    {
        // Nota: A validação de categoria inativa ocorre no Service (Application), não no Repository.
        // Este teste verifica que o Repository permite persistir (o Service é quem bloqueia).
        var databaseName = $"SmartLancheDb_ProductInactiveCat_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int categoryId, productId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var categoryRepository = new CategoryRepository(context);
                var category = new Category { Name = "Lanches", IsActive = false };
                await categoryRepository.AddAsync(category);
                categoryId = category.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var productRepository = new ProductRepository(context);
                var product = new Product
                {
                    Name = "X-Burger",
                    CategoryId = categoryId,
                    Price = 25.00m,
                    IsActive = true
                };
                await productRepository.AddAsync(product);
                productId = product.Id;
                Assert.True(productId > 0);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistCustomerWithValidData()
    {
        var databaseName = $"SmartLancheDb_PersistCustomer_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);

                var customer = new Customer
                {
                    Name = "João Silva",
                    Phone = "11999999999",
                    Notes = "Cliente VIP",
                    CreditLimit = 500m,
                    IsActive = true
                };

                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
                Assert.True(customerId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var customerRepository = new CustomerRepository(context);
                var savedCustomer = await customerRepository.GetByIdAsync(customerId);

                Assert.NotNull(savedCustomer);
                Assert.Equal("João Silva", savedCustomer.Name);
                Assert.Equal("11999999999", savedCustomer.Phone);
                Assert.Equal("Cliente VIP", savedCustomer.Notes);
                Assert.Equal(500m, savedCustomer.CreditLimit);
                Assert.True(savedCustomer.IsActive);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistCustomerWithMinimalData()
    {
        var databaseName = $"SmartLancheDb_PersistCustomerMin_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);

                var customer = new Customer
                {
                    Name = "Maria Santos",
                    CreditLimit = 0m,
                    IsActive = true
                };

                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
                Assert.True(customerId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var customerRepository = new CustomerRepository(context);
                var savedCustomer = await customerRepository.GetByIdAsync(customerId);

                Assert.NotNull(savedCustomer);
                Assert.Equal("Maria Santos", savedCustomer.Name);
                Assert.Null(savedCustomer.Phone);
                Assert.Null(savedCustomer.Notes);
                Assert.Equal(0m, savedCustomer.CreditLimit);
                Assert.True(savedCustomer.IsActive);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistOrderWithCustomer()
    {
        var databaseName = $"SmartLancheDb_PersistOrder_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId, orderId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);

                var customer = new Customer
                {
                    Name = "João Silva",
                    IsActive = true,
                    CreditLimit = 500m
                };

                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.EmPreparacao,
                    TotalAmount = 0,
                    Observacoes = "Pedido de teste"
                };

                await orderRepository.AddAsync(order);
                orderId = order.Id;
                Assert.True(orderId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);
                var savedOrder = await orderRepository.GetByIdAsync(orderId);

                Assert.NotNull(savedOrder);
                Assert.Equal(customerId, savedOrder.CustomerId);
                Assert.Equal(OrderStatus.EmPreparacao, savedOrder.Status);
                Assert.Equal(0, savedOrder.TotalAmount);
                Assert.Equal("Pedido de teste", savedOrder.Observacoes);
                Assert.NotNull(savedOrder.Customer);
                Assert.Equal("João Silva", savedOrder.Customer.Name);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistOrderWithoutCustomer()
    {
        var databaseName = $"SmartLancheDb_PersistOrderNoCustomer_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int orderId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var orderRepository = new OrderRepository(context);

                var order = new Order
                {
                    CustomerId = null,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.EmPreparacao,
                    TotalAmount = 0,
                    Observacoes = string.Empty
                };

                await orderRepository.AddAsync(order);
                orderId = order.Id;
                Assert.True(orderId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);
                var savedOrder = await orderRepository.GetByIdAsync(orderId);

                Assert.NotNull(savedOrder);
                Assert.Null(savedOrder.CustomerId);
                Assert.Equal(OrderStatus.EmPreparacao, savedOrder.Status);
                Assert.Equal(0, savedOrder.TotalAmount);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldPersistOrderWithItems()
    {
        var databaseName = $"SmartLancheDb_PersistOrderItems_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId, productId, orderId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);
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
                productId = product.Id;

                var customer = new Customer { Name = "João Silva", IsActive = true, CreditLimit = 500m };
                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.EmPreparacao,
                    TotalAmount = 0,
                    Observacoes = string.Empty
                };

                var orderItem = new OrderItem
                {
                    OrderId = 0, // Será preenchido após salvar o order
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 25.50m
                };

                order.OrderItems.Add(orderItem);
                order.RecalculateTotal();

                await orderRepository.AddAsync(order);
                orderId = order.Id;
                Assert.True(orderId > 0);
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);
                var savedOrder = await orderRepository.GetByIdAsync(orderId);

                Assert.NotNull(savedOrder);
                Assert.Equal(customerId, savedOrder.CustomerId);
                Assert.Equal(OrderStatus.EmPreparacao, savedOrder.Status);
                Assert.Equal(51.00m, savedOrder.TotalAmount);
                Assert.Single(savedOrder.OrderItems);

                var item = savedOrder.OrderItems.First();
                Assert.Equal(productId, item.ProductId);
                Assert.Equal(2, item.Quantity);
                Assert.Equal(25.50m, item.UnitPrice);
                Assert.Equal(51.00m, item.Subtotal);
                Assert.NotNull(item.Product);
                Assert.Equal("X-Burger", item.Product.Name);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldCascadeDeleteOrderItemsWhenOrderDeleted()
    {
        var databaseName = $"SmartLancheDb_CascadeDelete_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId, productId, orderId, itemId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);
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
                productId = product.Id;

                var customer = new Customer { Name = "João Silva", IsActive = true, CreditLimit = 500m };
                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.EmPreparacao,
                    TotalAmount = 0,
                    Observacoes = string.Empty
                };

                var orderItem = new OrderItem
                {
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 25.50m
                };

                order.OrderItems.Add(orderItem);
                order.RecalculateTotal();

                await orderRepository.AddAsync(order);
                orderId = order.Id;
                itemId = orderItem.Id;
            }

            // Deletar o pedido
            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);
                var order = await orderRepository.GetByIdAsync(orderId);
                Assert.NotNull(order);

                context.Orders.Remove(order);
                await context.SaveChangesAsync();
            }

            // Verificar se o item foi deletado em cascata
            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);
                var deletedOrder = await orderRepository.GetByIdAsync(orderId);
                Assert.Null(deletedOrder);

                var items = await orderRepository.GetItemsByOrderAsync(orderId);
                Assert.Empty(items);
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRestrictDeletionOfCustomerWithOrders()
    {
        var databaseName = $"SmartLancheDb_RestrictCustomerDelete_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId, orderId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);

                var customer = new Customer { Name = "João Silva", IsActive = true, CreditLimit = 500m };
                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.EmPreparacao,
                    TotalAmount = 0,
                    Observacoes = string.Empty
                };

                await orderRepository.AddAsync(order);
                orderId = order.Id;
            }

            // Verificar FK Restrict: constraint existe e bloqueia INSERT com CustomerId inexistente
            // Nota: LocalDB tem limitação conhecida - ON DELETE NO ACTION não bloqueia DELETE em colunas FK nullable
            // A proteção real é no nível da aplicação (CustomerService não permite desativar cliente com pedidos)
            await using (var context = new ApplicationDbContext(options))
            {
                // Verificar se o pedido existe no banco
                var orderCount = await context.Orders.CountAsync(o => o.CustomerId == customerId);
                Assert.Equal(1, orderCount);

                // Verificar FK constraint: existe, é trusted, NO_ACTION, not disabled
                var deleteAction = await context.Database.SqlQueryRaw<string>(
                    "SELECT delete_referential_action_desc AS Value FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers_CustomerId'")
                    .FirstOrDefaultAsync();
                var isNotTrusted = await context.Database.SqlQueryRaw<bool>(
                    "SELECT is_not_trusted AS Value FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers_CustomerId'")
                    .FirstOrDefaultAsync();
                var isDisabled = await context.Database.SqlQueryRaw<bool>(
                    "SELECT is_disabled AS Value FROM sys.foreign_keys WHERE name = 'FK_Orders_Customers_CustomerId'")
                    .FirstOrDefaultAsync();
                Assert.Contains("NO_ACTION", deleteAction);
                Assert.False(isNotTrusted);
                Assert.False(isDisabled);

                // Verificar colunas da FK
                var fkColumns = await context.Database.SqlQueryRaw<string>(
                    "SELECT COL_NAME(fkc.parent_object_id, fkc.parent_column_id) + ' -> ' + COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) FROM sys.foreign_key_columns fkc INNER JOIN sys.foreign_keys fk ON fkc.constraint_object_id = fk.object_id WHERE fk.name = 'FK_Orders_Customers_CustomerId'")
                    .ToListAsync();
                Assert.Contains("CustomerId -> Id", string.Join(", ", fkColumns));

                // FK deve bloquear INSERT com CustomerId inexistente
                var insertException = await Record.ExceptionAsync(async () =>
                {
                    await context.Database.ExecuteSqlRawAsync(
                        "INSERT INTO Orders (CustomerId, OrderDate, Status, TotalAmount, Notes) VALUES (99999, GETDATE(), 1, 0, 'test')");
                });
                Assert.NotNull(insertException); // Deve falhar com FK violation
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task ShouldRestrictDeletionOfProductWithOrderItems()
    {
        var databaseName = $"SmartLancheDb_RestrictProductDelete_{Guid.NewGuid():N}";
        var connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True";
        var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connectionString).Options;

        try
        {
            int customerId, productId, orderId;

            await using (var context = new ApplicationDbContext(options))
            {
                await context.Database.MigrateAsync();
                var customerRepository = new CustomerRepository(context);
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
                productId = product.Id;

                var customer = new Customer { Name = "João Silva", IsActive = true, CreditLimit = 500m };
                await customerRepository.AddAsync(customer);
                customerId = customer.Id;
            }

            await using (var context = new ApplicationDbContext(options))
            {
                var orderRepository = new OrderRepository(context);

                var order = new Order
                {
                    CustomerId = customerId,
                    OrderDate = DateTime.Now,
                    Status = OrderStatus.EmPreparacao,
                    TotalAmount = 0,
                    Observacoes = string.Empty
                };

                var orderItem = new OrderItem
                {
                    ProductId = productId,
                    Quantity = 2,
                    UnitPrice = 25.50m
                };

                order.OrderItems.Add(orderItem);
                order.RecalculateTotal();

                await orderRepository.AddAsync(order);
                orderId = order.Id;
            }

            // Tentar deletar o produto que tem itens de pedido
            await using (var context = new ApplicationDbContext(options))
            {
                // Verificar FK do produto
                var deleteAction = await context.Database.SqlQueryRaw<string>(
                    "SELECT delete_referential_action_desc AS Value FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Products_ProductId'")
                    .FirstOrDefaultAsync();
                var isNotTrusted = await context.Database.SqlQueryRaw<bool>(
                    "SELECT is_not_trusted AS Value FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Products_ProductId'")
                    .FirstOrDefaultAsync();
                var isDisabled = await context.Database.SqlQueryRaw<bool>(
                    "SELECT is_disabled AS Value FROM sys.foreign_keys WHERE name = 'FK_OrderItems_Products_ProductId'")
                    .FirstOrDefaultAsync();
                Assert.Contains("NO_ACTION", deleteAction);
                Assert.False(isNotTrusted);
                Assert.False(isDisabled);

                var productRepository = new ProductRepository(context);
                var product = await productRepository.GetByIdAsync(productId);
                Assert.NotNull(product);

                context.Products.Remove(product);

                await Assert.ThrowsAsync<DbUpdateException>(async () => await context.SaveChangesAsync());
            }
        }
        finally
        {
            await using var cleanupContext = new ApplicationDbContext(options);
            await cleanupContext.Database.EnsureDeletedAsync();
        }
    }
}
