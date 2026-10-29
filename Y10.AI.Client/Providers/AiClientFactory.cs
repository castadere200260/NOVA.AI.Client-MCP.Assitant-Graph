using Y10.AI.Client.Abstractions;

namespace Y10.AI.Client.Providers;

public sealed class AiClientFactory(
    OpenAiChatClient openAiClient,
    OllamaChatClient ollamaClient) : IAiClientFactory
{
    public IAiClient GetClient(string provider) =>
        provider.Trim().ToLowerInvariant() switch
        {
            "openai" or "chatgpt" => openAiClient,
            "ollama" => ollamaClient,
            _ => throw new ArgumentException("Provider must be 'ChatGPT' or 'Ollama'.", nameof(provider))
        };
}
