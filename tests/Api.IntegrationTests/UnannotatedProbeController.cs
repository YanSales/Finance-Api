using Microsoft.AspNetCore.Mvc;

namespace Api.IntegrationTests;

/// <summary>
/// Controller exclusivo de testes SEM nenhum atributo de autorizacao. Garante que a convencao
/// "controllers exigem autenticacao por padrao" (MapControllers().RequireAuthorization()) continua valendo.
/// </summary>
[ApiController]
[Route("api/v1/test/unannotated-probe")]
public sealed class UnannotatedProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
