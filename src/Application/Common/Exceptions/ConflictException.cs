namespace Application.Common.Exceptions;

/// <summary>
/// Conflito com o estado atual do recurso (HTTP 409). A mensagem e retornada ao cliente em Problem Details e nao deve conter informacao sensivel.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException()
    {
    }

    public ConflictException(string message)
        : base(message)
    {
    }

    public ConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
