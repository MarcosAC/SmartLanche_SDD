using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Windows.Threading;
using SmartLanche.Presentation.Commands;

namespace SmartLanche.Presentation.Services;

public class ToastNotificationService : IToastNotificationService
{
    private const int MaxToasts = 5;
    private readonly ObservableCollection<ToastItem> _toasts = [];
    private readonly DispatcherTimer _cleanupTimer;

    public IReadOnlyList<ToastItem> Toasts => _toasts;
    public ICommand DismissCommand { get; }

    public ToastNotificationService()
    {
        DismissCommand = new RelayCommand(Dismiss);

        _cleanupTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _cleanupTimer.Tick += CleanupExpiredToasts;
        _cleanupTimer.Start();
    }

    public void ShowSuccess(string message) => AddToast(ToastType.Success, message, 4000);
    public void ShowError(string message) => AddToast(ToastType.Error, message, 15000);
    public void ShowWarning(string message) => AddToast(ToastType.Warning, message, 8000);
    public void ShowInfo(string message) => AddToast(ToastType.Info, message, 4000);

    public void Dismiss(object? parameter)
    {
        if (parameter is Guid id)
        {
            var toast = _toasts.FirstOrDefault(t => t.Id == id);
            if (toast is not null)
            {
                _toasts.Remove(toast);
            }
        }
    }

    private void AddToast(ToastType type, string message, int autoDismissDelayMs)
    {
        if (_toasts.Count >= MaxToasts)
        {
            _toasts.RemoveAt(0);
        }

        var toast = new ToastItem(Guid.NewGuid(), type, message, DateTime.Now, autoDismissDelayMs);
        _toasts.Add(toast);
    }

    private void CleanupExpiredToasts(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var expired = _toasts.Where(t => (now - t.Timestamp).TotalMilliseconds >= t.AutoDismissDelayMs).ToList();
        foreach (var toast in expired)
        {
            _toasts.Remove(toast);
        }
    }
}