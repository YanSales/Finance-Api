using Application.Behaviors;
using FluentValidation;
using MediatR;
using Shouldly;

namespace Application.Tests.Authentication;

public sealed class ValidationBehaviorTests
{
    private sealed record PingRequest(string Name) : IRequest<string>;

    private sealed class PingRequestValidator : AbstractValidator<PingRequest>
    {
        public PingRequestValidator()
        {
            RuleFor(request => request.Name).NotEmpty().WithMessage("Name is required.");
        }
    }

    [Fact]
    public async Task HandleThrowsValidationExceptionAndSkipsTheHandlerWhenTheRequestIsInvalid()
    {
        var behavior = new ValidationBehavior<PingRequest, string>([new PingRequestValidator()]);
        var handlerCalled = false;

        var exception = await Should.ThrowAsync<ValidationException>(() =>
            behavior.Handle(
                new PingRequest(string.Empty),
                _ =>
                {
                    handlerCalled = true;
                    return Task.FromResult("pong");
                },
                CancellationToken.None));

        exception.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("Name is required.");
        handlerCalled.ShouldBeFalse();
    }

    [Fact]
    public async Task HandleCallsTheHandlerWhenTheRequestIsValid()
    {
        var behavior = new ValidationBehavior<PingRequest, string>([new PingRequestValidator()]);

        var response = await behavior.Handle(new PingRequest("ana"), _ => Task.FromResult("pong"), CancellationToken.None);

        response.ShouldBe("pong");
    }

    [Fact]
    public async Task HandleCallsTheHandlerWhenThereAreNoValidators()
    {
        var behavior = new ValidationBehavior<PingRequest, string>([]);

        var response = await behavior.Handle(new PingRequest(string.Empty), _ => Task.FromResult("pong"), CancellationToken.None);

        response.ShouldBe("pong");
    }

    [Fact]
    public async Task HandleForwardsTheCancellationTokenToTheNextHandlerInThePipeline()
    {
        var behavior = new ValidationBehavior<PingRequest, string>([new PingRequestValidator()]);
        using var cancellationTokenSource = new CancellationTokenSource();
        var received = CancellationToken.None;

        await behavior.Handle(
            new PingRequest("ana"),
            cancellationToken =>
            {
                received = cancellationToken;
                return Task.FromResult("pong");
            },
            cancellationTokenSource.Token);

        received.ShouldBe(cancellationTokenSource.Token);
    }
}
