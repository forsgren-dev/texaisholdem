namespace TexAIsHoldem;

public class BettingRound
{
    private readonly IGameUI _ui;
    private readonly IPlayerActionHandler _actionHandler;

    public BettingRound(IGameUI ui, IPlayerActionHandler actionHandler)
    {
        _ui = ui;
        _actionHandler = actionHandler;
    }

    public async Task<BettingRoundResult> ExecuteAsync(
        List<Participant> participants,
        int street,
        int bigBlindSeat,
        List<Card> communityCards,
        int initialPot,
        int initialBet,
        int[] initialBets)
    {
        int pot = initialPot;
        int currentBet = initialBet;
        var betThisRound = (int[])initialBets.Clone();
        
        var hasActed = new bool[participants.Count];
        int startPos = street == 0 ? (bigBlindSeat + 1) % participants.Count : 0;
        int actIndex = startPos;
        int maxIterations = participants.Count * 4;
        int iterations = 0;

        while (iterations++ < maxIterations)
        {
            var player = participants[actIndex];

            if (!player.Folded && !player.IsAllIn)
            {
                if (!hasActed[actIndex] || betThisRound[actIndex] < currentBet)
                {
                    int toCall = Math.Max(0, currentBet - betThisRound[actIndex]);

                    var result = await _actionHandler.GetPlayerActionAsync(
                        player, actIndex, toCall, pot, currentBet, betThisRound, street, communityCards);
                    
                    pot = result.NewPot;
                    currentBet = result.NewCurrentBet;
                    hasActed[actIndex] = true;
                }
            }

            actIndex = (actIndex + 1) % participants.Count;

            if (IsRoundComplete(participants, hasActed, betThisRound, currentBet))
                break;

            if (participants.Count(p => !p.Folded) < 2)
                break;
        }

        return new BettingRoundResult
        {
            Pot = pot,
            CurrentBet = currentBet,
            BetAmounts = betThisRound
        };
    }

    private bool IsRoundComplete(
        List<Participant> participants,
        bool[] hasActed,
        int[] betThisRound,
        int currentBet)
    {
        for (int i = 0; i < participants.Count; i++)
        {
            if (participants[i].Folded || participants[i].IsAllIn)
                continue;
            
            if (!hasActed[i] || betThisRound[i] < currentBet)
                return false;
        }
        
        return true;
    }
}

public class BettingRoundResult
{
    public int Pot { get; init; }
    public int CurrentBet { get; init; }
    public int[] BetAmounts { get; init; } = Array.Empty<int>();
}
