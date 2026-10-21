using UnityEngine;

public sealed class DayStats : MonoBehaviour
{
    public int DeliveredCount { get; private set; }
    public int LateCount { get; private set; }
    public int CancelledCount { get; private set; }
    public int TotalScore { get; private set; }

    public void RecordDelivery(int points, float lateSeconds)
    {
        DeliveredCount++;
        if (lateSeconds > 0) LateCount++;
        TotalScore += points;
    }

    public void RecordCancellation(int count, int penalty)
    {
        CancelledCount += count;
        TotalScore -= count * penalty;
    }
}
