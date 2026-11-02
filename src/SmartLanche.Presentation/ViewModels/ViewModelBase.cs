using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Presentation.Commands;

namespace SmartLanche.Presentation.ViewModels;

public abstract class ViewModelBase : INotifyPropertyChanged
{
    private bool _isLoading;
    private string? _errorMessage;
    private string? _successMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => _isLoading;
        protected set
        {
            if (SetProperty(ref _isLoading, value))
            {
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        protected set => SetProperty(ref _errorMessage, value);
    }

    public string? SuccessMessage
    {
        get => _successMessage;
        protected set => SetProperty(ref _successMessage, value);
    }

    protected void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>
    /// Raises the PropertyChanged event for the specified property.
    /// </summary>
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    /// <summary>
    /// Public wrapper for OnPropertyChanged to allow calling from lambdas/closures.
    /// </summary>
    public void RaisePropertyChanged(string propertyName)
    {
        OnPropertyChanged(propertyName);
    }

    protected async Task ExecuteWithLoadingAsync(Func<Task> action)
    {
        if (IsLoading)
        {
            return;
        }

        try
        {
            IsLoading = true;
            ClearMessages();
            await action();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }
}