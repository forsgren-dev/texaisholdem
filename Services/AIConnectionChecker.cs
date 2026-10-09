using Microsoft.Extensions.AI;

namespace TexAIsHoldem;

public record AIConnectionResult(bool IsConnected, string Message);

public static class AIConnectionChecker
{
    public static async Task<AIConnectionResult> CheckAsync(
        IChatClient chatClient,
        string model,
        int timeoutSeconds)
    {
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            var response = await chatClient.GetResponseAsync(
                "Reply with exactly READY.",
                cancellationToken: cancellation.Token);

            if (string.IsNullOrWhiteSpace(response.Text))
                return new AIConnectionResult(false, $"{model} returned an empty response. The game will use fallback actions.");

            return new AIConnectionResult(true, $"Connected to {model}. AI opponents are active.");
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            return new AIConnectionResult(false, $"Connection to {model} timed out. The game will use fallback actions.");
        }
        catch (Exception ex)
        {
            string reason = ex.Message.ReplaceLineEndings(" ").Trim();
            return new AIConnectionResult(false, $"Could not contact {model}: {reason} The game will use fallback actions.");
        }
    }
}
