using SmartLanche.Application.DTOs;
using SmartLanche.Application.Interfaces;
using SmartLanche.Domain.Entities;
using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.Services;

public class PendingPaymentService : IPendingPaymentService
{
    private readonly IPendingPaymentRepository _pendingPaymentRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly ICustomerRepository _customerRepository;

    public PendingPaymentService(
        IPendingPaymentRepository pendingPaymentRepository,
        IOrderRepository orderRepository,
        ICustomerRepository customerRepository)
    {
        _pendingPaymentRepository = pendingPaymentRepository;
        _orderRepository = orderRepository;
        _customerRepository = customerRepository;
    }

    public async Task<RegisterPendingPaymentResult> RegisterPaymentAsync(RegisterPendingPaymentRequest request, CancellationToken cancellationToken = default)
    {
        var pendingPayment = await _pendingPaymentRepository.GetByIdTrackedAsync(request.PendingPaymentId, cancellationToken);

        if (pendingPayment is null)
        {
            return new RegisterPendingPaymentResult(false, null, "Pendência não encontrada.");
        }

        try
        {
            pendingPayment.RegistrarPagamento(request.Valor, request.FormaPagamento, request.Notes);

            await _pendingPaymentRepository.UpdateAsync(pendingPayment, cancellationToken);

            var dto = MapToDto(pendingPayment);

            return new RegisterPendingPaymentResult(true, dto, null);
        }
        catch (ArgumentException ex)
        {
            return new RegisterPendingPaymentResult(false, null, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return new RegisterPendingPaymentResult(false, null, ex.Message);
        }
    }

    public async Task<PendingPaymentDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var pendingPayment = await _pendingPaymentRepository.GetByIdAsync(id, cancellationToken);

        return pendingPayment is null ? null : MapToDto(pendingPayment);
    }

    public async Task<PendingPaymentDto?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var pendingPayment = await _pendingPaymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        return pendingPayment is null ? null : MapToDto(pendingPayment);
    }

    public async Task<IReadOnlyList<PendingPaymentDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var pendingPayments = await _pendingPaymentRepository.GetByCustomerIdAsync(customerId, cancellationToken);

        return pendingPayments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PendingPaymentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var pendingPayments = await _pendingPaymentRepository.GetAllAsync(cancellationToken);

        return pendingPayments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PendingPaymentDto>> GetByStatusAsync(StatusPagamentoPendente status, CancellationToken cancellationToken = default)
    {
        var pendingPayments = await _pendingPaymentRepository.GetByStatusAsync(status, cancellationToken);

        return pendingPayments.Select(MapToDto).ToList();
    }

    public async Task<IReadOnlyList<PendingPaymentHistoryDto>> GetHistoryAsync(int pendingPaymentId, CancellationToken cancellationToken = default)
    {
        var pendingPayment = await _pendingPaymentRepository.GetByIdAsync(pendingPaymentId, cancellationToken);

        if (pendingPayment is null)
        {
            return [];
        }

        return pendingPayment.History
            .OrderByDescending(h => h.DataHora)
            .Select(h => new PendingPaymentHistoryDto(
                h.Id,
                h.PendingPaymentId,
                h.ValorPago,
                h.DataHora,
                h.FormaPagamento,
                h.Notes))
            .ToList();
    }

    public async Task<PendingPaymentDto> CreateForOrderAsync(int orderId, int customerId, decimal valor, string? observacoes, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(orderId, cancellationToken);

        if (order is null)
        {
            throw new InvalidOperationException("O pedido informado não existe.");
        }

        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);

        if (customer is null)
        {
            throw new InvalidOperationException("O cliente informado não existe.");
        }

        // Check if there's already a pending payment for this order
        var existing = await _pendingPaymentRepository.GetByOrderIdAsync(orderId, cancellationToken);

        if (existing is not null)
        {
            throw new InvalidOperationException("Já existe uma pendência para este pedido.");
        }

        var pendingPayment = new PendingPayment
        {
            OrderId = orderId,
            CustomerId = customerId,
            ValorOriginal = valor,
            ValorPago = 0m,
            DataHora = DateTime.Now,
            StatusPagamento = StatusPagamentoPendente.NaoPago,
            Notes = observacoes
        };

        await _pendingPaymentRepository.AddAsync(pendingPayment, cancellationToken);

        return MapToDto(pendingPayment);
    }

    private static PendingPaymentDto MapToDto(PendingPayment pp)
    {
        return new PendingPaymentDto(
            pp.Id,
            pp.CustomerId,
            pp.Customer?.Name ?? string.Empty,
            pp.OrderId,
            pp.ValorOriginal,
            pp.ValorPago,
            pp.SaldoPendente,
            pp.DataHora,
            pp.StatusPagamento,
            pp.DataPagamento,
            pp.Notes,
            pp.History
                .OrderByDescending(h => h.DataHora)
                .Select(h => new PendingPaymentHistoryDto(
                    h.Id,
                    h.PendingPaymentId,
                    h.ValorPago,
                    h.DataHora,
                    h.FormaPagamento,
                    h.Notes))
                .ToList());
    }

    public async Task<decimal> GetSaldoDevedorAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var pendingPayments = await _pendingPaymentRepository.GetByCustomerIdAsync(customerId, cancellationToken);

        return pendingPayments
            .Where(pp => pp.StatusPagamento == StatusPagamentoPendente.NaoPago)
            .Sum(pp => pp.SaldoPendente);
    }

    public async Task<IReadOnlyList<DevedorDto>> GetDevedoresAsync(CancellationToken cancellationToken = default)
    {
        var allPending = await _pendingPaymentRepository.GetAllAsync(cancellationToken);

        var devedores = allPending
            .Where(pp => pp.StatusPagamento == StatusPagamentoPendente.NaoPago)
            .GroupBy(pp => pp.Customer)
            .Where(g => g.Key is not null && g.Key.IsActive)
            .Select(g => new DevedorDto(
                g.Key!.Id,
                g.Key.Name,
                g.Key.Phone,
                g.Sum(pp => pp.SaldoPendente),
                g.Count(),
                g.Max(pp => pp.DataHora)))
            .OrderByDescending(d => d.SaldoDevedor)
            .ToList();

        return devedores;
    }
}