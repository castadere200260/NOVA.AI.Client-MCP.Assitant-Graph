using Y10.Foundation.Logging.Contracts;

namespace NOVA.AI.Client.Application.Logging;

/// <summary>Application boundary for submitting a Y10 logging contract.</summary>
public interface ICentralLogSubmission
{
    Task SubmitAsync(LogRequest request, CancellationToken cancellationToken = default);
}
