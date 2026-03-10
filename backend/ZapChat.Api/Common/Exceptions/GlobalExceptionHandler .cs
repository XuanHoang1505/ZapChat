using System.Text.Json;
using ZapChat.Api.Common.Response;

namespace ZapChat.Api.Common.Exceptions;

public class GlobalExceptionHandler : IMiddleware
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        => _logger = logger;

    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning("AppException: {Message}", ex.Message);
            await WriteResponseAsync(context, ex.StatusCode, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponseAsync(context, 500, "Lỗi hệ thống, vui lòng thử lại.");
        }
    }

    private static async Task WriteResponseAsync(
        HttpContext context, int statusCode, string message)
    {
        context.Response.StatusCode  = statusCode;
        context.Response.ContentType = "application/json";

        var response = ApiResponse<object>.Fail(message);
        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(json);
    }
}