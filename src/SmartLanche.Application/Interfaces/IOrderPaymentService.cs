using SmartLanche.Application.DTOs;

namespace SmartLanche.Application.Interfaces;

public interface IOrderPaymentService
{
    Task<CloseOrderWithPaymentResult> CloseWithPaymentAsync(CloseOrderWithPaymentRequest request, CancellationToken cancellationToken = default);
}