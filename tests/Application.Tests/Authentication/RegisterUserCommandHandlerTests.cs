using Application.Common.Exceptions;
using Application.Features.Authentication.RegisterUser;
using Application.Tests.Support;
using Domain.Enums;
using Shouldly;

namespace Application.Tests.Authentication;

public sealed class RegisterUserCommandHandlerTests
{
    private const string Password = "CorrectHorse-Battery-2026";

    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository users = new();
    private readonly FakePasswordHasher hasher = new();

    private RegisterUserCommandHandler CreateHandler() => new(users, hasher, new FixedTimeProvider(Now));

    [Fact]
    public async Task HandlePersistsTheUserWithANormalizedEmailAndOnlyTheHashOfThePassword()
    {
        var response = await CreateHandler().Handle(
            new RegisterUserCommand("  Ana@Example.COM ", Password),
            CancellationToken.None);

        var stored = users.Users.ShouldHaveSingleItem();
        stored.Email.ShouldBe("ana@example.com");
        stored.PasswordHash.ShouldBe(FakePasswordHasher.Prefix + Password);
        stored.PasswordHash.ShouldNotBe(Password);
        stored.Role.ShouldBe(UserRole.User);
        stored.IsActive.ShouldBeTrue();

        response.Id.ShouldBe(stored.Id);
        response.Email.ShouldBe("ana@example.com");
        response.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task HandleRejectsAnEmailThatIsAlreadyRegisteredIgnoringCase()
    {
        var handler = CreateHandler();
        await handler.Handle(new RegisterUserCommand("ana@example.com", Password), CancellationToken.None);

        await Should.ThrowAsync<ConflictException>(() =>
            handler.Handle(new RegisterUserCommand("ANA@example.com", Password), CancellationToken.None));

        users.Users.Count.ShouldBe(1);
    }

    [Fact]
    public async Task HandlePropagatesTheConflictRaisedByTheRepositoryInConcurrentRegistrations()
    {
        users.ThrowConflictOnAdd = true;

        await Should.ThrowAsync<ConflictException>(() =>
            CreateHandler().Handle(new RegisterUserCommand("ana@example.com", Password), CancellationToken.None));
    }
}
