using System.Text.Json.Serialization;

namespace ClipboardTranslator.Api;

public sealed class ChatMessage
{
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public sealed class ChatCompletionRequest
{
    [JsonPropertyName("model")]
    public string Model { get; set; } = string.Empty;

    [JsonPropertyName("messages")]
    public List<ChatMessage> Messages { get; set; } = new();

    [JsonPropertyName("temperature")]
    public double Temperature { get; set; } = 0;

    [JsonPropertyName("response_format")]
    public ResponseFormat? ResponseFormat { get; set; }
}

public sealed class ResponseFormat
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "json_object";
}

public sealed class ChatCompletionResponse
{
    [JsonPropertyName("choices")]
    public List<Choice>? Choices { get; set; }
}

public sealed class Choice
{
    [JsonPropertyName("message")]
    public ChatMessage? Message { get; set; }
}
