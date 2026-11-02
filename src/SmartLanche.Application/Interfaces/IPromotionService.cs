using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IPromotionService : IEntityService<PromotionDto>
{
    Task<PromotionDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PromotionDto> CreateAsync(CreatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<PromotionDto> UpdateAsync(UpdatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<decimal> CalculateDiscountAsync(int orderId, DateTime date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionDto>> GetEligiblePromotionsAsync(int orderId, DateTime date, CancellationToken cancellationToken = default);
}