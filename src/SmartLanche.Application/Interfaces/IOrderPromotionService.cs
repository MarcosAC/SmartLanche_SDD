using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IOrderPromotionService
{
    Task<OrderDto> RecalculateWithPromotionsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionDto>> GetAppliedPromotionsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default);
}