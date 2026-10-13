using System.Text;
using System.Text.Json;
using Api.Extensions;
using Api.Middleware;
using Application.Common.Exceptions;
using Domain.Exceptions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace Api.IntegrationTests;

public sealed class GlobalExceptionHandlerTests
{
    private static async Task<(int Status, string ContentType, string Json)> HandleAsync(Exception exception)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApiFoundation();
        await using var provider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = provider };
        httpContext.Response.Body = new MemoryStream();
        var handler = new GlobalExceptionHandler(
            provider.GetRequiredService<IProblemDetailsService>(),
            NullLogger<GlobalExceptionHandler>.Instance);

        var handled = await handler.TryHandleAsync(httpContext, exception, CancellationToken.None);

        handled.ShouldBeTrue();
        httpContext.Response.Body.Position = 0;
        using var reader = new StreamReader(httpContext.Response.Body, Encoding.UTF8);
        return (httpContext.Response.StatusCode, httpContext.Response.ContentType ?? string.Empty, await reader.ReadToEndAsync());
    }

    [Fact]
    public async Task ValidationExceptionBecomesBadRequestWithGroupedErrors()
    {
        var exception = new ValidationException(
        [
            new ValidationFailure("Amount", "Amount must be greater than zero."),
            new ValidationFailure("Description", "Description is required."),
        ]);

        var (status, contentType, json) = await HandleAsync(exception);

        status.ShouldBe(400);
        contentType.ShouldContain("application/problem+json");
        using var body = JsonDocument.Parse(json);
        body.RootElement.GetProperty("errors").GetProperty("Amount")[0].GetString().ShouldBe("Amount must be greater than zero.");
        body.RootElement.GetProperty("errors").GetProperty("Description").GetArrayLength().ShouldBe(1);
    }

    [Fact]
    public async Task DomainExceptionBecomesUnprocessableEntity()
    {
        var (status, _, json) = await HandleAsync(new DomainException("Cannot transfer to the same account."));

        status.ShouldBe(422);
        using var body = JsonDocument.Parse(json);
        body.RootElement.GetProperty("detail").GetString().ShouldBe("Cannot transfer to the same account.");
    }

    [Fact]
    public async Task NotFoundExceptionBecomesNotFound()
    {
        var (status, _, _) = await HandleAsync(new NotFoundException("Account not found."));

        status.ShouldBe(404);
    }

    [Fact]
    public async Task ConflictExceptionBecomesConflict()
    {
        var (status, _, _) = await HandleAsync(new ConflictException("Email already registered."));

        status.ShouldBe(409);
    }

    [Fact]
    public async Task UnexpectedExceptionBecomesGenericInternalServerErrorWithoutLeakingDetails()
    {
        var (status, _, json) = await HandleAsync(new InvalidOperationException("Password=hunter2 stack details"));

        status.ShouldBe(500);
        json.ShouldNotContain("hunter2");
        json.ShouldNotContain("InvalidOperationException");
        using var body = JsonDocument.Parse(json);
        body.RootElement.TryGetProperty("detail", out _).ShouldBeFalse();
    }
}
