namespace TexAIsHoldem;

public enum Suit
{
    Clubs,
    Diamonds,
    Hearts,
    Spades
}

public enum Rank
{
    Two = 2, Three, Four, Five, Six, Seven, Eight, Nine, Ten,
    Jack, Queen, King, Ace
}

public class Card
{
    public Suit Suit { get; }
    public Rank Rank { get; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public override string ToString()
    {
        string r = Rank switch
        {
            Rank.Ace => "A",
            Rank.King => "K",
            Rank.Queen => "Q",
            Rank.Jack => "J",
            Rank.Ten => "T",
            _ => ((int)Rank).ToString()
        };
        char s = Suit switch
        {
            Suit.Clubs => '\u2663',   // ♣
            Suit.Diamonds => '\u2666', // ♦
            Suit.Hearts => '\u2665',   // ♥
            Suit.Spades => '\u2660',   // ♠
            _ => '?'
        };
        return $"{r}{s}";
    }
}
