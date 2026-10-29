namespace NOVA.AI.Client.Infrastructure.Logging;

/// <summary>Configuration for forwarding complete Y10 contracts.</summary>
public sealed class CentralLoggingForwardingOptions
{
    public const string SectionName = "CentralLoggingForwarding";
    public string Endpoint { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public int TimeoutMilliseconds { get; set; } = 2000;
}
