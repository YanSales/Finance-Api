using FluentValidation;
using MediatR;

namespace Application.Behaviors;

/// <summary>
/// Executa os validators (FluentValidation) do request antes do handler. Falhas viram
/// <see cref="ValidationException"/>, convertida em HTTP 400 pelo tratamento global de excecoes.
/// </summary>
internal sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var validatorList = validators.ToList();

        if (validatorList.Count > 0)
        {
            var context = new ValidationContext<TRequest>(request);
            var results = await Task.WhenAll(
                validatorList.Select(validator => validator.ValidateAsync(context, cancellationToken)));
            var failures = results.SelectMany(result => result.Errors).Where(failure => failure is not null).ToList();

            if (failures.Count > 0)
            {
                throw new ValidationException(failures);
            }
        }

        // No MediatR 12.5.0 o delegate recebe o CancellationToken: propaga o cancelamento da requisicao ao handler.
        return await next(cancellationToken);
    }
}
