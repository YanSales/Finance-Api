using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Api.Extensions;

internal static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Identificador do usuario autenticado (claim <c>sub</c>). Sempre lido do token validado, nunca de
    /// parametros da requisicao. Retorna <see langword="null"/> se a claim estiver ausente ou invalida.
    /// </summary>
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var value = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
