namespace SmartLanche.Application.Common;

public readonly struct Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    private Result(T value)
    {
        _value = value;
        _error = null;
        IsSuccess = true;
    }

    private Result(string error)
    {
        _value = default;
        _error = error;
        IsSuccess = false;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;

    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public string Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful result.");

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(string error) => new(error);

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(string error) => Failure(error);
}

public readonly struct Result
{
    private readonly string? _error;

    private Result(string? error)
    {
        _error = error;
        IsSuccess = string.IsNullOrEmpty(error);
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Error => IsFailure ? _error! : throw new InvalidOperationException("No error on success.");

    public static Result Success() => new(null);
    public static Result Failure(string error) => new(error);

    public static implicit operator Result(string error) => Failure(error);
}