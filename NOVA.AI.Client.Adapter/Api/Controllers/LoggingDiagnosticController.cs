using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NOVA.AI.Client.Application.Diagnostics;

namespace NOVA.AI.Client.Adapter.Api.Controllers;

[ApiController]
[Route("api/v1/diagnostics/logging")]
public sealed class LoggingDiagnosticController(LoggingDiagnosticService diagnosticService) : ControllerBase
{
    /// <summary>Writes a diagnostic log through the configured Y10 central logging client.</summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public IActionResult Post()
    {
        diagnosticService.WriteDiagnostic();
        return Accepted(new { status = "accepted", message = "Diagnostic log submitted to the local logging queue." });
    }
}
