using Domain.Entities;
using FluentValidation;

namespace Application.Features.Authentication.Login;

/// <summary>
/// Validacao minima do login: nao aplica a politica de senha do cadastro nem valida o formato do e-mail,
/// para nao revelar regras nem diferenciar respostas. Os limites de tamanho evitam hashing de entradas gigantes.
/// </summary>
internal sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(command => command.Email)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Email is required.")
            .MaximumLength(User.EmailMaxLength).WithMessage("Email is too long.");

        RuleFor(command => command.Password)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Password is required.")
            .MaximumLength(PasswordPolicy.MaxLength).WithMessage("Password is too long.");
    }
}
