using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Y10.AI.Client.Abstractions;
using Y10.AI.Client.Configuration;
using Y10.AI.Client.Models;

namespace Y10.AI.Client.Providers;

/// <summary>Local Ollama /api/chat client.</summary>
public sealed class OllamaChatClient(HttpClient httpClient, IOptions<AiClientOptions> options) : IAiClient
{
    private readonly OllamaOptions _options = options.Value.Ollama;

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.", nameof(request));

        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });
        messages.Add(new { role = "user", content = request.Message });

        using var response = await httpClient.PostAsJsonAsync("api/chat", new
        {
            model = string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model,
            messages,
            stream = false,
            options = new { temperature = Math.Clamp(request.Temperature, 0, 2) }
        }, cancellationToken).ConfigureAwait(false);

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Ollama returned {(int)response.StatusCode}: {(body.Length <= 1200 ? body : body[..1200])}");

        using var json = JsonDocument.Parse(body);
        var content = json.RootElement.GetProperty("message").GetProperty("content").GetString();
        return new AiChatResponse("Ollama", string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model, content ?? string.Empty);
    }
}
