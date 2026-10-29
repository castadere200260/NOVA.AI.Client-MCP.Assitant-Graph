using Y10.Foundation.Logging.Contracts;

namespace NOVA.AI.Client.Application.Logging;

/// <summary>Validates and submits a log request using the configured logging boundary.</summary>
public sealed class SubmitLogService(ICentralLogSubmission submission)
{
    public async Task ExecuteAsync(LogRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Log message is required.", nameof(request));

        await submission.SubmitAsync(request, cancellationToken).ConfigureAwait(false);
    }
}
