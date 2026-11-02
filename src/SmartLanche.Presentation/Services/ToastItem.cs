using System;

namespace SmartLanche.Presentation.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public record ToastItem(
    Guid Id,
    ToastType Type,
    string Message,
    DateTime Timestamp,
    int AutoDismissDelayMs
);