using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartLanche.Presentation.Commands;

namespace SmartLanche.Presentation.ViewModels;

public abstract class BaseViewModel : INotifyPropertyChanged
{
    private bool _isLoading;
    private string? _errorMessage;
    private string? _successMessage;
    private bool _wasSaved;

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

    public bool WasSaved
    {
        get => _wasSaved;
        protected set => SetProperty(ref _wasSaved, value);
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

    protected void ClearMessages()
    {
        ErrorMessage = null;
        SuccessMessage = null;
    }

    protected void SetError(string message)
    {
        ErrorMessage = message;
        SuccessMessage = null;
    }

    protected void SetSuccess(string message)
    {
        SuccessMessage = message;
        ErrorMessage = null;
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}