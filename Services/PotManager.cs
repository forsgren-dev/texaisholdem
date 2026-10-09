namespace TexAIsHoldem;

public class PotManager
{
    public int CalculatePotSlice(int[] totalContributions, int currentLevel, int previousLevel)
    {
        int potSlice = 0;
        for (int i = 0; i < totalContributions.Length; i++)
        {
            potSlice += Math.Min(totalContributions[i], currentLevel) - Math.Min(totalContributions[i], previousLevel);
        }
        return potSlice;
    }

    public List<int> GetContributionLevels(int[] totalContributions, List<int> eligibleIndices)
    {
        return eligibleIndices
            .Select(idx => totalContributions[idx])
            .Distinct()
            .OrderBy(x => x)
            .ToList();
    }
}
