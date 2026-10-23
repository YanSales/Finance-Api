using Application.Features.Authentication.Login;
using Application.Tests.Support;
using Domain.Entities;
using Shouldly;

namespace Application.Tests.Authentication;

public sealed class LoginCommandHandlerTests
{
    private const string Email = "ana@example.com";
    private const string Password = "CorrectHorse-Battery-2026";

    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository users = new();
    private readonly FakePasswordHasher hasher = new();

    private LoginCommandHandler CreateHandler()
    {
        var timeProvider = new FixedTimeProvider(Now);

        return new LoginCommandHandler(users, hasher, new FakeAccessTokenService(timeProvider), timeProvider);
    }

    private User AddUser(string email = Email, string password = Password)
    {
        var user = User.Create(email, hasher.Hash(password), Now);
        users.Users.Add(user);

        return user;
    }

    [Fact]
    public async Task HandleIssuesABearerTokenForValidCredentials()
    {
        var user = AddUser();

        var result = await CreateHandler().Handle(new LoginCommand(Email, Password), CancellationToken.None);

        var response = result.ShouldNotBeNull();
        response.AccessToken.ShouldBe($"token-for-{user.Id}");
        response.TokenType.ShouldBe("Bearer");
        response.ExpiresAt.ShouldBe(Now.Add(FakeAccessTokenService.Lifetime));
        response.ExpiresInSeconds.ShouldBe(15 * 60);
    }

    [Fact]
    public async Task HandleNormalizesTheEmailBeforeLookingTheUserUp()
    {
        AddUser();

        var response = await CreateHandler().Handle(
            new LoginCommand("  ANA@Example.com ", Password),
            CancellationToken.None);

        response.ShouldNotBeNull();
    }

    [Fact]
    public async Task HandleReturnsNullForAnIncorrectPassword()
    {
        AddUser();

        var response = await CreateHandler().Handle(new LoginCommand(Email, "wrong-password-123"), CancellationToken.None);

        response.ShouldBeNull();
    }

    [Fact]
    public async Task HandleReturnsNullForAnUnknownUserButStillVerifiesAPasswordHash()
    {
        var response = await CreateHandler().Handle(new LoginCommand("nobody@example.com", Password), CancellationToken.None);

        response.ShouldBeNull();

        // O hasher e sempre acionado (com hash nulo = "dummy") para equalizar o tempo de resposta.
        hasher.VerifyCalls.ShouldBe(1);
        hasher.LastVerifiedHash.ShouldBeNull();
    }

    [Fact]
    public async Task HandleReturnsNullForAnInactiveUserEvenWithTheCorrectPassword()
    {
        var user = AddUser();
        user.Deactivate(Now.AddMinutes(1));

        var response = await CreateHandler().Handle(new LoginCommand(Email, Password), CancellationToken.None);

        response.ShouldBeNull();
    }
}
