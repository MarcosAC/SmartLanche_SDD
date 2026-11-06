using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class OrderService : BaseService, IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInventoryService _inventoryService;
    private readonly IOrderPromotionService _orderPromotionService;
    private readonly IPaymentService _paymentService;
    private readonly IPendingPaymentService _pendingPaymentService;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IOrderPromotionService orderPromotionService,
        IPaymentService paymentService,
        IPendingPaymentService pendingPaymentService)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _orderPromotionService = orderPromotionService;
        _paymentService = paymentService;
        _pendingPaymentService = pendingPaymentService;
    }

    public async Task<IReadOnlyList<ProductSalesDto>> GetTopProductsAsync(int topCount = 5, int days = 7, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(null, null, DateTime.Now.AddDays(-days), null, null, null, true, false, cancellationToken);

        var productSales = orders
            .Where(o => o.Status == OrderStatus.Finalizado)
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.Product.Name)
            .Select(g => new ProductSalesDto
            {
                ProductName = g.Key,
                QuantitySold = g.Sum(i => i.Quantity)
            })
            .OrderByDescending(p => p.QuantitySold)
            .Take(topCount)
            .ToList();

        return productSales;
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(OrderFilterRequest? filter = null, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(
            filter?.Status,
            filter?.CustomerId,
            filter?.DateFrom,
            filter?.DateTo,
            filter?.SearchTerm,
            filter?.SortBy,
            filter?.SortDescending ?? true,
            filter?.ExcludeCancelled ?? true,
            cancellationToken);

        return orders.Select(MapToDto).ToList();
    }

    public async Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken);

        return order is null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.CustomerId.HasValue)
        {
            var customer = await _customerRepository.GetByIdAsync(request.CustomerId.Value, cancellationToken);

            if (customer is null)
            {
                throw new InvalidOperationException("O cliente informado não existe.");
            }

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Cliente inativo não pode fazer pedidos.");
            }
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            OrderDate = DateTime.Now,
            Status = OrderStatus.Pendente,
            TotalAmount = 0,
            Notes = string.Empty
        };

        await _orderRepository.AddAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> AddItemAsync(AddOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pendente)
        {
            throw new InvalidOperationException("Apenas pedidos abertos podem ser alterados.");
        }

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new InvalidOperationException("O produto informado não existe.");
        }

        if (!product.IsActive)
        {
            throw new InvalidOperationException("Não é permitido adicionar produto inativo ao pedido.");
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(request.Quantity));
        }

        // Merge with existing item if same product (and same combo, if applicable)
        var existingItem = order.OrderItems
            .FirstOrDefault(i => i.ProductId == request.ProductId && i.ComboId == request.ComboId);

        if (existingItem is not null)
        {
            existingItem.Quantity += request.Quantity;
            existingItem.Validate();
        }
        else
        {
            var orderItem = new OrderItem
            {
                OrderId = request.OrderId,
                ProductId = request.ProductId,
                Quantity = request.Quantity,
                UnitPrice = product.Price,
                ComboId = request.ComboId
            };

            orderItem.Validate();
            order.OrderItems.Add(orderItem);
        }

        order.RecalculateTotal();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> RemoveItemAsync(int orderId, int orderItemId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pendente)
        {
            throw new InvalidOperationException("Apenas pedidos abertos podem ser alterados.");
        }

        var item = order.OrderItems.FirstOrDefault(i => i.Id == orderItemId);

        if (item is null)
        {
            throw new InvalidOperationException("Item não encontrado no pedido.");
        }

        order.OrderItems.Remove(item);
        order.RecalculateTotal();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> UpdateItemQuantityAsync(UpdateOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pendente)
        {
            throw new InvalidOperationException("Apenas pedidos abertos podem ser alterados.");
        }

        var item = order.OrderItems.FirstOrDefault(i => i.Id == request.OrderItemId);

        if (item is null)
        {
            throw new InvalidOperationException("Item não encontrado no pedido.");
        }

        if (request.Quantity <= 0)
        {
            throw new ArgumentException("A quantidade deve ser maior que zero.", nameof(request.Quantity));
        }

        item.Quantity = request.Quantity;
        item.Validate();

        order.RecalculateTotal();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> CloseAsync(CloseOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        // Allow closing both Pendente and Pronto orders when payment is provided
        var hasPaymentInfo = request.FormaPagamento.HasValue;
        var validStatusForClose = hasPaymentInfo
            ? (order.Status == OrderStatus.Pendente || order.Status == OrderStatus.Pronto)
            : order.Status == OrderStatus.Pronto;

        if (!validStatusForClose)
        {
            throw new InvalidOperationException(hasPaymentInfo
                ? "Apenas pedidos pendentes ou prontos podem ser fechados com pagamento."
                : "Apenas pedidos prontos podem ser finalizados.");
        }

        if (!order.OrderItems.Any())
        {
            throw new InvalidOperationException("Pedido não pode ser fechado sem itens.");
        }

        // Process payment if payment info is provided
        if (hasPaymentInfo)
        {
            var paymentRequest = new ProcessPaymentRequest(
                request.OrderId,
                request.FormaPagamento.Value,
                request.ValorPago ?? 0,
                request.Parcelas ?? 1,
                request.Observacoes);

            var paymentResult = await _paymentService.ProcessPaymentAsync(paymentRequest, cancellationToken);

            if (!paymentResult.Success)
            {
                throw new InvalidOperationException(paymentResult.ErrorMessage);
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
                        throw new InvalidOperationException(
                            $"Limite de crédito excedido. Saldo devedor atual: R$ {saldoDevedor:N2}, Novo pedido: R$ {order.TotalAmount:N2}, Limite: R$ {customer.CreditLimit:N2}.");
                    }

                    if (!customer.IsActive)
                    {
                        throw new InvalidOperationException("Cliente inativo não pode fazer pedidos fiados.");
                    }
                }
            }
        }

        order.Finalize();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // Trigger automatic stock deduction
        await _inventoryService.BaixaAutomaticaPedidoAsync(order.Id, order.OrderItems.ToList(), cancellationToken);

        // If payment is Fiado, create a pending payment
        if (hasPaymentInfo && request.FormaPagamento == FormaPagamento.Fiado && request.CustomerId.HasValue)
        {
            await _pendingPaymentService.CreateForOrderAsync(
                request.OrderId,
                request.CustomerId.Value,
                order.TotalAmount,
                request.Observacoes,
                cancellationToken);
        }

        return MapToDto(order);
    }

    public async Task<OrderDto> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status == OrderStatus.Cancelado)
        {
            throw new InvalidOperationException("Pedido já foi cancelado.");
        }

        // If order is Pendente, mark as Cancelado then delete completely (no audit trail needed)
        if (order.Status == OrderStatus.Pendente)
        {
            order.Cancel();
            await _orderRepository.DeleteAsync(order, cancellationToken);
            return MapToDto(order);
        }

        // For other statuses, set to Cancelado and restore stock if needed
        var wasDelivered = order.Status == OrderStatus.Finalizado || order.Status == OrderStatus.Pronto;

        order.Cancel();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // If order was delivered/ready, restore stock
        if (wasDelivered)
        {
            await _inventoryService.EstornarBaixaPedidoAsync(order.Id, cancellationToken);
        }

        return MapToDto(order);
    }

    public async Task DeleteAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        await _orderRepository.DeleteAsync(order, cancellationToken);
    }

    public async Task<OrderDto> StartPreparationAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pendente)
        {
            throw new InvalidOperationException("Apenas pedidos pendentes podem iniciar preparação.");
        }

        order.StartPreparation();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> MarkAsReadyAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.EmPreparacao)
        {
            throw new InvalidOperationException("Apenas pedidos em preparo podem ser marcados como prontos.");
        }

        order.MarkAsReady();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> FinalizeAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdTrackedAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Pronto)
        {
            throw new InvalidOperationException("Apenas pedidos prontos podem ser finalizados.");
        }

        if (!order.OrderItems.Any())
        {
            throw new InvalidOperationException("Pedido não pode ser finalizado sem itens.");
        }

        order.Finalize();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // Trigger automatic stock deduction
        await _inventoryService.BaixaAutomaticaPedidoAsync(order.Id, order.OrderItems.ToList(), cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> RecalculateWithPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _orderPromotionService.RecalculateWithPromotionsAsync(orderId, cancellationToken);
    }

    public async Task<IReadOnlyList<PromotionDto>> GetAppliedPromotionsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _orderPromotionService.GetAppliedPromotionsAsync(orderId, cancellationToken);
    }
}