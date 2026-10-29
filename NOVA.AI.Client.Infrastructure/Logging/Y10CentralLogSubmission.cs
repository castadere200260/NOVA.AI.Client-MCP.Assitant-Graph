using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using NOVA.AI.Client.Application.Logging;
using Y10.Foundation.Logging.Contracts;

namespace NOVA.AI.Client.Infrastructure.Logging;

/// <summary>Forwards complete Y10 log contracts to the central Y10 API.</summary>
internal sealed class Y10CentralLogSubmission(
    IHttpClientFactory httpClientFactory,
    ILogger<Y10CentralLogSubmission> logger) : ICentralLogSubmission
{
    public async Task SubmitAsync(LogRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var client = httpClientFactory.CreateClient("Y10CentralLogForwarding");
            using var response = await client.PostAsJsonAsync(
                "api/v1/logs", request, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Central logging API returned status code {StatusCode}.",
                    (int)response.StatusCode);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            // Forwarding failures must not crash the calling application.
            logger.LogWarning(exception, "Could not forward log request to the central logging API.");
        }
    }
}
