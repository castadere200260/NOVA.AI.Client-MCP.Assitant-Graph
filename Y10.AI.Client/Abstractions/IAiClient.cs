using Y10.AI.Client.Models;

namespace Y10.AI.Client.Abstractions;

public interface IAiClient
{
    Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default);
}
