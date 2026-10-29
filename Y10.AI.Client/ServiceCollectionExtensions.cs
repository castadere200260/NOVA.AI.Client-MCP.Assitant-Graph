using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Y10.AI.Client.Abstractions;
using Y10.AI.Client.Configuration;
using Y10.AI.Client.Providers;

namespace Y10.AI.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddY10AiClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<AiClientOptions>()
            .Bind(configuration.GetSection(AiClientOptions.SectionName))
            .Validate(o => Uri.TryCreate(o.OpenAI.BaseUrl, UriKind.Absolute, out _), "AiClient:OpenAI:BaseUrl must be an absolute URL.")
            .Validate(o => Uri.TryCreate(o.Ollama.BaseUrl, UriKind.Absolute, out _), "AiClient:Ollama:BaseUrl must be an absolute URL.")
            .ValidateOnStart();

        services.AddHttpClient<OpenAiChatClient>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<AiClientOptions>>().Value.OpenAI;
            client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 1, 600));
        });
        services.AddHttpClient<OllamaChatClient>((sp, client) =>
        {
            var o = sp.GetRequiredService<IOptions<AiClientOptions>>().Value.Ollama;
            client.BaseAddress = new Uri(o.BaseUrl.TrimEnd('/') + "/");
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 1, 600));
        });
        services.AddTransient<IAiClientFactory, AiClientFactory>();
        return services;
    }
}
