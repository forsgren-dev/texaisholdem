namespace TexAIsHoldem;

public class Deck
{
    private readonly List<Card> _cards;
    private int _nextIndex;
    private static readonly Random Rng = new();

    public Deck()
    {
        _cards = new List<Card>(52);
        foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                _cards.Add(new Card(suit, rank));
        _nextIndex = 0;
    }

    public void Shuffle()
    {
        for (int i = _cards.Count - 1; i > 0; i--)
        {
            int j = Rng.Next(i + 1);
            (_cards[i], _cards[j]) = (_cards[j], _cards[i]);
        }
        _nextIndex = 0;
    }

    public Card Draw()
    {
        if (_nextIndex >= _cards.Count)
            throw new InvalidOperationException("Deck is empty.");
        return _cards[_nextIndex++];
    }

    public int Remaining => _cards.Count - _nextIndex;
}
