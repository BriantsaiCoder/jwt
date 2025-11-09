using System.Net;
using JwtAuthApi.DTOs;
using Microsoft.AspNetCore.Diagnostics;

namespace JwtAuthApi.Middleware;

/// <summary>
/// 全域錯誤處理中介軟體
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
  private readonly ILogger<GlobalExceptionHandler> _logger;

  public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
  {
    _logger = logger;
  }

  public async ValueTask<bool> TryHandleAsync(
      HttpContext httpContext,
      Exception exception,
      CancellationToken cancellationToken)
  {
    var traceId = httpContext.TraceIdentifier;
    var statusCode = exception switch
    {
      UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
      ArgumentException => (int)HttpStatusCode.BadRequest,
      KeyNotFoundException => (int)HttpStatusCode.NotFound,
      _ => (int)HttpStatusCode.InternalServerError
    };

    var errorResponse = new ErrorResponse
    {
      StatusCode = statusCode,
      Message = exception.Message,
      TraceId = traceId,
      Timestamp = DateTime.UtcNow
    };

    // 記錄錯誤
    if (statusCode >= 500)
    {
      _logger.LogError(exception,
          "發生內部伺服器錯誤。TraceId: {TraceId}, Message: {Message}, StackTrace: {StackTrace}",
          traceId, exception.Message, exception.StackTrace);
    }
    else
    {
      _logger.LogWarning(exception,
          "發生客戶端錯誤。StatusCode: {StatusCode}, TraceId: {TraceId}, Message: {Message}",
          statusCode, traceId, exception.Message);
    }

    httpContext.Response.StatusCode = statusCode;
    httpContext.Response.ContentType = "application/json";

    await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

    return true;
  }
}
