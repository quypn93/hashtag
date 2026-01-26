namespace HashTag.Options;

/// <summary>
/// OpenAI/Groq API configuration options
/// </summary>
public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-3.5-turbo";
    public int MaxTokens { get; set; } = 800;
    public double Temperature { get; set; } = 0.7;
    public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string Provider { get; set; } = "OpenAI";
}
