namespace Application.Common.Exceptions;

/// <summary>
/// Recurso solicitado nao encontrado (HTTP 404). A mensagem e retornada ao cliente em Problem Details e nao deve conter informacao sensivel.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException()
    {
    }

    public NotFoundException(string message)
        : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
