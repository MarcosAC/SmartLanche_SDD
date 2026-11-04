using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class OrderPaymentService : BaseService, IOrderPaymentService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IPaymentService _paymentService;
    private readonly IPendingPaymentService _pendingPaymentService;
    private readonly IInventoryService _inventoryService;

    public OrderPaymentService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IPaymentService paymentService,
        IPendingPaymentService pendingPaymentService,
        IInventoryService inventoryService)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _paymentService = paymentService;
        _pendingPaymentService = pendingPaymentService;
        _inventoryService = inventoryService;
    }

    public async Task<CloseOrderWithPaymentResult> CloseWithPaymentAsync(CloseOrderWithPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return new CloseOrderWithPaymentResult(false, null, null, null, "O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pronto)
        {
            return new CloseOrderWithPaymentResult(false, null, null, null, "Apenas pedidos prontos podem ser finalizados.");
        }

        if (!order.OrderItems.Any())
        {
            return new CloseOrderWithPaymentResult(false, null, null, null, "Pedido não pode ser fechado sem itens.");
        }

        // Validate payment
        var paymentRequest = new ProcessPaymentRequest(
            request.OrderId,
            request.FormaPagamento,
            request.ValorPago,
            request.Parcelas,
            request.Observacoes);

        var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest, cancellationToken);

        if (!paymentResult.Success)
        {
            return new CloseOrderWithPaymentResult(false, null, null, null, paymentResult.ErrorMessage);
        }

        // If payment is Fiado, validate credit limit before closing
        if (request.FormaPagamento == FormaPagamento.Fiado && request.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);
            if (customer is not null)
            {
                var saldoDevedor = await _pendingPaymentService.GetSaldoDevedorAsync(request.CustomerId.Value, cancellationToken);
                var novoSaldo = saldoDevedor + order.TotalAmount;

                if (customer.CreditLimit > 0 && novoSaldo > customer.CreditLimit)
                {
                    return new CloseOrderWithPaymentResult(
                        false,
                        null,
                        null,
                        null,
                        $"Limite de crédito excedido. Saldo devedor atual: R$ {saldoDevedor:N2}, Novo pedido: R$ {order.TotalAmount:N2}, Limite: R$ {customer.CreditLimit:N2}.");
                }

                if (!customer.IsActive)
                {
                    return new CloseOrderWithPaymentResult(false, null, null, null, "Cliente inativo não pode fazer pedidos fiados.");
                }
            }
        }

        order.Finalize();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // Trigger automatic stock deduction
        await _inventoryService.BaixaAutomaticaPedidoAsync(order.Id, order.OrderItems.ToList(), cancellationToken);

        PendingPaymentDto? pendingPaymentDto = null;

        // If payment is Fiado, create a pending payment
        if (request.FormaPagamento == FormaPagamento.Fiado && request.CustomerId.HasValue)
        {
            var pendingResult = await _pendingPaymentService.CreateForOrderAsync(
                request.OrderId,
                request.CustomerId.Value,
                order.TotalAmount,
                request.Observacoes,
                cancellationToken);

            pendingPaymentDto = pendingResult;
        }

        return new CloseOrderWithPaymentResult(
            true,
            order.Id,
            paymentResult.Payment,
            pendingPaymentDto,
            null);
    }
}