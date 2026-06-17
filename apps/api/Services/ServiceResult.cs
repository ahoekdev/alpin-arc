using System.Diagnostics.CodeAnalysis;

namespace Api.Services;

public enum ServiceErrorType
{
    NotFound,
    BadRequest,
    Conflict,
}

public sealed record ServiceError(ServiceErrorType Type, string Message);

public sealed record ServiceResult
{
    private ServiceResult(ServiceError? error)
    {
        Error = error;
    }

    public ServiceError? Error { get; }

    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Error is null;

    public static ServiceResult Success() => new((ServiceError?)null);

    public static ServiceResult NotFound(string message) =>
        new(new ServiceError(ServiceErrorType.NotFound, message));

    public static ServiceResult BadRequest(string message) =>
        new(new ServiceError(ServiceErrorType.BadRequest, message));

    public static ServiceResult Conflict(string message) =>
        new(new ServiceError(ServiceErrorType.Conflict, message));
}

public sealed record ServiceResult<T>
    where T : notnull
{
    private ServiceResult(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ServiceError? Error { get; }

    [MemberNotNullWhen(true, nameof(Value))]
    [MemberNotNullWhen(false, nameof(Error))]
    public bool Succeeded => Error is null;

    public static ServiceResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);

        return new(value, null);
    }

    public static ServiceResult<T> NotFound(string message) =>
        new(default, new ServiceError(ServiceErrorType.NotFound, message));

    public static ServiceResult<T> BadRequest(string message) =>
        new(default, new ServiceError(ServiceErrorType.BadRequest, message));

    public static ServiceResult<T> Conflict(string message) =>
        new(default, new ServiceError(ServiceErrorType.Conflict, message));
}
