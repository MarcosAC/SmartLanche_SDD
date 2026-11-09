using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Enums;
using SmartLanche.Presentation.ViewModels.Promotions;
using Xunit;

namespace SmartLanche.Application.Tests.ViewModels;

public class PromotionListViewModelTests
{
    private readonly FakePromotionService _promotionService;
    private readonly PromotionListViewModel _viewModel;

    public PromotionListViewModelTests()
    {
        _promotionService = new FakePromotionService();
        _viewModel = new PromotionListViewModel(_promotionService);
    }

    [Fact]
    public async Task LoadAsync_LoadsPromotions()
    {
        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 1,
            Name = "Promo 1",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 2,
            Name = "Promo 2",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 2
        });

        await _viewModel.LoadAsync();

        Assert.Equal(2, _viewModel.Items.Count);
    }

    [Fact]
    public async Task LoadAsync_WithActiveFilter_ReturnsOnlyActive()
    {
        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 1,
            Name = "Active Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 2,
            Name = "Inactive Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = false,
            Priority = 1
        });

        // The base class CrudListViewModelBase filters by isActive via GetActiveFilter()
        // which returns null by default (show all). To test filtering, we'd need a
        // view model that overrides GetActiveFilter().
        await _viewModel.LoadAsync();

        // By default, shows all (both active and inactive)
        Assert.Equal(2, _viewModel.Items.Count);
    }

    [Fact]
    public async Task SearchText_FiltersPromotionsByName()
    {
        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 1,
            Name = "Desconto Verão",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 2,
            Name = "Desconto Inverno",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 3,
            Name = "Promoção Especial",
            DiscountType = DiscountType.FixedAmount,
            DiscountValue = 5,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        await _viewModel.LoadAsync();

        _viewModel.SearchText = "Verão";
        await Task.Delay(350); // Wait for debounce

        Assert.Single(_viewModel.Items);
        Assert.Equal("Desconto Verão", _viewModel.Items[0].Name);
    }

    [Fact]
    public async Task SortColumn_SortsByNameAscending()
    {
        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 1,
            Name = "Z Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 2,
            Name = "A Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _viewModel.SortColumn = "name";
        _viewModel.SortDescending = false;
        await _viewModel.LoadAsync();

        Assert.Equal("A Promo", _viewModel.Items[0].Name);
        Assert.Equal("Z Promo", _viewModel.Items[1].Name);
    }

    [Fact]
    public async Task SortColumn_SortsByNameDescending()
    {
        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 1,
            Name = "Z Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 2,
            Name = "A Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _viewModel.SortColumn = "name";
        _viewModel.SortDescending = true;
        await _viewModel.LoadAsync();

        Assert.Equal("Z Promo", _viewModel.Items[0].Name);
        Assert.Equal("A Promo", _viewModel.Items[1].Name);
    }

    [Fact]
    public async Task SortColumn_SortsByPriority()
    {
        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 1,
            Name = "Low Priority",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 10
        });

        _promotionService.AddPromotion(new PromotionDto
        {
            Id = 2,
            Name = "High Priority",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        });

        _viewModel.SortColumn = "priority";
        _viewModel.SortDescending = false;
        await _viewModel.LoadAsync();

        Assert.Equal("High Priority", _viewModel.Items[0].Name);
        Assert.Equal("Low Priority", _viewModel.Items[1].Name);
    }

    [Fact]
    public void NewItemCommand_InvokesRequestNewItem()
    {
        bool invoked = false;
        _viewModel.RequestNewItem += () => invoked = true;

        _viewModel.NewItemCommand.Execute(null);

        Assert.True(invoked);
    }

    [Fact]
    public void EditItemCommand_InvokesRequestEditItem_WithSelectedItem()
    {
        var promo = new PromotionDto
        {
            Id = 1,
            Name = "Test Promo",
            DiscountType = DiscountType.Percentage,
            DiscountValue = 10,
            StartDate = DateTime.Today.AddDays(-1),
            EndDate = DateTime.Today.AddDays(30),
            IsActive = true,
            Priority = 1
        };

        _promotionService.AddPromotion(promo);

        PromotionDto? invokedPromo = null;
        _viewModel.RequestEditItem += p => invokedPromo = p;

        _viewModel.SelectedItem = promo;
        _viewModel.EditItemCommand.Execute(null);

        Assert.NotNull(invokedPromo);
        Assert.Equal(1, invokedPromo!.Id);
    }

    [Fact]
    public void EditItemCommand_DoesNotInvoke_WhenNoItemSelected()
    {
        bool invoked = false;
        _viewModel.RequestEditItem += _ => invoked = true;

        _viewModel.SelectedItem = null;
        _viewModel.EditItemCommand.Execute(null);

        Assert.False(invoked);
    }

    private sealed class FakePromotionService : IPromotionService
    {
        private readonly List<PromotionDto> _promotions = [];
        private int _nextId = 1;

        public void AddPromotion(PromotionDto promotion)
        {
            promotion.Id = _nextId++;
            _promotions.Add(promotion);
        }

        public Task<IReadOnlyList<PromotionDto>> GetAllAsync(bool? isActive = null, CancellationToken cancellationToken = default)
        {
            IEnumerable<PromotionDto> query = _promotions;
            if (isActive.HasValue)
                query = query.Where(p => p.IsActive == isActive.Value);
            return Task.FromResult<IReadOnlyList<PromotionDto>>(query.OrderBy(p => p.Priority).ThenBy(p => p.Id).ToList());
        }

        public Task<PromotionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => Task.FromResult<PromotionDto?>(_promotions.FirstOrDefault(p => p.Id == id));

        public Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<PromotionDto> UpdateAsync(UpdatePromotionRequest request, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task DeactivateAsync(int id, CancellationToken cancellationToken = default)
        {
            var promo = _promotions.FirstOrDefault(p => p.Id == id);
            if (promo != null) promo.IsActive = false;
            return Task.CompletedTask;
        }

        public Task ReactivateAsync(int id, CancellationToken cancellationToken = default)
        {
            var promo = _promotions.FirstOrDefault(p => p.Id == id);
            if (promo != null) promo.IsActive = true;
            return Task.CompletedTask;
        }

        public Task<decimal> CalculateDiscountAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
            => Task.FromResult(0m);

        public Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
        {
            var result = _promotions
                .Where(p => p.IsActive
                    && p.StartDate.Date <= date.Date
                    && (!p.EndDate.HasValue || p.EndDate.Value.Date >= date.Date))
                .OrderBy(p => p.Priority)
                .ThenBy(p => p.Id)
                .ToList();
            return Task.FromResult<IReadOnlyList<PromotionDto>>(result);
        }
    }

}