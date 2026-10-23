using Application.Features.Users.GetCurrentUser;
using Application.Tests.Support;
using Domain.Entities;
using Domain.Enums;
using Shouldly;

namespace Application.Tests.Users;

public sealed class GetCurrentUserQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeUserRepository users = new();

    [Fact]
    public async Task HandleReturnsTheUserData()
    {
        var user = User.Create("ana@example.com", "hash", Now);
        user.ChangeRole(UserRole.Admin, Now);
        users.Users.Add(user);

        var result = await new GetCurrentUserQueryHandler(users).Handle(
            new GetCurrentUserQuery(user.Id),
            CancellationToken.None);

        var response = result.ShouldNotBeNull();
        response.Id.ShouldBe(user.Id);
        response.Email.ShouldBe("ana@example.com");
        response.Role.ShouldBe("Admin");
        response.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public async Task HandleReturnsNullWhenTheUserDoesNotExist()
    {
        var response = await new GetCurrentUserQueryHandler(users).Handle(
            new GetCurrentUserQuery(Guid.NewGuid()),
            CancellationToken.None);

        response.ShouldBeNull();
    }

    [Fact]
    public async Task HandleReturnsNullWhenTheUserIsInactive()
    {
        var user = User.Create("ana@example.com", "hash", Now);
        user.Deactivate(Now);
        users.Users.Add(user);

        var response = await new GetCurrentUserQueryHandler(users).Handle(
            new GetCurrentUserQuery(user.Id),
            CancellationToken.None);

        response.ShouldBeNull();
    }
}
