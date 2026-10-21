using System;

public static class DeliveryScoring
{
    public static int Calculate(float arrivedAt, float deadline, int basePoints, float penaltyPerSecond, float service = 0, float tipRatio = 0)
    {
        double late = Math.Max(0d, (double)arrivedAt - deadline);
        double points = Math.Max(0d, basePoints - late * penaltyPerSecond)
            + (arrivedAt<=deadline ? basePoints*tipRatio*Math.Max(0d,Math.Min(1d,service)) : 0d);
        return (int)Math.Round(points, MidpointRounding.AwayFromZero);
    }

    public static float CancellationAt(float deadline, int basePoints, float penaltyPerSecond)
        => penaltyPerSecond > 0 ? deadline + basePoints / penaltyPerSecond : float.PositiveInfinity;
}
