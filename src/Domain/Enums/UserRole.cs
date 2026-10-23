namespace Domain.Enums;

/// <summary>
/// Papel do usuario na plataforma. Base minima para diferenciar autenticacao (quem e) de
/// autorizacao (o que pode fazer). Nao e um sistema de permissoes: novos papeis exigem decisao explicita.
/// </summary>
public enum UserRole
{
    /// <summary>Usuario comum. Papel atribuido em todo cadastro publico.</summary>
    User = 1,

    /// <summary>Administrador da plataforma. Nunca atribuido pelo cadastro publico.</summary>
    Admin = 2,
}
