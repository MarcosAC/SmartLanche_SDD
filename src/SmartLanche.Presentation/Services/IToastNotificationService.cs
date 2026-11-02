using System;
using System.Collections.Generic;

namespace SmartLanche.Presentation.Services;

public interface IToastNotificationService
{
    IReadOnlyList<ToastItem> Toasts { get; }
    void ShowSuccess(string message);
    void ShowError(string message);
    void ShowWarning(string message);
    void ShowInfo(string message);
}