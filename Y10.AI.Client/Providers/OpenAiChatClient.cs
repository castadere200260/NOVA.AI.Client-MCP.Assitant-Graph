using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Y10.AI.Client.Abstractions;
using Y10.AI.Client.Configuration;
using Y10.AI.Client.Models;

namespace Y10.AI.Client.Providers;

/// <summary>OpenAI Chat Completions client using HttpClient; no vendor SDK required.</summary>
public sealed class OpenAiChatClient(HttpClient httpClient, IOptions<AiClientOptions> options) : IAiClient
{
    private readonly OpenAiOptions _options = options.Value.OpenAI;

    public async Task<AiChatResponse> ChatAsync(AiChatRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("Configure AiClient:OpenAI:ApiKey (environment variable recommended).");
        if (string.IsNullOrWhiteSpace(request.Message))
            throw new ArgumentException("Message is required.", nameof(request));

        using var message = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);
        message.Content = JsonContent.Create(new
        {
            model = string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model,
            messages = BuildMessages(request),
            temperature = Math.Clamp(request.Temperature, 0, 2)
        });

        using var response = await httpClient.SendAsync(message, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"OpenAI returned {(int)response.StatusCode}: {Limit(body)}");

        using var json = JsonDocument.Parse(body);
        var content = json.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return new AiChatResponse("ChatGPT", string.IsNullOrWhiteSpace(request.Model) ? _options.Model : request.Model, content ?? string.Empty);
    }

    private static object[] BuildMessages(AiChatRequest request)
    {
        var messages = new List<object>();
        if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
            messages.Add(new { role = "system", content = request.SystemPrompt });
        messages.Add(new { role = "user", content = request.Message });
        return messages.ToArray();
    }

    private static string Limit(string value) => value.Length <= 1200 ? value : value[..1200];
}
