using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public record PaymentDto(
    int Id,
    int OrderId,
    FormaPagamento FormaPagamento,
    decimal ValorPago,
    decimal Troco,
    int Parcelas,
    decimal ValorParcela,
    DateTime DataHora,
    string? Observacoes,
    bool Estornado,
    DateTime? EstornoDataHora,
    string? EstornoMotivo
);

public record ProcessPaymentRequest(
    int OrderId,
    FormaPagamento FormaPagamento,
    decimal ValorPago,
    int Parcelas,
    string? Observacoes
);

public record ProcessPaymentResult(
    bool Success,
    PaymentDto? Payment,
    string? ErrorMessage
);

public record PendingPaymentDto(
    int Id,
    int CustomerId,
    string CustomerName,
    int OrderId,
    decimal ValorOriginal,
    decimal ValorPago,
    decimal SaldoPendente,
    DateTime DataHora,
    StatusPagamentoPendente StatusPagamento,
    DateTime? DataPagamento,
    string? Observacoes,
    IReadOnlyList<PendingPaymentHistoryDto> History
);

public record PendingPaymentHistoryDto(
    int Id,
    int PendingPaymentId,
    decimal ValorPago,
    DateTime DataHora,
    FormaPagamento FormaPagamento,
    string? Observacoes
);

public record RegisterPendingPaymentRequest(
    int PendingPaymentId,
    decimal Valor,
    FormaPagamento FormaPagamento,
    string? Observacoes
);

public record RegisterPendingPaymentResult(
    bool Success,
    PendingPaymentDto? PendingPayment,
    string? ErrorMessage
);

public record DevedorDto(
    int CustomerId,
    string CustomerName,
    string? CustomerPhone,
    decimal SaldoDevedor,
    int QuantidadePendencias,
    DateTime? UltimaPendenciaData
);

public record PaymentFilterRequest(
    string? SearchTerm = null,
    FormaPagamento? FormaPagamento = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
);