namespace Api.Services;

public enum ServiceErrorType
{
    NotFound,
    BadRequest,
    Conflict,
}

public sealed record ServiceError(ServiceErrorType Type, string Message);

public sealed record ServiceResult(ServiceError? Error)
{
    public bool Succeeded => Error is null;

    public static ServiceResult Success() => new((ServiceError?)null);

    public static ServiceResult NotFound(string message) =>
        new(new ServiceError(ServiceErrorType.NotFound, message));

    public static ServiceResult BadRequest(string message) =>
        new(new ServiceError(ServiceErrorType.BadRequest, message));

    public static ServiceResult Conflict(string message) =>
        new(new ServiceError(ServiceErrorType.Conflict, message));
}

public sealed record ServiceResult<T>(T? Value, ServiceError? Error)
{
    public bool Succeeded => Error is null;

    public static ServiceResult<T> Success(T value) => new(value, null);

    public static ServiceResult<T> NotFound(string message) =>
        new(default, new ServiceError(ServiceErrorType.NotFound, message));

    public static ServiceResult<T> BadRequest(string message) =>
        new(default, new ServiceError(ServiceErrorType.BadRequest, message));

    public static ServiceResult<T> Conflict(string message) =>
        new(default, new ServiceError(ServiceErrorType.Conflict, message));
}
