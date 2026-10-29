namespace Y10.AI.Client.Models;

public sealed class AiChatRequest
{
    public string Message { get; set; } = string.Empty;
    public string? SystemPrompt { get; set; }
    public string? Model { get; set; }
    public double Temperature { get; set; } = 0.2;
}
