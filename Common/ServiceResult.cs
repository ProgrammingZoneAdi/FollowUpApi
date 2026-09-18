namespace FollowUpApi.Common;

public enum ServiceResultStatus
{
    Success,
    ValidationError,
    Forbidden,
    NotFound,
    Conflict,
    Error
}
public sealed class ServiceResult<T>
{
    private ServiceResult(ServiceResultStatus status, string message, T? data = default)
    {
        Status = status;
        Message = message;
        Data = data;
    }

    public ServiceResultStatus Status { get;  }

    public string Message { get; }

    public T? Data { get; }

    public bool Success => Status == ServiceResultStatus.Success;

    public static ServiceResult<T> Ok(T data, string message = "Success")
    {
        return new ServiceResult<T>(ServiceResultStatus.Success, message, data);
    }

    public static ServiceResult<T> ValidationError(string message)
    {
        return new ServiceResult<T>(ServiceResultStatus.ValidationError, message);
    }

    public static ServiceResult<T> Forbidden (string message)
    {
        return new ServiceResult<T>(ServiceResultStatus.Forbidden, message);
    }

    public static ServiceResult<T> NotFound(string message)
    {
        return new ServiceResult<T>(ServiceResultStatus.NotFound, message);
    }

    public static ServiceResult<T> Conflict(string message)
    {
        return new ServiceResult<T>(ServiceResultStatus.Conflict, message);
    }

    public static ServiceResult<T> Error(string message)
    {
        return new ServiceResult<T>(ServiceResultStatus.Error, message);
    }

}
