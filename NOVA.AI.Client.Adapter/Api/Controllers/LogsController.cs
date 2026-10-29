using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NOVA.AI.Client.Application.Logging;
using Y10.Foundation.Logging.Contracts;

namespace NOVA.AI.Client.Adapter.Api.Controllers;

[ApiController]
[Route("api/v1/logs")]
public sealed class LogsController(SubmitLogService submitLogService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Post(
        [FromBody] LogRequest? request,
        CancellationToken cancellationToken)
    {
        if (request is null || string.IsNullOrWhiteSpace(request.Message))
            return BadRequest("Message is required.");

        await submitLogService.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);
        return Accepted();
    }
}
