using Domain.Entities;
using FluentValidation;

namespace Application.Features.Authentication.RegisterUser;

/// <summary>Validacao de entrada do cadastro (formato do e-mail e politica de senha).</summary>
internal sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(User.EmailMaxLength).WithMessage("Email is too long.")
            .EmailAddress().WithMessage("Email is not a valid email address.");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(PasswordPolicy.MinLength).WithMessage("Password must have at least 12 characters.")
            .MaximumLength(PasswordPolicy.MaxLength).WithMessage("Password must have at most 128 characters.");
    }
}
