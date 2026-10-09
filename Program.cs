using Microsoft.Extensions.AI;
using Microsoft.Agents.AI;
using OpenAI.Chat;
using System.ClientModel;
using OpenAI;

namespace TexAIsHoldem;

// My little project experimenting with GPT-4o Mini agents as Texas Hold'em poker opponents.
// Three agents play with different styles: Aggressive, Moderate, and Careful.
// Code is following SOLID principles.
// Erik Forsgren, April 2026, github.com/forsgren-dev/

internal class Program
{
    static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var config = new GameConfiguration();
        var ui = new ConsoleGameUI();

        // Create players
        var humanPlayer = new Participant
        {
            Name = "Player",
            TotalChips = config.StartingChips,
            IsHuman = true
        };

        var aiPlayers = new List<Participant>
        {
            new() { Name = "Aggressive", TotalChips = config.StartingChips },
            new() { Name = "Moderate", TotalChips = config.StartingChips },
            new() { Name = "Careful", TotalChips = config.StartingChips },
        };

        // Initializing AI agents:
        string apiKey = GameConfiguration.LoadApiKey();
        var chatClient = new ChatClient(config.AIModel,
                new ApiKeyCredential(apiKey),
                new OpenAIClientOptions { Endpoint = new Uri(config.AIEndpoint) })
            .AsIChatClient();

        var aiConnection = await AIConnectionChecker.CheckAsync(
            chatClient, config.AIModel, config.AITimeoutSeconds);

        var agents = new Dictionary<string, AIAgent>
        {
            ["Aggressive"] = PokerAgents.CreateAggressiveAgent(chatClient),
            ["Moderate"] = PokerAgents.CreateModerateAgent(chatClient),
            ["Careful"] = PokerAgents.CreateCarefulAgent(chatClient),
        };
        var handJudgeAgent = PokerAgents.CreateHandJudgeAgent(chatClient);

        // Creating game components:
        var actionParser = new ActionParser(config.BigBlind);
        var actionHandler = new PlayerActionHandler(ui, actionParser, agents, config.BigBlind, config.AITimeoutSeconds);
        var bettingRound = new BettingRound(ui, actionHandler);
        var potManager = new PotManager();
        var handResolver = new HandResolver(ui, handJudgeAgent, potManager);

        // Creating and running game engine:
        var gameEngine = new PokerGameEngine(
            config, ui, bettingRound, handResolver, humanPlayer, aiPlayers, aiConnection);
        await gameEngine.RunAsync();
    }
}
