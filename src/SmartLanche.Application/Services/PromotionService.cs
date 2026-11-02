using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class PromotionService : CrudServiceBase<Promotion, PromotionDto, CreatePromotionRequest, UpdatePromotionRequest, IPromotionRepository>, IPromotionService
{
    private readonly IProductRepository _productRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<PromotionService> _logger;

    public PromotionService(
        IServiceScopeFactory scopeFactory,
        IProductRepository productRepository,
        ICategoryRepository categoryRepository,
        IOrderRepository orderRepository,
        ILogger<PromotionService> logger)
        : base(scopeFactory)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _categoryRepository = categoryRepository ?? throw new ArgumentNullException(nameof(categoryRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override string GetEntityName() => "Promoção";

    protected override async Task<IReadOnlyList<Promotion>> GetAllEntitiesAsync(IPromotionRepository repository, bool? isActive, CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(isActive, cancellationToken);
    }

    protected override async Task<Promotion?> GetEntityByIdAsync(IPromotionRepository repository, int id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    protected override async Task AddEntityAsync(IPromotionRepository repository, Promotion entity, CancellationToken cancellationToken)
    {
        await repository.AddAsync(entity, cancellationToken);
    }

    protected override async Task UpdateEntityAsync(IPromotionRepository repository, Promotion entity, CancellationToken cancellationToken)
    {
        await repository.UpdateAsync(entity, cancellationToken);
    }

    protected override PromotionDto MapToDto(Promotion entity)
    {
        return new PromotionDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            DiscountType = entity.DiscountType,
            DiscountValue = entity.DiscountValue,
            StartDate = entity.StartDate,
            EndDate = entity.EndDate,
            IsActive = entity.IsActive,
            Priority = entity.Priority,
            ApplicableProductIds = entity.ApplicableProducts.Select(p => p.Id).ToList(),
            ApplicableCategoryIds = entity.ApplicableCategories.Select(c => c.Id).ToList()
        };
    }

    protected override async Task<Promotion> CreateEntityAsync(IPromotionRepository repository, CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        var promotion = new Promotion
        {
            Name = request.Name,
            Description = request.Description,
            DiscountType = request.DiscountType,
            DiscountValue = request.DiscountValue,
            StartDate = request.StartDate.Date,
            EndDate = request.EndDate?.Date,
            IsActive = true,
            Priority = request.Priority
        };

        // Load applicable products
        foreach (var productId in request.ApplicableProductIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product != null)
            {
                promotion.ApplicableProducts.Add(product);
            }
        }

        // Load applicable categories
        foreach (var categoryId in request.ApplicableCategoryIds)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category != null)
            {
                promotion.ApplicableCategories.Add(category);
            }
        }

        promotion.Validate();

        _logger.LogInformation("Promoção criada com sucesso: {PromotionId} - {Name}", promotion.Id, promotion.Name);
        return promotion;
    }

    protected override async Task UpdateEntityAsync(IPromotionRepository repository, Promotion entity, UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.DiscountType = request.DiscountType;
        entity.DiscountValue = request.DiscountValue;
        entity.StartDate = request.StartDate.Date;
        entity.EndDate = request.EndDate?.Date;
        entity.IsActive = request.IsActive;
        entity.Priority = request.Priority;

        // Update many-to-many relationships
        entity.ApplicableProducts.Clear();
        entity.ApplicableCategories.Clear();

        // Load applicable products
        foreach (var productId in request.ApplicableProductIds)
        {
            var product = await _productRepository.GetByIdAsync(productId, cancellationToken);
            if (product != null)
            {
                entity.ApplicableProducts.Add(product);
            }
        }

        // Load applicable categories
        foreach (var categoryId in request.ApplicableCategoryIds)
        {
            var category = await _categoryRepository.GetByIdAsync(categoryId, cancellationToken);
            if (category != null)
            {
                entity.ApplicableCategories.Add(category);
            }
        }

        entity.Validate();
    }

    protected override async Task ValidateCreateRequestAsync(CreatePromotionRequest request, CancellationToken cancellationToken)
    {
        await ValidatePromotionRequestAsync(request.Name, request.DiscountType, request.DiscountValue, request.StartDate, request.EndDate, request.Priority, cancellationToken);
    }

    protected override async Task ValidateUpdateRequestAsync(UpdatePromotionRequest request, CancellationToken cancellationToken)
    {
        await ValidatePromotionRequestAsync(request.Name, request.DiscountType, request.DiscountValue, request.StartDate, request.EndDate, request.Priority, cancellationToken);
    }

    private static Task ValidatePromotionRequestAsync(string name, DiscountType discountType, decimal discountValue, DateTime startDate, DateTime? endDate, int priority, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("O nome da promoção é obrigatório.", nameof(name));
        }

        if (name.Trim().Length > 100)
        {
            throw new ArgumentException("O nome da promoção deve possuir no máximo 100 caracteres.", nameof(name));
        }

        if (discountValue <= 0)
        {
            throw new ArgumentException("O valor do desconto deve ser maior que zero.", nameof(discountValue));
        }

        if (discountType == DiscountType.Percentage && discountValue > 100)
        {
            throw new ArgumentException("O desconto percentual não pode exceder 100%.", nameof(discountValue));
        }

        if (endDate.HasValue && endDate.Value.Date < startDate.Date)
        {
            throw new ArgumentException("A data de término não pode ser anterior à data de início.", nameof(endDate));
        }

        if (priority < 0)
        {
            throw new ArgumentException("A prioridade não pode ser negativa.", nameof(priority));
        }

        return Task.CompletedTask;
    }

    // Additional methods specific to PromotionService
    public async Task<decimal> CalculateDiscountAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await ExecuteInScopeAsync<decimal>(async (scope, ct) =>
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var promotionRepository = scope.ServiceProvider.GetRequiredService<IPromotionRepository>();

            var order = await orderRepository.GetByIdAsync(orderId, ct);
            if (order == null)
            {
                throw new InvalidOperationException($"Pedido com ID {orderId} não encontrado");
            }

            // Get all active promotions valid for the given date
            var promotions = await promotionRepository.GetAllAsync(true, ct);
            var eligiblePromotions = promotions
                .Where(p => p.IsValidOn(date))
                .OrderByDescending(p => p.Priority)
                .ToList();

            if (!eligiblePromotions.Any())
            {
                return 0m;
            }

            decimal totalDiscount = 0m;

            foreach (var orderItem in order.OrderItems)
            {
                var product = orderItem.Product;
                if (product == null)
                {
                    continue;
                }

                // Check if product belongs to any applicable category
                var productCategories = new List<Category>();
                if (product.Category != null)
                {
                    productCategories.Add(product.Category);
                }

                // Find eligible promotions for this order item
                foreach (var promotion in eligiblePromotions)
                {
                    bool isApplicable = false;

                    // Global promotion (applies to all products)
                    if (promotion.IsGlobal)
                    {
                        isApplicable = true;
                    }
                    // Promotion applies to specific products
                    else if (promotion.ApplicableProducts.Any(p => p.Id == product.Id))
                    {
                        isApplicable = true;
                    }
                    // Promotion applies to specific categories
                    else if (promotion.ApplicableCategories.Any(c => productCategories.Any(pc => pc.Id == c.Id)))
                    {
                        isApplicable = true;
                    }

                    if (isApplicable)
                    {
                        var itemAmount = orderItem.Subtotal;
                        var discount = promotion.CalculateDiscount(itemAmount);
                        totalDiscount += discount;
                        _logger.LogDebug("Promoção {PromotionId} aplicada ao item {ProductName}: {Discount:C}", promotion.Id, product.Name, discount);
                        // Apply only the highest priority promotion per item
                        break;
                    }
                }
            }

            return Math.Round(totalDiscount, 2, MidpointRounding.AwayFromZero);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await ExecuteInScopeAsync<IReadOnlyList<PromotionDto>>(async (scope, ct) =>
        {
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var promotionRepository = scope.ServiceProvider.GetRequiredService<IPromotionRepository>();

            var order = await orderRepository.GetByIdAsync(orderId, ct);
            if (order == null)
            {
                throw new InvalidOperationException($"Pedido com ID {orderId} não encontrado");
            }

            // Get all active promotions valid for the given date
            var promotions = await promotionRepository.GetAllAsync(true, ct);
            var eligiblePromotions = promotions
                .Where(p => p.IsValidOn(date))
                .OrderByDescending(p => p.Priority)
                .ToList();

            if (!eligiblePromotions.Any())
            {
                return Array.Empty<PromotionDto>();
            }

            var result = new List<PromotionDto>();

            foreach (var orderItem in order.OrderItems)
            {
                var product = orderItem.Product;
                if (product == null)
                {
                    continue;
                }

                // Check if product belongs to any applicable category
                var productCategories = new List<Category>();
                if (product.Category != null)
                {
                    productCategories.Add(product.Category);
                }

                // Find eligible promotions for this order item
                foreach (var promotion in eligiblePromotions)
                {
                    bool isApplicable = false;

                    // Global promotion (applies to all products)
                    if (promotion.IsGlobal)
                    {
                        isApplicable = true;
                    }
                    // Promotion applies to specific products
                    else if (promotion.ApplicableProducts.Any(p => p.Id == product.Id))
                    {
                        isApplicable = true;
                    }
                    // Promotion applies to specific categories
                    else if (promotion.ApplicableCategories.Any(c => productCategories.Any(pc => pc.Id == c.Id)))
                    {
                        isApplicable = true;
                    }

                    if (isApplicable)
                    {
                        result.Add(MapToDto(promotion));
                        // Apply only the highest priority promotion per item
                        break;
                    }
                }
            }

            return result.DistinctBy(p => p.Id).ToList();
        }, cancellationToken);
    }
}