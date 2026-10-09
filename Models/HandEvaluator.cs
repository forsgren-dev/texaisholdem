namespace TexAIsHoldem;

public enum HandRank
{
    HighCard,
    OnePair,
    TwoPair,
    ThreeOfAKind,
    Straight,
    Flush,
    FullHouse,
    FourOfAKind,
    StraightFlush,
    RoyalFlush
}

public class HandResult
{
    public HandRank Rank { get; set; }
    public int[] TieBreakers { get; set; } = Array.Empty<int>();
    public string Description { get; set; } = "";
}

public static class HandEvaluator
{
    public static HandResult Evaluate(List<Card> holeCards, List<Card> communityCards)
    {
        var allCards = holeCards.Concat(communityCards).ToList();
        HandResult? best = null;

        foreach (var combo in Combinations(allCards, 5))
        {
            var result = EvaluateFive(combo);
            if (best == null || Compare(result, best) > 0)
                best = result;
        }

        return best ?? new HandResult { Description = "No hand" };
    }

    public static int Compare(HandResult a, HandResult b)
    {
        int cmp = a.Rank.CompareTo(b.Rank);
        if (cmp != 0) return cmp;
        for (int i = 0; i < Math.Min(a.TieBreakers.Length, b.TieBreakers.Length); i++)
        {
            cmp = a.TieBreakers[i].CompareTo(b.TieBreakers[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    private static HandResult EvaluateFive(List<Card> cards)
    {
        var sorted = cards.OrderByDescending(c => c.Rank).ToList();
        var ranks = sorted.Select(c => (int)c.Rank).ToArray();

        bool isFlush = sorted.All(c => c.Suit == sorted[0].Suit);
        bool isStraight = CheckStraight(ranks, out int straightHigh);

        var groups = sorted.GroupBy(c => c.Rank)
            .OrderByDescending(g => g.Count())
            .ThenByDescending(g => g.Key)
            .ToList();
        int[] counts = groups.Select(g => g.Count()).ToArray();

        if (isFlush && isStraight)
        {
            if (straightHigh == (int)Rank.Ace)
                return new HandResult { Rank = HandRank.RoyalFlush, TieBreakers = new[] { straightHigh }, Description = "Royal Flush" };
            return new HandResult { Rank = HandRank.StraightFlush, TieBreakers = new[] { straightHigh }, Description = $"Straight Flush, {RankName((Rank)straightHigh)} high" };
        }

        if (counts[0] == 4)
        {
            var quad = groups[0].Key;
            var kicker = groups[1].Key;
            return new HandResult { Rank = HandRank.FourOfAKind, TieBreakers = new[] { (int)quad, (int)kicker }, Description = $"Four of a Kind, {PluralRankName(quad)} with {RankName(kicker)} kicker" };
        }

        if (counts[0] == 3 && counts.Length >= 2 && counts[1] == 2)
        {
            var trip = groups[0].Key;
            var pair = groups[1].Key;
            return new HandResult { Rank = HandRank.FullHouse, TieBreakers = new[] { (int)trip, (int)pair }, Description = $"Full House, {PluralRankName(trip)} full of {PluralRankName(pair)}" };
        }

        if (isFlush)
            return new HandResult { Rank = HandRank.Flush, TieBreakers = ranks, Description = $"Flush, {RankName(sorted[0].Rank)} high" };

        if (isStraight)
            return new HandResult { Rank = HandRank.Straight, TieBreakers = new[] { straightHigh }, Description = $"Straight, {RankName((Rank)straightHigh)} high" };

        if (counts[0] == 3)
        {
            var trip = groups[0].Key;
            var kickers = groups.Skip(1).Select(g => (int)g.Key).OrderByDescending(x => x).ToArray();
            var tb = new[] { (int)trip }.Concat(kickers).ToArray();
            return new HandResult { Rank = HandRank.ThreeOfAKind, TieBreakers = tb, Description = $"Three of a Kind, {PluralRankName(trip)} with {RankName((Rank)kickers[0])} kicker" };
        }

        if (counts[0] == 2 && counts.Length >= 2 && counts[1] == 2)
        {
            var pair1 = groups[0].Key;
            var pair2 = groups[1].Key;
            var highPair = pair1 > pair2 ? pair1 : pair2;
            var lowPair = pair1 > pair2 ? pair2 : pair1;
            var kicker = groups[2].Key;
            return new HandResult { Rank = HandRank.TwoPair, TieBreakers = new[] { (int)highPair, (int)lowPair, (int)kicker }, Description = $"Two Pair, {PluralRankName(highPair)} and {PluralRankName(lowPair)} with {RankName(kicker)} kicker" };
        }

        if (counts[0] == 2)
        {
            var pair = groups[0].Key;
            var kickers = groups.Skip(1).Select(g => (int)g.Key).OrderByDescending(x => x).ToArray();
            var tb = new[] { (int)pair }.Concat(kickers).ToArray();
            return new HandResult { Rank = HandRank.OnePair, TieBreakers = tb, Description = $"Pair of {PluralRankName(pair)} with {RankName((Rank)kickers[0])} kicker" };
        }

        return new HandResult { Rank = HandRank.HighCard, TieBreakers = ranks, Description = $"{RankName(sorted[0].Rank)} high with {RankName(sorted[1].Rank)} kicker" };
    }

    private static bool CheckStraight(int[] ranks, out int highCard)
    {
        highCard = ranks[0];
        if (ranks[0] - ranks[4] == 4 && ranks.Distinct().Count() == 5)
            return true;
        
        if (ranks[0] == (int)Rank.Ace && ranks[1] == (int)Rank.Five &&
            ranks[2] == (int)Rank.Four && ranks[3] == (int)Rank.Three &&
            ranks[4] == (int)Rank.Two)
        {
            highCard = (int)Rank.Five;
            return true;
        }
        return false;
    }

    private static string RankName(Rank r) => r switch
    {
        Rank.Two => "Two",
        Rank.Three => "Three",
        Rank.Four => "Four",
        Rank.Five => "Five",
        Rank.Six => "Six",
        Rank.Seven => "Seven",
        Rank.Eight => "Eight",
        Rank.Nine => "Nine",
        Rank.Ten => "Ten",
        Rank.Jack => "Jack",
        Rank.Queen => "Queen",
        Rank.King => "King",
        Rank.Ace => "Ace",
        _ => r.ToString()
    };

    private static string PluralRankName(Rank r) => r switch
    {
        Rank.Six => "Sixes",
        _ => RankName(r) + "s"
    };

    private static IEnumerable<List<Card>> Combinations(List<Card> cards, int k)
    {
        int n = cards.Count;
        if (k > n) yield break;

        int[] indices = Enumerable.Range(0, k).ToArray();
        yield return indices.Select(i => cards[i]).ToList();

        while (true)
        {
            int i;
            for (i = k - 1; i >= 0; i--)
            {
                if (indices[i] != i + n - k)
                    break;
            }
            if (i < 0) yield break;

            indices[i]++;
            for (int j = i + 1; j < k; j++)
                indices[j] = indices[j - 1] + 1;

            yield return indices.Select(idx => cards[idx]).ToList();
        }
    }
}
