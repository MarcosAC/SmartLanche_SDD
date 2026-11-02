using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IComboService : IEntityService<ComboDto>
{
    Task<ComboDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<ComboDto> CreateAsync(CreateComboRequest request, CancellationToken cancellationToken = default);

    Task<ComboDto> UpdateAsync(UpdateComboRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ComboDto>> GetAvailableCombosAsync(CancellationToken cancellationToken = default);

    Task<OrderDto> AddComboToOrderAsync(int orderId, int comboId, int quantity, CancellationToken cancellationToken = default);
}