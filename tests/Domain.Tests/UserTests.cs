using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Shouldly;

namespace Domain.Tests;

public sealed class UserTests
{
    private const string PasswordHash = "stored-password-hash";

    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateNormalizesTheEmailAndStartsActiveWithTheUserRole()
    {
        var user = User.Create("  Ana.Silva@Example.COM ", PasswordHash, Now);

        user.Email.ShouldBe("ana.silva@example.com");
        user.PasswordHash.ShouldBe(PasswordHash);
        user.Role.ShouldBe(UserRole.User);
        user.IsActive.ShouldBeTrue();
        user.CreatedAt.ShouldBe(Now);
        user.UpdatedAt.ShouldBe(Now);
        user.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void CreateGeneratesDistinctIds()
    {
        var first = User.Create("first@example.com", PasswordHash, Now);
        var second = User.Create("second@example.com", PasswordHash, Now);

        first.Id.ShouldNotBe(second.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-at-sign")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    [InlineData("a@@example.com")]
    [InlineData("a@b@example.com")]
    [InlineData("with space@example.com")]
    public void CreateRejectsInvalidEmails(string email)
    {
        Should.Throw<DomainException>(() => User.Create(email, PasswordHash, Now));
    }

    [Fact]
    public void CreateRejectsEmailsLongerThanTheLimit()
    {
        var email = new string('a', User.EmailMaxLength) + "@example.com";

        Should.Throw<DomainException>(() => User.Create(email, PasswordHash, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateRejectsAnEmptyPasswordHash(string passwordHash)
    {
        Should.Throw<DomainException>(() => User.Create("ana@example.com", passwordHash, Now));
    }

    [Fact]
    public void NormalizeEmailTrimsAndLowercasesWithInvariantCulture()
    {
        User.NormalizeEmail("  MIXED.Case@Example.COM ").ShouldBe("mixed.case@example.com");
    }

    [Fact]
    public void DeactivateMakesTheUserInactiveAndUpdatesTheTimestamp()
    {
        var user = User.Create("ana@example.com", PasswordHash, Now);
        var later = Now.AddHours(1);

        user.Deactivate(later);

        user.IsActive.ShouldBeFalse();
        user.UpdatedAt.ShouldBe(later);
        user.CreatedAt.ShouldBe(Now);
    }

    [Fact]
    public void DeactivatingAnInactiveUserDoesNotChangeTheTimestamp()
    {
        var user = User.Create("ana@example.com", PasswordHash, Now);
        user.Deactivate(Now.AddHours(1));

        user.Deactivate(Now.AddHours(2));

        user.UpdatedAt.ShouldBe(Now.AddHours(1));
    }

    [Fact]
    public void ActivateRestoresAnInactiveUser()
    {
        var user = User.Create("ana@example.com", PasswordHash, Now);
        user.Deactivate(Now.AddHours(1));

        user.Activate(Now.AddHours(2));

        user.IsActive.ShouldBeTrue();
        user.UpdatedAt.ShouldBe(Now.AddHours(2));
    }

    [Fact]
    public void ChangeRoleUpdatesTheRoleAndTheTimestamp()
    {
        var user = User.Create("ana@example.com", PasswordHash, Now);

        user.ChangeRole(UserRole.Admin, Now.AddMinutes(5));

        user.Role.ShouldBe(UserRole.Admin);
        user.UpdatedAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void ChangeRoleRejectsAnUndefinedRole()
    {
        var user = User.Create("ana@example.com", PasswordHash, Now);

        Should.Throw<DomainException>(() => user.ChangeRole((UserRole)99, Now));
        user.Role.ShouldBe(UserRole.User);
    }
}
