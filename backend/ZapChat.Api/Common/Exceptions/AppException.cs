namespace ZapChat.Api.Common.Exceptions;

public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public static AppException BadRequest(string message)
        => new(message, 400);

    public static AppException Unauthorized(string message = "Không có quyền truy cập.")
        => new(message, 401);

    public static AppException Forbidden(string message = "Bị từ chối truy cập.")
        => new(message, 403);

    public static AppException NotFound(string message = "Không tìm thấy.")
        => new(message, 404);

    public static AppException Conflict(string message)
        => new(message, 409);
}