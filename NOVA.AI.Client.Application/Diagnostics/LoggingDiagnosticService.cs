using NOVA.AI.Client.Application.Logging;

namespace NOVA.AI.Client.Application.Diagnostics;

public sealed class LoggingDiagnosticService(IApplicationLogger logger)
{
    public void WriteDiagnostic()
    {
        logger.Information("NOVA.AI.Client central logging diagnostic requested.");
        logger.Debug("NOVA.AI.Client central logging integration is active.");
    }
}
