using Microsoft.Agents.AI;

namespace TexAIsHoldem;

public interface IActionParser
{
    PlayerAction ParseAction(string actionText, int toCall);
}

public class PlayerAction
{
    public ActionType Type { get; init; }
    public int Amount { get; init; }

    public PlayerAction(ActionType type, int amount = 0)
    {
        Type = type;
        Amount = amount;
    }
}

public enum ActionType
{
    Fold,
    Check,
    Call,
    Raise,
    AllIn
}

public class ActionParser : IActionParser
{
    private readonly int _bigBlind;

    public ActionParser(int bigBlind)
    {
        _bigBlind = bigBlind;
    }

    public PlayerAction ParseAction(string actionText, int toCall)
    {
        var raw = actionText.Trim().ToUpperInvariant();

        if (raw.StartsWith("FOLD") || raw == "F")
            return new PlayerAction(ActionType.Fold);

        if (raw.StartsWith("CHECK") || (raw.StartsWith("CALL") && toCall == 0) || (raw == "C" && toCall == 0))
            return new PlayerAction(ActionType.Check);

        if (raw.StartsWith("ALLIN") || raw == "A")
            return new PlayerAction(ActionType.AllIn);

        if (raw.StartsWith("CALL") || raw == "C")
            return new PlayerAction(ActionType.Call, toCall);

        var raiseMatch = System.Text.RegularExpressions.Regex.Match(raw, @"RAISE\s*(\d+)");
        if (raiseMatch.Success)
        {
            int raiseToTotal = int.Parse(raiseMatch.Groups[1].Value);
            return new PlayerAction(ActionType.Raise, raiseToTotal);
        }

        // Unrecognized action treated as fold:
        return new PlayerAction(ActionType.Fold);
    }
}
