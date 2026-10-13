namespace Domain.Exceptions;

/// <summary>
/// Violacao de uma regra de negocio ou invariante de dominio. A mensagem e destinada ao cliente
/// da API (retornada em Problem Details, HTTP 422) e nao deve conter informacao sensivel.
/// </summary>
public class DomainException : Exception
{
    public DomainException()
    {
    }

    public DomainException(string message)
        : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
