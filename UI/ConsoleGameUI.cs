namespace TexAIsHoldem;

public class ConsoleGameUI : IGameUI
{
    private const int BigBlind = 2000;
    
    private static readonly Dictionary<string, ConsoleColor> PlayerColors = new()
    {
        ["Player"] = ConsoleColor.Green,
        ["Aggressive"] = ConsoleColor.Red,
        ["Careful"] = ConsoleColor.Cyan,
        ["Moderate"] = ConsoleColor.Yellow
    };

    private void WriteWithColors(string text)
    {
        var segments = new List<(string text, ConsoleColor? color)>();
        int index = 0;

        while (index < text.Length)
        {
            int nearestIndex = text.Length;
            string? foundName = null;

            foreach (var name in PlayerColors.Keys)
            {
                int nameIndex = text.IndexOf(name, index, StringComparison.Ordinal);
                if (nameIndex >= 0 && nameIndex < nearestIndex)
                {
                    nearestIndex = nameIndex;
                    foundName = name;
                }
            }

            if (foundName != null)
            {
                if (nearestIndex > index)
                    segments.Add((text.Substring(index, nearestIndex - index), null));
                segments.Add((foundName, PlayerColors[foundName]));
                index = nearestIndex + foundName.Length;
            }
            else
            {
                segments.Add((text.Substring(index), null));
                break;
            }
        }

        foreach (var (segmentText, color) in segments)
        {
            if (color.HasValue)
            {
                var prev = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                Console.Write(segmentText);
                Console.ForegroundColor = prev;
            }
            else
            {
                Console.Write(segmentText);
            }
        }
    }

    private void WriteLineWithColors(string text)
    {
        WriteWithColors(text);
        Console.WriteLine();
    }

    private static void WriteColored(string text, ConsoleColor color)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.Write(text);
        Console.ForegroundColor = previousColor;
    }

    private static void WritePlayerCards(List<Card> cards)
    {
        WriteColored("Your cards:", ConsoleColor.Green);
        Console.WriteLine($" {string.Join(" ", cards)}");
    }

    public void DisplayWelcome()
    {
        Console.WriteLine("=== WELCOME TO TEXaiSHOLDEM ===");
    }

    public void DisplayAIStatus(bool isConnected, string message)
    {
        WriteColored(isConnected ? "AI READY: " : "AI FALLBACK: ",
            isConnected ? ConsoleColor.Green : ConsoleColor.Red);
        Console.WriteLine(message);
        Console.WriteLine();
    }

    public void ClearScreen()
    {
        try
        {
            Console.Clear();
        }
        catch (Exception ex) when (ex is IOException || ex is InvalidOperationException)
        {
            // Throwing an exception if there is no console - never to be seen anyware :)
        }
    }

    public void DisplayRoundStart(int roundNumber, List<Participant> participants)
    {
        Console.WriteLine($"=== Texas Hold'em — Round {roundNumber} ===");
        foreach (var p in participants)
            WriteLineWithColors($"  {p.Name}: ${p.TotalChips:N0}");
        Console.WriteLine();
    }

    public void DisplayPlayerCards(List<Card> cards)
    {
        WritePlayerCards(cards);
        Console.WriteLine();
    }

    public void DisplayBlind(string playerName, int amount, bool isSmallBlind)
    {
        string blindType = isSmallBlind ? "small blind" : "big blind";
        WriteLineWithColors($"{playerName} posts {blindType} ${amount:N0}.");
    }

    public void DisplayPotTotal(int pot)
    {
        Console.WriteLine($"Pot: ${pot:N0}");
    }

    public void DisplayStreetCards(string streetName, List<Card> communityCards, List<Card> playerCards)
    {
        WriteColored($"{streetName}:", ConsoleColor.White);
        Console.WriteLine($" {string.Join(" ", communityCards)}");
        WritePlayerCards(playerCards);
    }

    public void DisplayBettingStart(string streetName)
    {
        Console.WriteLine();
        Console.WriteLine($"--- {streetName} betting ---");
    }

    public void DisplayStreetComplete(int pot, string streetName)
    {
        Console.WriteLine($"Pot: ${pot:N0}. {streetName} complete.");
        Console.WriteLine();
    }

    public void DisplayNoAction(string streetName)
    {
        Console.WriteLine($"--- {streetName}: no further action ---");
        Console.WriteLine();
    }

    public void DisplayPlayerAction(string playerName, string action, int amount = 0, int pot = 0)
    {
        WriteWithColors($"{playerName}: ");
        if (amount > 0 && pot > 0)
            Console.WriteLine($"{action} ${amount:N0}. Pot: ${pot:N0}.");
        else if (amount > 0)
            Console.WriteLine($"{action} ${amount:N0}.");
        else
            Console.WriteLine($"{action}.");
    }

    public void DisplayShowdown(List<Card> communityCards, List<(Participant p, List<Card> cards)> playerCards)
    {
        Console.WriteLine("=== Showdown ===");
        var prevColor = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"Board: {string.Join(" ", communityCards)}");
        Console.ForegroundColor = prevColor;
        
        foreach (var (p, cards) in playerCards)
            WriteLineWithColors($"  {p.Name}: {string.Join(" ", cards)}");
        Console.WriteLine();
    }

    public void DisplayHandResult(string playerName, string handDescription)
    {
        WriteLineWithColors($"  {playerName} has {handDescription}");
    }

    public void DisplayAICommentary(string commentary)
    {
        WriteWithColors($"=>{commentary}");
        Console.WriteLine();
    }

    public void DisplayPotWinner(string potLabel, int amount, string playerName, string handDescription)
    {
        WriteLineWithColors($"🏆 {potLabel} ${amount:N0}: {playerName} wins with {handDescription}!");
    }

    public void DisplaySplitPot(string potLabel, int totalAmount, int amountEach, List<string> playerNames)
    {
        WriteLineWithColors($"{potLabel} ${totalAmount:N0}: split ${amountEach:N0} each — {string.Join(", ", playerNames)}.");
    }

    public void DisplayChipCounts(List<Participant> participants)
    {
        Console.WriteLine();
        Console.WriteLine("Chip counts:");
        foreach (var p in participants)
        {
            string tag = !p.IsHuman && p.TotalChips <= 0 ? "  💀 ELIMINATED" : "";
            WriteLineWithColors($"  {p.Name}: ${p.TotalChips:N0}{tag}");
        }
    }

    public void DisplayGameOver(bool playerWon, int finalChips)
    {
        Console.WriteLine("══════════════════════════");
        Console.WriteLine("        GAME OVER");
        Console.WriteLine("══════════════════════════");
        Console.WriteLine();
        
        if (playerWon)
            Console.WriteLine($"🏆 You won with ${finalChips:N0}!");
        else
            Console.WriteLine("💀 You ran out of chips!");
        
        Console.WriteLine();
        Console.WriteLine("Press Enter to exit...");
        Console.ReadLine();
    }

    public void WaitForNextRound()
    {
        Console.WriteLine("\nPress Enter for next round...");
        Console.ReadLine();
    }

    public string GetPlayerAction(Participant player, int pot, int toCall, int totalChips)
    {
        WritePlayerCards(player.HoleCards);
        WriteWithColors($"{player.Name}: ");
        Console.Write($"Pot ${pot:N0}, to call ${toCall:N0}, chips ${totalChips:N0}. [F]old [C]{(toCall == 0 ? "heck" : "all")} [R]aise [A]ll-in: ");
        string? input = Console.ReadLine()?.Trim().ToUpperInvariant();
        Console.WriteLine();
        return NormalizeInput(input ?? "", toCall, totalChips);
    }

    public int GetRaiseAmount(int maxChips)
    {
        Console.Write($"Raise to amount (max ${maxChips:N0}): ");
        string? amountStr = Console.ReadLine()?.Trim()
            ?.Replace(" ", "").Replace(",", "").Replace(".", "").Replace("$", "");
        
        if (int.TryParse(amountStr, out int amount) && amount > 0)
            return Math.Min(amount, maxChips);
        
        return Math.Min(BigBlind, maxChips);
    }

    private string NormalizeInput(string input, int toCall, int totalChips)
    {
        input = input.Trim().ToUpperInvariant();
        if (input.Length == 0) return "FOLD";
        
        char c = input[0];
        if (c == 'F') return "FOLD";
        if (c == 'C') return toCall == 0 ? "CHECK" : "CALL";
        if (c == 'A') return "ALLIN";
        if (c == 'R')
        {
            int amount = GetRaiseAmount(totalChips);
            return "RAISE " + amount;
        }
        
        return input;
    }
}
