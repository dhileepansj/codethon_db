using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using DCView.Hackathon.Shared.ResponseModel;

namespace DCView.Hackathon.API.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Business logic error");
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>(
                new[] { ex.Message }, "Bad Request"), _jsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            // In Development, show the actual error for debugging
            var env = context.RequestServices.GetService<IWebHostEnvironment>();
            var errors = env?.IsDevelopment() == true
                ? new[] { ex.Message, ex.InnerException?.Message ?? "" }
                : new[] { "An unexpected error occurred" };

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new ApiResponse<object>(
                errors, "Internal Server Error"), _jsonOptions);
        }
    }
}
