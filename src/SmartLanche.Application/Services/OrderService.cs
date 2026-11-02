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
    private readonly IOrderPaymentService _orderPaymentService;

    public OrderService(
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository,
        IProductRepository productRepository,
        IInventoryService inventoryService,
        IOrderPromotionService orderPromotionService,
        IOrderPaymentService orderPaymentService)
    {
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
        _productRepository = productRepository;
        _inventoryService = inventoryService;
        _orderPromotionService = orderPromotionService;
        _orderPaymentService = orderPaymentService;
    }

    public async Task<IReadOnlyList<ProductSalesDto>> GetTopProductsAsync(int topCount = 5, int days = 7, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(null, null, DateTime.Now.AddDays(-days), null, null, null, true, cancellationToken);

        var productSales = orders
            .Where(o => o.Status == OrderStatus.Fechado)
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
            Status = OrderStatus.Aberto,
            TotalAmount = 0,
            Notes = string.Empty
        };

        await _orderRepository.AddAsync(order, cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> AddItemAsync(AddOrderItemRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Aberto)
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
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Aberto)
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
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Aberto)
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
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        if (order.Status != OrderStatus.Aberto)
        {
            throw new InvalidOperationException("Apenas pedidos abertos podem ser fechados.");
        }

        if (!order.OrderItems.Any())
        {
            throw new InvalidOperationException("Pedido não pode ser fechado sem itens.");
        }

        order.Close();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // Trigger automatic stock deduction within a transaction scope
        // This ensures order close and inventory deduction are atomic
        await _inventoryService.BaixaAutomaticaPedidoAsync(order.Id, order.OrderItems.ToList(), cancellationToken);

        return MapToDto(order);
    }

    public async Task<OrderDto> CancelAsync(CancelOrderRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        var wasDelivered = order.Status == OrderStatus.Fechado || order.Status == OrderStatus.Entregue;

        if (order.Status == OrderStatus.Cancelado)
        {
            throw new InvalidOperationException("Pedido já foi cancelado.");
        }

        order.Cancel();

        await _orderRepository.UpdateAsync(order, cancellationToken);

        // If order was delivered, restore stock
        if (wasDelivered)
        {
            await _inventoryService.EstornarBaixaPedidoAsync(order.Id, cancellationToken);
        }

        return MapToDto(order);
    }

    public async Task<CloseOrderWithPaymentResult> CloseWithPaymentAsync(CloseOrderWithPaymentRequest request, CancellationToken cancellationToken = default)
    {
        return await _orderPaymentService.CloseWithPaymentAsync(request, cancellationToken);
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