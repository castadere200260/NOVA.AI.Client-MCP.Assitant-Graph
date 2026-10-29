namespace NOVA.AI.Client.Application.Logging;

/// <summary>Application command for submitting a log message to central logging.</summary>
public sealed record LogMessageCommand(LogSeverity Severity, string Message);
