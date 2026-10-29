using ModelContextProtocol.Server;
using NOVA.AI.Client.Application.Diagnostics;
using NOVA.AI.Client.Application.Logging;
using System.ComponentModel;
using Y10.Foundation.Logging.Contracts;

namespace NOVA.AI.Client.Mcp.Tools;

/// <summary>
/// MCP tools that expose existing NOVA.AI.Client application use cases.
/// The tools intentionally delegate to the same services used by the REST controllers.
/// </summary>
[McpServerToolType]
public sealed class NovaLoggingTools(SubmitLogService submitLogService,
                                     LoggingDiagnosticService diagnosticService)
{
    [McpServerTool(Name = "submit_log")]
    [Description(
        "Submit a structured Y10 log request through the same application service used by POST /api/v1/logs. " +
        "Provide the full LogRequest contract, including level, message, service metadata, timestamp, and optional properties.")]
    public async Task<string> SubmitLogAsync(LogRequest request,
                                            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required.", nameof(request));
        }

        await submitLogService.ExecuteAsync(request, cancellationToken).ConfigureAwait(false);

        return "The log request was processed by the submission service. Check service logs for any forwarding failure.";
    }


    [McpServerTool(Name = "write_logging_diagnostic")]
    [Description(
        "Write a diagnostic message through the configured Y10.Foundation.Logging.Client queue, " +
        "using the same application service as POST /api/v1/diagnostics/logging.")]
    public string WriteLoggingDiagnostic()
    {
        diagnosticService.WriteDiagnostic();
        return "The diagnostic logging use case was invoked.";
    }
}
