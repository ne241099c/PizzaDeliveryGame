using UnityEngine;

/// <summary>Editor autoplay parameters; economic and delivery rules remain on their existing components.</summary>
public sealed class DaySimulationSettings : MonoBehaviour
{
    [SerializeField, Min(1)] private int daysPerPolicy = 10;
    [SerializeField] private float[] promiseSeconds = {30f,60f,120f};
    [SerializeField] private int randomSeed = 20261022;
    [SerializeField, Min(.01f)] private float clockStepSeconds = .25f;
    [SerializeField, Min(1f)] private float maximumDaySeconds = 3600f;
    public int DaysPerPolicy => daysPerPolicy;
    public float[] PromiseSeconds => (float[])promiseSeconds.Clone();
    public int RandomSeed => randomSeed;
    public float ClockStepSeconds => clockStepSeconds;
    public float MaximumDaySeconds => maximumDaySeconds;
}
