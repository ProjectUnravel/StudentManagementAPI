using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Net;
using System.Text.Json;

namespace StudentManagementApi.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly Microsoft.Extensions.Logging.ILogger<GlobalExceptionMiddleware> _logger;
        private static readonly Serilog.ILogger _serilogLogger = Log.ForContext<GlobalExceptionMiddleware>();

        public GlobalExceptionMiddleware(RequestDelegate next, Microsoft.Extensions.Logging.ILogger<GlobalExceptionMiddleware> logger)
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
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            // Log the exception with structured logging
            _serilogLogger.Error(exception,
                "Unhandled exception occurred. RequestId: {RequestId}, Method: {Method}, Path: {Path}, UserId: {UserId}, UserAgent: {UserAgent}",
                context.TraceIdentifier,
                context.Request.Method,
                context.Request.Path,
                context.User?.Identity?.Name ?? "Anonymous",
                context.Request.Headers["User-Agent"].FirstOrDefault());

            var response = context.Response;
            response.ContentType = "application/json";

            var errorResponse = new ErrorResponse();

            switch (exception)
            {
                case ArgumentException argEx:
                    // Bad request
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Status = false;
                    errorResponse.Message = argEx.Message;
                    errorResponse.ErrorCode = "INVALID_ARGUMENT";
                    
                    _serilogLogger.Warning(argEx, 
                        "Bad request: {Message}, RequestPath: {Path}", 
                        argEx.Message, context.Request.Path);
                    break;

                case KeyNotFoundException keyNotFoundEx:
                    // Not found
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    errorResponse.Status = false;
                    errorResponse.Message = keyNotFoundEx.Message;
                    errorResponse.ErrorCode = "NOT_FOUND";
                    
                    _serilogLogger.Warning(keyNotFoundEx, 
                        "Resource not found: {Message}, RequestPath: {Path}", 
                        keyNotFoundEx.Message, context.Request.Path);
                    break;

                case DbUpdateException dbEx:
                    // Database-related errors
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Status = false;
                    errorResponse.Message = "Database operation failed. Please check the data and try again.";
                    errorResponse.ErrorCode = "DATABASE_ERROR";
                    
                    _serilogLogger.Error(dbEx, 
                        "Database error: {Message}, InnerException: {InnerException}, RequestPath: {Path}", 
                        dbEx.Message, dbEx.InnerException?.Message, context.Request.Path);
                    break;

                case UnauthorizedAccessException unauthorizedEx:
                    // Unauthorized
                    response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    errorResponse.Status = false;
                    errorResponse.Message = "Unauthorized access";
                    errorResponse.ErrorCode = "UNAUTHORIZED";
                    
                    _serilogLogger.Warning(unauthorizedEx, 
                        "Unauthorized access attempt: {Message}, RequestPath: {Path}", 
                        unauthorizedEx.Message, context.Request.Path);
                    break;

                case InvalidOperationException invalidOpEx:
                    // Bad request for invalid operations
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    errorResponse.Status = false;
                    errorResponse.Message = invalidOpEx.Message;
                    errorResponse.ErrorCode = "INVALID_OPERATION";
                    
                    _serilogLogger.Warning(invalidOpEx, 
                        "Invalid operation: {Message}, RequestPath: {Path}", 
                        invalidOpEx.Message, context.Request.Path);
                    break;

                default:
                    // Internal server error
                    response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    errorResponse.Status = false;
                    errorResponse.Message = "An internal server error occurred";
                    errorResponse.ErrorCode = "INTERNAL_ERROR";
                    
                    _serilogLogger.Error(exception, 
                        "Unhandled exception: {Message}, StackTrace: {StackTrace}, RequestPath: {Path}", 
                        exception.Message, exception.StackTrace, context.Request.Path);
                    break;
            }

            // Log important request context data
            _serilogLogger.Information(
                "Exception response sent. StatusCode: {StatusCode}, RequestId: {RequestId}, ResponseMessage: {ResponseMessage}",
                response.StatusCode, context.TraceIdentifier, errorResponse.Message);

            var jsonResponse = JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await response.WriteAsync(jsonResponse);
        }
    }

    public class ErrorResponse
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    // Extension method to register the middleware
    public static class GlobalExceptionMiddlewareExtensions
    {
        public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<GlobalExceptionMiddleware>();
        }
    }
}