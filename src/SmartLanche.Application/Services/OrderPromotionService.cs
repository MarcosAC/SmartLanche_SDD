using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class OrderPromotionService : BaseService, IOrderPromotionService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IPromotionService _promotionService;

    public OrderPromotionService(
        IOrderRepository orderRepository,
        IPromotionService promotionService)
    {
        _orderRepository = orderRepository;
        _promotionService = promotionService;
    }

    public async Task<OrderDto> RecalculateWithPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pendente)
        {
            throw new InvalidOperationException("Apenas pedidos pendentes podem ser recalculados.");
        }

        var eligiblePromotions = await _promotionService.GetEligiblePromotionsAsync(orderId, DateTime.Now, cancellationToken);

        foreach (var item in order.OrderItems)
        {
            var applicablePromotions = eligiblePromotions
                .Where(p => p.IsGlobal ||
                            p.ApplicableProductIds.Contains(item.ProductId) ||
                            (item.Product != null && p.ApplicableCategoryIds.Contains(item.Product.CategoryId)))
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.Id)
                .ToList();

            if (applicablePromotions.Count > 0)
            {
                var promotion = applicablePromotions[0];
                var discount = promotion.CalculateDiscount(item.Subtotal);
                item.UnitPrice -= discount / item.Quantity;
            }
        }

        order.RecalculateTotal();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<IReadOnlyList<PromotionDto>> GetAppliedPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            return [];
        }

        var eligiblePromotions = await _promotionService.GetEligiblePromotionsAsync(orderId, DateTime.Now, cancellationToken);

        var applied = order.OrderItems
            .SelectMany(item => eligiblePromotions
                .Where(p => p.IsGlobal ||
                            p.ApplicableProductIds.Contains(item.ProductId) ||
                            (item.Product != null && p.ApplicableCategoryIds.Contains(item.Product.CategoryId)))
                .OrderByDescending(p => p.Priority)
                .ThenBy(p => p.Id)
                .Take(1))
            .DistinctBy(p => p.Id)
            .ToList();

        return applied;
    }

    public async Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default)
    {
        return await _promotionService.GetEligiblePromotionsAsync(orderId, date, cancellationToken);
    }
}