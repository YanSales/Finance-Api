namespace Application.Abstractions.Authentication;

/// <summary>Hash e verificacao de senhas. Implementada com um algoritmo consolidado (ver Infrastructure).</summary>
public interface IPasswordHasher
{
    /// <summary>Gera o hash (com salt) de uma senha.</summary>
    string Hash(string password);

    /// <summary>
    /// Verifica a senha contra um hash. Quando <paramref name="passwordHash"/> e <see langword="null"/>
    /// (usuario inexistente) executa um calculo equivalente e retorna <see langword="false"/>, para que o
    /// tempo de resposta nao revele se a conta existe.
    /// </summary>
    bool Verify(string? passwordHash, string password);
}
