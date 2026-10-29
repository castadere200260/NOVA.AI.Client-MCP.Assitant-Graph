namespace NOVA.AI.Client.Application.Logging;

/// <summary>
/// Application logging port. Infrastructure adapts this abstraction to Y10 central logging.
/// </summary>
public interface INovaApplicationLogger
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? exception = null);
    void Debug(string message);
}
