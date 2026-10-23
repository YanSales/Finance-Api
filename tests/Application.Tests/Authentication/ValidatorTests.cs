using Application.Features.Authentication.Login;
using Application.Features.Authentication.RegisterUser;
using Shouldly;

namespace Application.Tests.Authentication;

public sealed class ValidatorTests
{
    private const string ValidPassword = "CorrectHorse-Battery-2026";

    private readonly RegisterUserCommandValidator registerValidator = new();
    private readonly LoginCommandValidator loginValidator = new();

    [Fact]
    public void RegisterAcceptsAValidEmailAndPassword()
    {
        registerValidator.Validate(new RegisterUserCommand("ana@example.com", ValidPassword)).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void RegisterRejectsInvalidEmails(string email)
    {
        registerValidator.Validate(new RegisterUserCommand(email, ValidPassword)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RegisterRejectsAnEmailLongerThanTheLimit()
    {
        var email = new string('a', 250) + "@example.com";

        registerValidator.Validate(new RegisterUserCommand(email, ValidPassword)).IsValid.ShouldBeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short-pass1")] // 11 caracteres
    public void RegisterRejectsPasswordsBelowTheMinimumLength(string password)
    {
        registerValidator.Validate(new RegisterUserCommand("ana@example.com", password)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void RegisterAcceptsAPasswordWithExactlyTheMinimumLength()
    {
        registerValidator.Validate(new RegisterUserCommand("ana@example.com", new string('a', 12))).IsValid.ShouldBeTrue();
    }

    [Fact]
    public void RegisterRejectsPasswordsAboveTheMaximumLength()
    {
        registerValidator.Validate(new RegisterUserCommand("ana@example.com", new string('a', 129))).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void LoginDoesNotApplyThePasswordPolicyOfTheRegistration()
    {
        loginValidator.Validate(new LoginCommand("ana@example.com", "short")).IsValid.ShouldBeTrue();
    }

    [Theory]
    [InlineData("", "password")]
    [InlineData("ana@example.com", "")]
    public void LoginRequiresEmailAndPassword(string email, string password)
    {
        loginValidator.Validate(new LoginCommand(email, password)).IsValid.ShouldBeFalse();
    }

    [Fact]
    public void LoginRejectsOversizedPasswordsBeforeAnyHashing()
    {
        loginValidator.Validate(new LoginCommand("ana@example.com", new string('a', 129))).IsValid.ShouldBeFalse();
    }
}
