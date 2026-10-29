using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NOVA.AI.Client.Application.Logging;
using NOVA.AI.Client.Infrastructure.Logging;
using Y10.Foundation.Logging;

namespace NOVA.AI.Client.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNovaAiClientInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Local application logs: use the official Y10 client and its non-blocking queue.
        services.AddCentralLoggingClient(configuration);
        services.AddSingleton<IApplicationLogger, Y10ApplicationLogger>();
        services.AddScoped<NOVA.AI.Client.Application.Diagnostics.LoggingDiagnosticService>();

        // Forwarding endpoint for requests that already contain the complete Y10 contract.
        services.Configure<CentralLoggingForwardingOptions>(
            configuration.GetSection(CentralLoggingForwardingOptions.SectionName));
        services.AddHttpClient("Y10CentralLogForwarding", (sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<CentralLoggingForwardingOptions>>().Value;
            if (!Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var endpoint) ||
                (endpoint.Scheme != Uri.UriSchemeHttp && endpoint.Scheme != Uri.UriSchemeHttps))
            {
                throw new InvalidOperationException(
                    "CentralLoggingForwarding:Endpoint must be an absolute HTTP or HTTPS URL.");
            }

            if (!string.IsNullOrWhiteSpace(options.ApiKey))
            {
                if (endpoint.Scheme != Uri.UriSchemeHttps && !endpoint.IsLoopback)
                    throw new InvalidOperationException("An API key requires HTTPS for non-loopback endpoints.");
                client.DefaultRequestHeaders.Add("X-API-KEY", options.ApiKey);
            }

            client.BaseAddress = endpoint;
            client.Timeout = TimeSpan.FromMilliseconds(Math.Max(100, options.TimeoutMilliseconds));
        });

        services.AddScoped<ICentralLogSubmission, Y10CentralLogSubmission>();
        services.AddScoped<SubmitLogService>();
        return services;
    }
}
