using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MiniIAM.Shared.Middlewares;

public class GlobalExceptionHandlerMiddleware(RequestDelegate next, ILogger<GlobalExceptionHandlerMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            await context.Response.WriteAsync(
                JsonSerializer.Serialize(new { errors = ex.ValidationResult.ErrorMessage }));
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex,
                $"Unauthorized access request detected. Request= {GetRequestDetails(context)}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                $"An unhandled exception occurred. Request= {context.Request.Path} | Exception= {GetExceptionDetails(ex)}");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }

    private string GetRequestDetails(HttpContext context) => JsonSerializer.Serialize(new
        { context.Request.Method, context.Request.Host, context.Request.Path, context.Request.QueryString });

    private string GetExceptionDetails(Exception exception) => JsonSerializer.Serialize(new
    {
        exception.GetType().Name,
        exception.Message,
        exception.StackTrace,
        InnerException = string.IsNullOrEmpty(exception.InnerException?.Message)
            ? null
            : GetExceptionDetails(exception.InnerException)
    });
}