using Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.IntegrationTests;

/// <summary>Endpoint exclusivo de testes, protegido pela politica <see cref="Policies.AdminOnly"/>.</summary>
[ApiController]
[Route("api/v1/test/admin-probe")]
[Authorize(Policy = Policies.AdminOnly)]
public sealed class AdminProbeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok();
}
