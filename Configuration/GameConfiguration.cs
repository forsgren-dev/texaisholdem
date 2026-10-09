namespace TexAIsHoldem;

public class GameConfiguration
{

    // To try the "game", set the API key to your LLM provider in a file named 'apikey.txt',
    // and place it one folder above /TexAIsHoldem/. Also set the endpoint and model below:

    public string AIModel { get; init; } = "gpt-6-luna";
    public string AIEndpoint { get; init; } = "https://api.openai.com/v1";

    public int StartingChips { get; init; } = 100_000;
    public int SmallBlind { get; init; } = 1000;
    public int BigBlind { get; init; } = 2000;
    public int AITimeoutSeconds { get; init; } = 30;
    
    public static string ResolveApiKeyFilePath()
    {
        string projectDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        return Path.GetFullPath(Path.Combine(projectDir, "..", "apikey.txt"));
    }

    public static string LoadApiKey()
    {
        string apiKeyPath = ResolveApiKeyFilePath();
        if (!File.Exists(apiKeyPath))
            throw new FileNotFoundException($"Create this file with your API key on the first line: {apiKeyPath}");
        
        string apiKeyText = File.ReadAllText(apiKeyPath).Trim();
        if (string.IsNullOrEmpty(apiKeyText))
            throw new InvalidOperationException($"API key file is empty: {apiKeyPath}");
        
        return apiKeyText;
    }
}
