using Microsoft.Agents.AI;

namespace TexAIsHoldem;

public interface IPlayerActionHandler
{
    Task<BettingResult> GetPlayerActionAsync(
        Participant player,
        int playerIndex,
        int toCall,
        int pot,
        int currentBet,
        int[] betThisRound,
        int street,
        List<Card> communityCards);
}

public class BettingResult
{
    public bool Raised { get; init; }
    public int NewPot { get; init; }
    public int NewCurrentBet { get; init; }
}

public class PlayerActionHandler : IPlayerActionHandler
{
    private readonly IGameUI _ui;
    private readonly IActionParser _actionParser;
    private readonly Dictionary<string, AIAgent> _agents;
    private readonly int _bigBlind;
    private readonly int _aiTimeoutSeconds;

    public PlayerActionHandler(
        IGameUI ui,
        IActionParser actionParser,
        Dictionary<string, AIAgent> agents,
        int bigBlind,
        int aiTimeoutSeconds)
    {
        _ui = ui;
        _actionParser = actionParser;
        _agents = agents;
        _bigBlind = bigBlind;
        _aiTimeoutSeconds = aiTimeoutSeconds;
    }

    public async Task<BettingResult> GetPlayerActionAsync(
        Participant player,
        int playerIndex,
        int toCall,
        int pot,
        int currentBet,
        int[] betThisRound,
        int street,
        List<Card> communityCards)
    {
        string actionText;

        if (player.IsHuman)
        {
            actionText = _ui.GetPlayerAction(player, pot, toCall, player.TotalChips);
        }
        else
        {
            actionText = await GetAIActionAsync(player, pot, currentBet, betThisRound[playerIndex], street, communityCards, toCall);
            _ui.DisplayPlayerAction(player.Name, actionText);
            
            if (player.Name == "Aggressive")
                Console.WriteLine();
        }

        var action = _actionParser.ParseAction(actionText, toCall);
        return ApplyAction(player, playerIndex, action, toCall, betThisRound, pot, currentBet);
    }

    private async Task<string> GetAIActionAsync(
        Participant player,
        int pot,
        int currentBet,
        int myBetThisRound,
        int street,
        List<Card> communityCards,
        int toCall)
    {
        if (!_agents.TryGetValue(player.Name, out var agent))
            throw new InvalidOperationException($"Unknown agent: {player.Name}");

        string prompt = BuildPrompt(player, pot, currentBet, myBetThisRound, street, communityCards);
        string fallback = toCall == 0 ? "CHECK" : "CALL";

        try
        {
            var runTask = agent.RunAsync(prompt);
            if (await Task.WhenAny(runTask, Task.Delay(TimeSpan.FromSeconds(_aiTimeoutSeconds))) != runTask)
            {
                return fallback; 
            }

            var response = await runTask;
            string action = response.Text.Trim().ToUpperInvariant();
            return string.IsNullOrWhiteSpace(action) ? fallback : action;
        }
        catch
        {
            return fallback; 
        }
    }

    private string BuildPrompt(
        Participant player,
        int pot,
        int currentBet,
        int myBetThisRound,
        int street,
        List<Card> communityCards)
    {
        var sb = new System.Text.StringBuilder();
        string streetName = street switch
        {
            0 => "Pre-flop",
            1 => "Flop",
            2 => "Turn",
            3 => "River",
            _ => "Unknown"
        };

        sb.AppendLine($"Texas Hold'em. Current street: {streetName}.");
        sb.AppendLine("Your hole cards: " + string.Join(" ", player.HoleCards));
        
        if (communityCards.Count > 0)
            sb.AppendLine("Community cards: " + string.Join(" ", communityCards));
        
        int toCall = currentBet - myBetThisRound;
        sb.AppendLine($"Your chips: ${player.TotalChips}. Pot: ${pot}. Current bet: ${currentBet}. To call: ${toCall}.");

        if (toCall == 0)
        {
            sb.AppendLine("You can CHECK for free or RAISE. NEVER FOLD when you can check for free!");
            sb.AppendLine("Respond with exactly one of: CHECK | RAISE <amount>");
        }
        else
        {
            sb.AppendLine($"You CANNOT raise more than your chips (${player.TotalChips}). If you want to go all-in, RAISE {player.TotalChips}.");
            sb.AppendLine("Respond with exactly one of: FOLD | CALL | RAISE <amount>");
        }

        return sb.ToString();
    }

    private BettingResult ApplyAction(
        Participant player,
        int playerIndex,
        PlayerAction action,
        int toCall,
        int[] betThisRound,
        int pot,
        int currentBet)
    {
        switch (action.Type)
        {
            case ActionType.Fold:
                player.Folded = true;
                _ui.DisplayPlayerAction(player.Name, "folds");
                return new BettingResult { Raised = false, NewPot = pot, NewCurrentBet = currentBet };

            case ActionType.Check:
                _ui.DisplayPlayerAction(player.Name, "checks");
                return new BettingResult { Raised = false, NewPot = pot, NewCurrentBet = currentBet };

            case ActionType.AllIn:
                return HandleAllIn(player, playerIndex, betThisRound, pot, currentBet);

            case ActionType.Call:
                return HandleCall(player, playerIndex, toCall, betThisRound, pot, currentBet);

            case ActionType.Raise:
                return HandleRaise(player, playerIndex, action.Amount, betThisRound, pot, currentBet);

            default:
                player.Folded = true;
                _ui.DisplayPlayerAction(player.Name, "folds (unrecognized action)");
                return new BettingResult { Raised = false, NewPot = pot, NewCurrentBet = currentBet };
        }
    }

    private BettingResult HandleAllIn(Participant player, int playerIndex, int[] betThisRound, int pot, int currentBet)
    {
        int amount = player.TotalChips;
        player.TotalChips = 0;
        player.IsAllIn = true;
        pot += amount;
        betThisRound[playerIndex] += amount;
        currentBet = Math.Max(currentBet, betThisRound[playerIndex]);
        
        _ui.DisplayPlayerAction(player.Name, "is ALL-IN for", amount, pot);
        return new BettingResult { Raised = true, NewPot = pot, NewCurrentBet = currentBet };
    }

    private BettingResult HandleCall(Participant player, int playerIndex, int toCall, int[] betThisRound, int pot, int currentBet)
    {
        int amount = Math.Min(toCall, player.TotalChips);
        player.TotalChips -= amount;
        pot += amount;
        betThisRound[playerIndex] += amount;

        if (player.TotalChips == 0)
        {
            player.IsAllIn = true;
            _ui.DisplayPlayerAction(player.Name, "calls ALL-IN", amount, pot);
        }
        else
        {
            _ui.DisplayPlayerAction(player.Name, "calls", amount, pot);
        }

        return new BettingResult { Raised = false, NewPot = pot, NewCurrentBet = currentBet };
    }

    private BettingResult HandleRaise(Participant player, int playerIndex, int raiseToTotal, int[] betThisRound, int pot, int currentBet)
    {
        int minRaiseTo = currentBet + _bigBlind;
        raiseToTotal = Math.Max(raiseToTotal, minRaiseTo);

        int toPutIn = raiseToTotal - betThisRound[playerIndex];
        if (toPutIn <= 0) toPutIn = _bigBlind;

        if (toPutIn >= player.TotalChips)
        {
            return HandleAllIn(player, playerIndex, betThisRound, pot, currentBet);
        }

        player.TotalChips -= toPutIn;
        pot += toPutIn;
        betThisRound[playerIndex] += toPutIn;
        currentBet = Math.Max(currentBet, betThisRound[playerIndex]);
        
        _ui.DisplayPlayerAction(player.Name, $"raises to ${currentBet:N0}. Pot", 0, pot);
        return new BettingResult { Raised = true, NewPot = pot, NewCurrentBet = currentBet };
    }
}
