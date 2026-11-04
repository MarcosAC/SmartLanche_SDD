using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IOrderRepository _orderRepository;

    public PaymentService(
        IPaymentRepository paymentRepository,
        IOrderRepository orderRepository)
    {
        _paymentRepository = paymentRepository;
        _orderRepository = orderRepository;
    }

    public async Task<ProcessPaymentResult> ProcessPaymentAsync(ProcessPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return new ProcessPaymentResult(false, null, "O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pendente && order.Status != OrderStatus.Pronto)
        {
            return new ProcessPaymentResult(false, null, "Apenas pedidos pendentes ou prontos podem receber pagamento.");
        }

        try
        {
            var payment = new Payment
            {
                OrderId = request.OrderId,
                FormaPagamento = request.FormaPagamento,
                ValorPago = request.ValorPago,
                Parcelas = request.Parcelas,
                Observacoes = request.Observacoes
            };

            payment.Validate(order.TotalAmount);

            await _paymentRepository.AddAsync(payment, cancellationToken);

            var paymentDto = MapToDto(payment);

            return new ProcessPaymentResult(true, paymentDto, null);
        }
        catch (ArgumentException ex)
        {
            return new ProcessPaymentResult(false, null, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new ProcessPaymentResult(false, null, ex.Message);
        }
    }

    public async Task<PaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdAsync(id, cancellationToken);

        return payment is null ? null : MapToDto(payment);
    }

    public async Task<IReadOnlyList<PaymentDto>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        return payments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetAllAsync(PaymentFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var payments = await _paymentRepository.GetAllAsync(filter, cancellationToken);

        return payments.Select(MapToDto).ToList();
    }

    public async Task<ProcessPaymentResult> EstornarPaymentAsync(int paymentId, string motivo, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdTrackedAsync(paymentId, cancellationToken);

        if (payment is null)
        {
            return new ProcessPaymentResult(false, null, "Pagamento não encontrado.");
        }

        if (payment.Estornado)
        {
            return new ProcessPaymentResult(false, null, "Pagamento já foi estornado.");
        }

        try
        {
            payment.Estornar(motivo);

            await _paymentRepository.UpdateAsync(payment, cancellationToken);

            var paymentDto = MapToDto(payment);

            return new ProcessPaymentResult(true, paymentDto, null);
        }
        catch (InvalidOperationException ex)
        {
            return new ProcessPaymentResult(false, null, ex.Message);
        }
    }

    private static PaymentDto MapToDto(Payment payment)
    {
        return new PaymentDto(
            payment.Id,
            payment.OrderId,
            payment.FormaPagamento,
            payment.ValorPago,
            payment.Troco,
            payment.Parcelas,
            payment.ValorParcela,
            payment.DataHora,
            payment.Observacoes,
            payment.Estornado,
            payment.EstornoDataHora,
            payment.EstornoMotivo);
    }
}