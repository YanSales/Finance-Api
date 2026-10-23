namespace Api.Authorization;

/// <summary>Nomes das politicas de autorizacao. Base minima: sem RBAC/permissoes granulares nesta fase.</summary>
internal static class Policies
{
    /// <summary>Exige o papel <c>Admin</c>. Usuario autenticado sem o papel recebe 403.</summary>
    public const string AdminOnly = "AdminOnly";
}
