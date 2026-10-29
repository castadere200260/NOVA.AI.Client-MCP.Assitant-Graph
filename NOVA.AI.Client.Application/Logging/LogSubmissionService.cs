namespace NOVA.AI.Client.Application.Logging;

/// <summary>Maps application log commands to the application logging port.</summary>
public sealed class LogSubmissionService(INovaApplicationLogger logger) : ILogSubmissionService
{
    public void Submit(LogMessageCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (string.IsNullOrWhiteSpace(command.Message))
        {
            throw new ArgumentException("A log message is required.", nameof(command));
        }

        switch (command.Severity)
        {
            case LogSeverity.Debug:
                logger.Debug(command.Message);
                break;
            case LogSeverity.Information:
                logger.Info(command.Message);
                break;
            case LogSeverity.Warning:
                logger.Warn(command.Message);
                break;
            case LogSeverity.Error:
                logger.Error(command.Message);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unsupported log severity.");
        }
    }
}
