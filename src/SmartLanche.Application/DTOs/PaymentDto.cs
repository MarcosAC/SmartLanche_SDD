using SmartLanche.Domain.Enums;

namespace SmartLanche.Application.DTOs;

public record PaymentDto(
    int Id,
    int OrderId,
    string? CustomerName,
    FormaPagamento PaymentMethod,
    decimal AmountPaid,
    decimal Change,
    int Installments,
    decimal InstallmentAmount,
    DateTime DateTime,
    string? Notes,
    bool IsReversed,
    DateTime? ReversedAt,
    string? ReversalReason
);

public record ProcessPaymentRequest(
    int OrderId,
    FormaPagamento PaymentMethod,
    decimal AmountPaid,
    int Installments,
    string? Notes
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
    decimal OriginalAmount,
    decimal AmountPaid,
    decimal PendingBalance,
    DateTime DateTime,
    StatusPagamentoPendente PaymentStatus,
    DateTime? PaymentDate,
    string? Notes,
    IReadOnlyList<PendingPaymentHistoryDto> History
);

public record PendingPaymentHistoryDto(
    int Id,
    int PendingPaymentId,
    decimal AmountPaid,
    DateTime DateTime,
    FormaPagamento PaymentMethod,
    string? Notes
);

public record RegisterPendingPaymentRequest(
    int PendingPaymentId,
    decimal Amount,
    FormaPagamento PaymentMethod,
    string? Notes
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
    decimal DebtBalance,
    int PendingCount,
    DateTime? LastPendingDate
);

public record PaymentFilterRequest(
    string? SearchTerm = null,
    FormaPagamento? PaymentMethod = null,
    DateTime? StartDate = null,
    DateTime? EndDate = null
);