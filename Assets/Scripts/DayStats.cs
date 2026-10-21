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

    public void ResetForDay() { SpawnedCount=ClosingCancelledCount=AccidentCount=DeliveredCount=LateCount=CancelledCount=TotalScore=0;BusySeconds=0; }

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
