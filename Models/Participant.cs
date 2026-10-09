namespace TexAIsHoldem;

public class Participant
{
    public string Name { get; set; } = "";
    public int TotalChips { get; set; } = 100_000;
    public List<Card> HoleCards { get; } = new();
    public bool IsHuman { get; set; }
    public bool Folded { get; set; }
    public bool IsAllIn { get; set; }
}
