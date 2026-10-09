using Microsoft.Agents.AI;

namespace TexAIsHoldem;

public class PokerGameEngine
{
    private readonly GameConfiguration _config;
    private readonly IGameUI _ui;
    private readonly BettingRound _bettingRound;
    private readonly HandResolver _handResolver;
    private readonly Participant _humanPlayer;
    private readonly List<Participant> _aiPlayers;
    private readonly AIConnectionResult _aiConnection;

    public PokerGameEngine(
        GameConfiguration config,
        IGameUI ui,
        BettingRound bettingRound,
        HandResolver handResolver,
        Participant humanPlayer,
        List<Participant> aiPlayers,
        AIConnectionResult aiConnection)
    {
        _config = config;
        _ui = ui;
        _bettingRound = bettingRound;
        _handResolver = handResolver;
        _humanPlayer = humanPlayer;
        _aiPlayers = aiPlayers;
        _aiConnection = aiConnection;
    }

    public async Task RunAsync()
    {
        int roundNumber = 0;
        int dealerSeat = 0;

        while (ShouldContinueGame())
        {
            roundNumber++;
            dealerSeat = await PlayRoundAsync(roundNumber, dealerSeat);

            _ui.DisplayChipCounts(GetAllParticipants());
            _ui.WaitForNextRound();
        }

        ShowGameOver();
    }

    private bool ShouldContinueGame()
    {
        _aiPlayers.RemoveAll(a => a.TotalChips <= 0);
        return _humanPlayer.TotalChips > 0 && _aiPlayers.Count > 0;
    }

    private async Task<int> PlayRoundAsync(int roundNumber, int dealerSeat)
    {
        _ui.ClearScreen();

        if (roundNumber == 1)
        {
            _ui.DisplayWelcome();
            _ui.DisplayAIStatus(_aiConnection.IsConnected, _aiConnection.Message);
        }

        var participants = GetAllParticipants();
        _ui.DisplayRoundStart(roundNumber, participants);

        var deck = new Deck();
        deck.Shuffle();
        var communityCards = new List<Card>();

        DealHoleCards(participants, deck);
        _ui.DisplayPlayerCards(_humanPlayer.HoleCards);

        var (pot, totalContributions, bbSeat) = PostBlinds(participants, dealerSeat);

        await PlayAllStreets(participants, deck, communityCards, pot, totalContributions, bbSeat);

        var remaining = participants.Where(p => !p.Folded).ToList();
        DealRemainingCommunityCards(deck, communityCards, remaining);

        await _handResolver.ResolveHandAsync(participants, remaining, communityCards, pot, totalContributions);

        return (dealerSeat + 1) % participants.Count;
    }

    private List<Participant> GetAllParticipants()
    {
        var participants = new List<Participant> { _humanPlayer };
        participants.AddRange(_aiPlayers);
        return participants;
    }

    private void DealHoleCards(List<Participant> participants, Deck deck)
    {
        foreach (var p in participants)
        {
            p.HoleCards.Clear();
            p.HoleCards.Add(deck.Draw());
            p.HoleCards.Add(deck.Draw());
            p.Folded = false;
            p.IsAllIn = false;
        }
    }

    private (int pot, int[] totalContributions, int bbSeat) PostBlinds(List<Participant> participants, int dealerSeat)
    {
        int pot = 0;
        var totalContributions = new int[participants.Count];

        int sbSeat = (dealerSeat + 1) % participants.Count;
        int bbSeat = (dealerSeat + 2) % participants.Count;
        
        if (participants.Count == 2)
        {
            sbSeat = dealerSeat;
            bbSeat = (dealerSeat + 1) % participants.Count;
        }

        // Small blind:
        int sbAmount = Math.Min(_config.SmallBlind, participants[sbSeat].TotalChips);
        participants[sbSeat].TotalChips -= sbAmount;
        pot += sbAmount;
        totalContributions[sbSeat] = sbAmount;
        if (participants[sbSeat].TotalChips == 0)
            participants[sbSeat].IsAllIn = true;
        _ui.DisplayBlind(participants[sbSeat].Name, sbAmount, true);

        // Big blind:
        int bbAmount = Math.Min(_config.BigBlind, participants[bbSeat].TotalChips);
        participants[bbSeat].TotalChips -= bbAmount;
        pot += bbAmount;
        totalContributions[bbSeat] = bbAmount;
        if (participants[bbSeat].TotalChips == 0)
            participants[bbSeat].IsAllIn = true;
        _ui.DisplayBlind(participants[bbSeat].Name, bbAmount, false);
        _ui.DisplayPotTotal(pot);
        
        Console.WriteLine();

        return (pot, totalContributions, bbSeat);
    }

    private async Task PlayAllStreets(
        List<Participant> participants,
        Deck deck,
        List<Card> communityCards,
        int initialPot,
        int[] totalContributions,
        int bbSeat)
    {
        int pot = initialPot;
        int currentBet = _config.BigBlind;
        var betThisRound = (int[])totalContributions.Clone();

        string[] streetNames = { "Pre-flop", "Flop", "Turn", "River" };

        for (int street = 0; street <= 3; street++)
        {
            if (participants.Count(p => !p.Folded) < 2)
                break;

            if (street > 0)
            {
                currentBet = 0;
                Array.Clear(betThisRound);
            }

            DealCommunityCards(deck, communityCards, street);

            if (street > 0)
            {
                _ui.DisplayStreetCards(streetNames[street], communityCards, _humanPlayer.HoleCards);
            }

            int canAct = participants.Count(p => !p.Folded && !p.IsAllIn);
            if (canAct < 2)
            {
                _ui.DisplayNoAction(streetNames[street]);
                UpdateContributions(totalContributions, betThisRound, street);
                continue;
            }

            _ui.DisplayBettingStart(streetNames[street]);

            var result = await _bettingRound.ExecuteAsync(
                participants, street, bbSeat, communityCards, pot, currentBet, betThisRound);

            pot = result.Pot;
            currentBet = result.CurrentBet;
            betThisRound = result.BetAmounts;

            UpdateContributions(totalContributions, betThisRound, street);

            _ui.DisplayStreetComplete(pot, streetNames[street]);
        }
    }

    private void DealCommunityCards(Deck deck, List<Card> communityCards, int street)
    {
        if (street == 1)
        {
            deck.Draw(); 
            communityCards.Add(deck.Draw());
            communityCards.Add(deck.Draw());
            communityCards.Add(deck.Draw());
        }
        else if (street == 2)
        {
            deck.Draw(); 
            communityCards.Add(deck.Draw());
        }
        else if (street == 3)
        {
            deck.Draw(); 
            communityCards.Add(deck.Draw());
        }
    }

    private void UpdateContributions(int[] totalContributions, int[] betThisRound, int street)
    {
        if (street == 0)
        {
            for (int i = 0; i < totalContributions.Length; i++)
                totalContributions[i] = betThisRound[i];
        }
        else
        {
            for (int i = 0; i < totalContributions.Length; i++)
                totalContributions[i] += betThisRound[i];
        }
    }

    private void DealRemainingCommunityCards(Deck deck, List<Card> communityCards, List<Participant> remaining)
    {
        if (remaining.Count <= 1)
            return;

        while (communityCards.Count < 5)
        {
            if (communityCards.Count == 0)
            {
                deck.Draw();
                communityCards.Add(deck.Draw());
                communityCards.Add(deck.Draw());
                communityCards.Add(deck.Draw());
            }
            else
            {
                deck.Draw();
                communityCards.Add(deck.Draw());
            }
        }
    }

    private void ShowGameOver()
    {
        _ui.ClearScreen();
        bool playerWon = _humanPlayer.TotalChips > 0;
        _ui.DisplayGameOver(playerWon, _humanPlayer.TotalChips);
    }
}
