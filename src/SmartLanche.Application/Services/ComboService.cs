using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;

namespace SmartLanche.Application.Services;

public class ComboService : CrudServiceBase<Combo, ComboDto, CreateComboRequest, UpdateComboRequest, IComboRepository>, IComboService
{
    private readonly IProductRepository _productRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderService _orderService;
    private readonly ILogger<ComboService> _logger;

    public ComboService(
        IServiceScopeFactory scopeFactory,
        IProductRepository productRepository,
        IOrderRepository orderRepository,
        IOrderService orderService,
        ILogger<ComboService> logger)
        : base(scopeFactory)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _orderRepository = orderRepository ?? throw new ArgumentNullException(nameof(orderRepository));
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    protected override string GetEntityName() => "Combo";

    protected override async Task<IReadOnlyList<Combo>> GetAllEntitiesAsync(IComboRepository repository, bool? isActive, CancellationToken cancellationToken)
    {
        return await repository.GetAllAsync(isActive, cancellationToken);
    }

    protected override async Task<Combo?> GetEntityByIdAsync(IComboRepository repository, int id, CancellationToken cancellationToken)
    {
        return await repository.GetByIdAsync(id, cancellationToken);
    }

    protected override async Task AddEntityAsync(IComboRepository repository, Combo entity, CancellationToken cancellationToken)
    {
        await repository.AddAsync(entity, cancellationToken);
    }

    protected override async Task UpdateEntityAsync(IComboRepository repository, Combo entity, CancellationToken cancellationToken)
    {
        await repository.UpdateAsync(entity, cancellationToken);
    }

    protected override ComboDto MapToDto(Combo entity)
    {
        return new ComboDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description,
            Price = entity.Price,
            IsActive = entity.IsActive,
            IsAvailable = entity.IsAvailable,
            Items = entity.ComboItems
                .Where(ci => ci.Product != null)
                .Select(ci => new ComboItemDto
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product!.Name,
                    Quantity = ci.Quantity
                })
                .ToList()
        };
    }

    protected override async Task<Combo> CreateEntityAsync(IComboRepository repository, CreateComboRequest request, CancellationToken cancellationToken)
    {
        var combo = new Combo
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        };

        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId, cancellationToken);
            if (product == null)
            {
                throw new InvalidOperationException($"Produto com ID {itemRequest.ProductId} não encontrado");
            }

            combo.ComboItems.Add(new ComboItem
            {
                ProductId = itemRequest.ProductId,
                Quantity = itemRequest.Quantity,
                Product = product
            });
        }

        combo.Validate();

        _logger.LogInformation("Combo criado com sucesso: {ComboId} - {Name}", combo.Id, combo.Name);
        return combo;
    }

    protected override async Task UpdateEntityAsync(IComboRepository repository, Combo entity, UpdateComboRequest request, CancellationToken cancellationToken)
    {
        entity.Name = request.Name;
        entity.Description = request.Description;
        entity.Price = request.Price;
        entity.IsActive = request.IsActive;

        // Remove existing items
        entity.ComboItems.Clear();

        // Add new items
        foreach (var itemRequest in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(itemRequest.ProductId, cancellationToken);
            if (product == null)
            {
                throw new InvalidOperationException($"Produto com ID {itemRequest.ProductId} não encontrado");
            }

            entity.ComboItems.Add(new ComboItem
            {
                ComboId = entity.Id,
                ProductId = itemRequest.ProductId,
                Quantity = itemRequest.Quantity,
                Product = product
            });
        }

        entity.Validate();
    }

    protected override async Task ValidateCreateRequestAsync(CreateComboRequest request, CancellationToken cancellationToken)
    {
        await ValidateComboRequestAsync(request.Name, request.Price, request.Items, cancellationToken);
    }

    protected override async Task ValidateUpdateRequestAsync(UpdateComboRequest request, CancellationToken cancellationToken)
    {
        await ValidateComboRequestAsync(request.Name, request.Price, request.Items, cancellationToken);
    }

    private static Task ValidateComboRequestAsync(string name, decimal price, IReadOnlyList<CreateComboItemRequest> items, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nome do combo é obrigatório");
        }

        if (price <= 0)
        {
            throw new ArgumentException("Preço do combo deve ser maior que zero");
        }

        if (items.Count < 2)
        {
            throw new ArgumentException("Combo deve ter pelo menos 2 itens componentes");
        }

        foreach (var item in items)
        {
            if (item.Quantity <= 0)
            {
                throw new ArgumentException("Quantidade de cada item deve ser maior que zero");
            }
        }

        return Task.CompletedTask;
    }

    // Additional methods specific to ComboService
    public async Task<IReadOnlyList<ComboDto>> GetAvailableCombosAsync(CancellationToken cancellationToken = default)
    {
        return await ExecuteInScopeAsync(async (repository, ct) =>
        {
            var combos = await repository.GetAvailableAsync(ct);
            return combos.Select(MapToDto).ToList();
        }, cancellationToken);
    }

    public async Task<OrderDto> AddComboToOrderAsync(int orderId, int comboId, int quantity, CancellationToken cancellationToken = default)
    {
        try
        {
            var combo = await ExecuteInScopeAsync(async (repository, ct) =>
            {
                return await repository.GetWithItemsAsync(comboId, ct);
            }, cancellationToken);

            if (combo == null)
            {
                throw new InvalidOperationException($"Combo com ID {comboId} não encontrado");
            }

            if (!combo.IsAvailable)
            {
                throw new InvalidOperationException("Combo não está disponível (um ou mais produtos inativos)");
            }

            var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            if (order == null)
            {
                throw new InvalidOperationException($"Pedido com ID {orderId} não encontrado");
            }

            // Add each component as an order item
            foreach (var comboItem in combo.ComboItems)
            {
                var product = comboItem.Product ?? await _productRepository.GetByIdAsync(comboItem.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new InvalidOperationException($"Produto com ID {comboItem.ProductId} não encontrado");
                }

                if (!product.IsActive)
                {
                    throw new InvalidOperationException($"Produto {product.Name} está inativo");
                }

                await _orderService.AddItemAsync(new AddOrderItemRequest
                {
                    OrderId = orderId,
                    ProductId = comboItem.ProductId,
                    Quantity = comboItem.Quantity * quantity,
                    ComboId = comboId
                }, cancellationToken);
            }

            var updatedOrder = await _orderRepository.GetByIdAsync(orderId, cancellationToken);
            _logger.LogInformation("Combo {ComboId} adicionado ao pedido {OrderId} (quantidade: {Quantity})", comboId, orderId, quantity);
            return MapOrderToDto(updatedOrder!);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao adicionar combo {ComboId} ao pedido {OrderId}", comboId, orderId);
            throw;
        }
    }

    private static OrderDto MapOrderToDto(Order order)
    {
        return new OrderDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            CustomerName = order.Customer?.Name,
            Status = order.Status,
            OrderDate = order.OrderDate,
            TotalAmount = order.TotalAmount,
            Items = order.OrderItems.Select(oi => new OrderItemDto
            {
                Id = oi.Id,
                OrderId = oi.OrderId,
                ProductId = oi.ProductId,
                ProductName = oi.Product?.Name ?? string.Empty,
                Quantity = oi.Quantity,
                UnitPrice = oi.UnitPrice,
                Subtotal = oi.Subtotal
            }).ToList()
        };
    }
}