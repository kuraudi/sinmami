using System.Net;
using System.Text.Json;
using FluentValidation;
using RentGen.Application.Common.Exceptions;

namespace RentGen.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<ErrorHandlingMiddleware> _logger = logger;

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException validationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                traceId = context.TraceIdentifier,
                code = "validation_error",
                message = "Обнаружены ошибки валидации запроса.",
                errors = validationException.Errors.Select(x => new
                {
                    field = x.PropertyName,
                    code = x.ErrorCode,
                    message = x.ErrorMessage
                })
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (BusinessValidationException businessValidationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.UnprocessableEntity;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                traceId = context.TraceIdentifier,
                code = "business_validation_error",
                message = businessValidationException.Message,
                errors = businessValidationException.Errors.Select(x => new
                {
                    field = x.Field,
                    stepKey = x.StepKey,
                    code = x.Code,
                    message = x.Message
                })
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (UnauthorizedAccessException unauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                traceId = context.TraceIdentifier,
                code = "feature_access_denied",
                message = unauthorizedAccessException.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (InvalidOperationException invalidOperationException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                traceId = context.TraceIdentifier,
                code = "invalid_operation",
                message = invalidOperationException.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (HttpRequestException httpRequestException)
        {
            _logger.LogWarning(httpRequestException, "External AI provider request failed");

            context.Response.StatusCode = (int)HttpStatusCode.BadGateway;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                traceId = context.TraceIdentifier,
                code = "ai_provider_error",
                message = "Не удалось получить ответ от ИИ-провайдера.",
                details = httpRequestException.Message
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception");

            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new
            {
                traceId = context.TraceIdentifier,
                code = "internal_error",
                message = "На сервере произошла непредвиденная ошибка."
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload));
        }
    }
}
