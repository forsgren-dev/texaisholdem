namespace TexAIsHoldem;

public interface IGameUI
{
    void DisplayWelcome();
    void DisplayAIStatus(bool isConnected, string message);
    void DisplayRoundStart(int roundNumber, List<Participant> participants);
    void DisplayPlayerCards(List<Card> cards);
    void DisplayBlind(string playerName, int amount, bool isSmallBlind);
    void DisplayPotTotal(int pot);
    void DisplayStreetCards(string streetName, List<Card> communityCards, List<Card> playerCards);
    void DisplayBettingStart(string streetName);
    void DisplayStreetComplete(int pot, string streetName);
    void DisplayNoAction(string streetName);
    void DisplayPlayerAction(string playerName, string action, int amount = 0, int pot = 0);
    void DisplayShowdown(List<Card> communityCards, List<(Participant p, List<Card> cards)> playerCards);
    void DisplayHandResult(string playerName, string handDescription);
    void DisplayAICommentary(string commentary);
    void DisplayPotWinner(string potLabel, int amount, string playerName, string handDescription);
    void DisplaySplitPot(string potLabel, int totalAmount, int amountEach, List<string> playerNames);
    void DisplayChipCounts(List<Participant> participants);
    void DisplayGameOver(bool playerWon, int finalChips);
    void WaitForNextRound();
    void ClearScreen();
    string GetPlayerAction(Participant player, int pot, int toCall, int totalChips);
    int GetRaiseAmount(int maxChips);
}
