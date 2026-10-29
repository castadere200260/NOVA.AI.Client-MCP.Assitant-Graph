using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace NOVA.AI.Client.Adapter.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Get() => Ok(new { status = "healthy", service = "NOVA.AI.Client" });
}
