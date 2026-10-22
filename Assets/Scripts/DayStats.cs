using UnityEngine;

public sealed class DayStats : MonoBehaviour
{
    public int SpawnedCount { get; private set; }
    public int ClosingCancelledCount { get; private set; }
    public float BusySeconds { get; private set; }
    public void RecordSpawn() => SpawnedCount++;
    public void RecordReturn(DeliveryTrip trip,float duration) => BusySeconds+=Mathf.Max(0,Mathf.Min(duration,trip.ReturnsAt)-Mathf.Min(duration,trip.StartedAt));
    public float BusyRatio(int people,float duration) => people>0 && duration>0 ? BusySeconds/(people*duration) : 0;
    public int AccidentCount { get; private set; }
    public void RecordAccident() => AccidentCount++;
    public int DeliveredCount { get; private set; }
    public int LateCount { get; private set; }
    public int CancelledCount { get; private set; }
    public int TotalScore { get; private set; }
    /// <summary>Delivered pizzas by quality stars (index 1-3), and how many of the 2/3-star ones were on time.</summary>
    public int[] StarCounts { get; } = new int[4];
    public int TwoStarOnTime { get; private set; }
    public int ThreeStarOnTime { get; private set; }
    public void RecordQuality(int stars,bool onTime)
    {
        stars=Mathf.Clamp(stars,1,3);StarCounts[stars]++;
        if(onTime && stars==2)TwoStarOnTime++;
        if(onTime && stars==3)ThreeStarOnTime++;
    }

    public void ResetForDay() { SpawnedCount=ClosingCancelledCount=AccidentCount=DeliveredCount=LateCount=CancelledCount=TotalScore=0;BusySeconds=0;TwoStarOnTime=ThreeStarOnTime=0;System.Array.Clear(StarCounts,0,StarCounts.Length); }

    public void RecordDelivery(int points, float lateSeconds)
    {
        DeliveredCount++;
        if (lateSeconds > 0) LateCount++;
        TotalScore += points;
    }

    public void RecordCancellation(int count, int penalty, bool closing=false)
    {
        CancelledCount += count; if(closing)ClosingCancelledCount+=count;
        TotalScore -= count * penalty;
    }
}
