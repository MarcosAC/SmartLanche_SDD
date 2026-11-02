using Microsoft.Extensions.DependencyInjection;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Application.Services;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Tests.Services;

public class CategoryServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly ICategoryRepository _repository;
    private readonly CategoryService _service;

    public CategoryServiceTests()
    {
        var services = new ServiceCollection();
        _repository = new FakeCategoryRepository();
        services.AddSingleton(_repository);
        _serviceProvider = services.BuildServiceProvider();

        _service = new CategoryService(_serviceProvider.GetRequiredService<IServiceScopeFactory>());
    }

    [Fact]
    public async Task CreateAsync_WithValidData_CreatesCategory()
    {
        var request = new CreateCategoryRequest
        {
            Name = "  Lanches  ",
            Description = "  Lanches diversos  "
        };

        var result = await _service.CreateAsync(request);

        Assert.Equal("Lanches", result.Name);
        Assert.Equal("Lanches diversos", result.Description);
        Assert.True(result.IsActive);
        Assert.True(result.Id > 0);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task CreateAsync_WithEmptyName_ThrowsArgumentException(string name)
    {
        var request = new CreateCategoryRequest { Name = name };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithNameLongerThan100Characters_ThrowsArgumentException()
    {
        var request = new CreateCategoryRequest
        {
            Name = new string('A', 101)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithDuplicateNameIgnoringCase_ThrowsInvalidOperationException()
    {
        await _service.CreateAsync(new CreateCategoryRequest { Name = "Bebidas" });

        var request = new CreateCategoryRequest { Name = "  BEBIDAS  " };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithDescriptionLongerThan500Characters_ThrowsArgumentException()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Lanches",
            Description = new string('A', 501)
        };

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_WithEmptyDescription_StoresNullDescription()
    {
        var request = new CreateCategoryRequest
        {
            Name = "Lanches",
            Description = "   "
        };

        var result = await _service.CreateAsync(request);

        Assert.Null(result.Description);
    }

    [Fact]
    public async Task UpdateAsync_WithValidData_UpdatesCategory()
    {
        var created = await _service.CreateAsync(new CreateCategoryRequest { Name = "Lanches" });

        var result = await _service.UpdateAsync(
            new UpdateCategoryRequest
            {
                Id = created.Id,
                Name = "Porções",
                Description = "Porções diversas",
                IsActive = true
            });

        Assert.Equal("Porções", result.Name);
        Assert.Equal("Porções diversas", result.Description);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task UpdateAsync_WithDuplicateName_ThrowsInvalidOperationException()
    {
        await _service.CreateAsync(new CreateCategoryRequest { Name = "Lanches" });

        var second = await _service.CreateAsync(
            new CreateCategoryRequest
            {
                Name = "Bebidas"
            });

        var request = new UpdateCategoryRequest
        {
            Id = second.Id,
            Name = "  LANCHES ",
            IsActive = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_WithNonexistentCategory_ThrowsInvalidOperationException()
    {
        var request = new UpdateCategoryRequest
        {
            Id = 999,
            Name = "Lanches",
            IsActive = true
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UpdateAsync(request));
    }

    [Fact]
    public async Task DeactivateAsync_ExistingCategory_SetsInactive()
    {
        var created = await _service.CreateAsync(new CreateCategoryRequest { Name = "Lanches" });

        await _service.DeactivateAsync(created.Id);

        var result = await _service.GetAllAsync(isActive: false);

        Assert.Single(result);
        Assert.False(result[0].IsActive);
    }

    [Fact]
    public async Task ReactivateAsync_InactiveCategory_SetsActive()
    {
        var created = await _service.CreateAsync(new CreateCategoryRequest { Name = "Lanches" });

        await _service.DeactivateAsync(created.Id);
        await _service.ReactivateAsync(created.Id);

        var result = await _service.GetAllAsync(isActive: true);

        Assert.Single(result);
        Assert.True(result[0].IsActive);
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
    }

    private sealed class FakeCategoryRepository : ICategoryRepository
    {
        private readonly List<Category> _categories = [];
        private int _nextId = 1;

        public Task<IReadOnlyList<Category>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<Category> query = _categories;

            if (isActive.HasValue)
            {
                query = query.Where(category => category.IsActive == isActive.Value);
            }

            IReadOnlyList<Category> result = query.OrderBy(category => category.Name).ToList();

            return Task.FromResult(result);
        }

        public Task<Category?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var category = _categories.FirstOrDefault(item => item.Id == id);

            return Task.FromResult(category);
        }

        public Task<bool> ExistsByNormalizedNameAsync(string normalizedName, int? excludeId = null, CancellationToken cancellationToken = default)
        {
            var exists = _categories.Any(category => category.Id != excludeId && category.Name.Trim().ToUpperInvariant() == normalizedName);

            return Task.FromResult(exists);
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            category.Id = _nextId++;
            _categories.Add(category);

            return Task.CompletedTask;
        }

        public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            var index = _categories.FindIndex(item => item.Id == category.Id);

            if (index >= 0)
            {
                _categories[index] = category;
            }

            return Task.CompletedTask;
        }
    }
}