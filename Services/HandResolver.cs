using Microsoft.Agents.AI;

namespace TexAIsHoldem;

public class HandResolver
{
    private readonly IGameUI _ui;
    private readonly AIAgent _handJudgeAgent;
    private readonly PotManager _potManager;

    public HandResolver(IGameUI ui, AIAgent handJudgeAgent, PotManager potManager)
    {
        _ui = ui;
        _handJudgeAgent = handJudgeAgent;
        _potManager = potManager;
    }

    public async Task ResolveHandAsync(
        List<Participant> allParticipants,
        List<Participant> remainingPlayers,
        List<Card> communityCards,
        int pot,
        int[] totalContributions)
    {
        if (remainingPlayers.Count == 1)
        {
            remainingPlayers[0].TotalChips += pot;
            _ui.DisplayPlayerAction(remainingPlayers[0].Name, $"wins ${pot:N0} (everyone else folded)");
            return;
        }

        // Showdown:
        var playerCards = remainingPlayers.Select(p => (p, p.HoleCards)).ToList();
        _ui.DisplayShowdown(communityCards, playerCards);

        // Evaluating hands:
        var results = new List<(Participant p, HandResult hand, int idx)>();
        foreach (var p in remainingPlayers)
        {
            int idx = allParticipants.IndexOf(p);
            var hand = HandEvaluator.Evaluate(p.HoleCards, communityCards);
            results.Add((p, hand, idx));
        }

        // Displaying hands and commentary:
        await DisplayHandResultsAsync(results, communityCards);

        // Distributing pots:
        DistributePots(allParticipants, results, totalContributions);
    }

    private async Task DisplayHandResultsAsync(
        List<(Participant p, HandResult hand, int idx)> results,
        List<Card> communityCards)
    {
        foreach (var (p, hand, _) in results)
        {
            _ui.DisplayHandResult(p.Name, hand.Description);
        }

        // AI Commentary:
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Community cards: {string.Join(" ", communityCards)}");
        sb.AppendLine();
        
        foreach (var (p, hand, _) in results)
        {
            sb.AppendLine($"{p.Name} holds {p.HoleCards[0]} {p.HoleCards[1]} — {hand.Description}");
        }
        
        sb.AppendLine();
        sb.AppendLine("Write ONE SHORT sentence commenting on who won and why (e.g., 'Aggressive takes it with the better kicker' or 'Player got lucky on the river').");
        sb.AppendLine("Do NOT repeat the hand descriptions. ONLY output your commentary sentence. No other text.");

        try
        {
            var response = await _handJudgeAgent.RunAsync(sb.ToString());
            string text = response.Text.Trim();
            
            if (!string.IsNullOrWhiteSpace(text))
            {
                _ui.DisplayAICommentary(text);
            }
        }
        catch
        {
            // I am ignoring AI commentary errors :)
        }
    }

    private void DistributePots(
        List<Participant> allParticipants,
        List<(Participant p, HandResult hand, int idx)> results,
        int[] totalContributions)
    {
        var eligibleIndices = results.Select(r => r.idx).ToList();
        var levels = _potManager.GetContributionLevels(totalContributions, eligibleIndices);
        int prevLevel = 0;

        Console.WriteLine();

        foreach (var level in levels)
        {
            int potSlice = _potManager.CalculatePotSlice(totalContributions, level, prevLevel);
            
            if (potSlice <= 0)
            {
                prevLevel = level;
                continue;
            }

            var eligible = results.Where(r => totalContributions[r.idx] >= level).ToList();
            eligible.Sort((a, b) => HandEvaluator.Compare(b.hand, a.hand));
            var winners = eligible.Where(e => HandEvaluator.Compare(e.hand, eligible[0].hand) == 0).ToList();

            string potLabel = prevLevel == 0 ? "Main pot" : "Side pot";

            if (winners.Count == 1)
            {
                winners[0].p.TotalChips += potSlice;
                _ui.DisplayPotWinner(potLabel, potSlice, winners[0].p.Name, winners[0].hand.Description);
            }
            else
            {
                int each = potSlice / winners.Count;
                foreach (var w in winners)
                {
                    w.p.TotalChips += each;
                }
                
                // Handling odd chip:
                if (potSlice % winners.Count != 0)
                {
                    winners[0].p.TotalChips += potSlice % winners.Count;
                }

                var winnerNames = winners.Select(w => w.p.Name).ToList();
                _ui.DisplaySplitPot(potLabel, potSlice, each, winnerNames);
            }

            prevLevel = level;
        }
    }
}
