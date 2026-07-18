namespace YardMasterSuite.Core;

/// <summary>
/// Pure consist car-count formatting for the train HUD bar.
/// </summary>
public static class CarsDisplay
{
    public static string Format(int? carCount) =>
        carCount is null ? "— Cars" : $"Cars {carCount.Value}";
}
