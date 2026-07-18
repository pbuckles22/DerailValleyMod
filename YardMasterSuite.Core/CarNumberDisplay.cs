using System.Collections.Generic;

namespace YardMasterSuite.Core;

/// <summary>
/// Pure car-number formatting. Loco = N/A; not on a usable loco train = XX;
/// freight numbered from the loco (loco itself not counted).
/// </summary>
public static class CarNumberDisplay
{
    public const string LocoLabel = "Car N/A";
    public const string NotOnTrainLabel = "Car XX";

    public static string Format(bool isLoco, int? freightNumberFromLoco)
    {
        if (isLoco)
        {
            return LocoLabel;
        }

        return freightNumberFromLoco is null
            ? NotOnTrainLabel
            : $"Car {freightNumberFromLoco.Value}";
    }

    /// <summary>
    /// 1-based freight index from the loco along a linear consist (excludes loco).
    /// </summary>
    public static int? FreightNumberFromLoco(
        int locoIndex,
        int carIndex,
        IReadOnlyList<bool> isLocoByIndex)
    {
        if (isLocoByIndex == null || isLocoByIndex.Count == 0)
        {
            return null;
        }

        var count = isLocoByIndex.Count;
        if (locoIndex < 0 || locoIndex >= count || carIndex < 0 || carIndex >= count)
        {
            return null;
        }

        if (isLocoByIndex[carIndex])
        {
            return null;
        }

        var lo = locoIndex < carIndex ? locoIndex : carIndex;
        var hi = locoIndex < carIndex ? carIndex : locoIndex;
        var freight = 0;
        for (var i = lo; i <= hi; i++)
        {
            if (!isLocoByIndex[i])
            {
                freight++;
            }
        }

        return freight > 0 ? freight : null;
    }
}
