using Application.Common.Exceptions;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Api.Middleware;

/// <summary>
/// Converte excecoes em respostas Problem Details (RFC 7807). Evita try/catch nos controllers.
/// Excecoes inesperadas retornam 500 generico: nenhuma mensagem interna ou stack trace e exposta.
/// </summary>
internal sealed partial class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    private const int ClientClosedRequestStatusCode = 499;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            httpContext.Response.StatusCode = ClientClosedRequestStatusCode;
            return true;
        }

        ProblemDetails problem = exception switch
        {
            ValidationException validation => CreateValidationProblem(validation),
            DomainException domain => Create(StatusCodes.Status422UnprocessableEntity, "Business rule violation", domain.Message),
            NotFoundException notFound => Create(StatusCodes.Status404NotFound, "Resource not found", notFound.Message),
            ConflictException conflict => Create(StatusCodes.Status409Conflict, "Conflict", conflict.Message),
            _ => Create(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", detail: null),
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            LogUnhandledException(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Create(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };

    private static HttpValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation error",
            Detail = "One or more validation errors occurred.",
        };
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Error, Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception, string method, string path);
}
