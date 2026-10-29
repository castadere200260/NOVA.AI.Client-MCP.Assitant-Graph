using NOVA.AI.Client.Application.Logging;
using Y10.Foundation.Logging;

namespace NOVA.AI.Client.Infrastructure.Logging;

/// <summary>Adapts NOVA application events to the official Y10 client.</summary>
internal sealed class Y10ApplicationLogger(ILogService logger) : IApplicationLogger
{
    public void Information(string message) => logger.Info(message);
    public void Debug(string message) => logger.Debug(message);
}
