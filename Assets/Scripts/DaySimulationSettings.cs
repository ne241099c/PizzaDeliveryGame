using UnityEngine;

/// <summary>Editor autoplay parameters; economic and delivery rules remain on their existing components.</summary>
public sealed class DaySimulationSettings : MonoBehaviour
{
    [SerializeField, Min(1)] private int daysPerPolicy = 10;
    [SerializeField] private float[] promiseSeconds = {30f,60f,120f};
    [SerializeField] private int randomSeed = 20261022;
    [SerializeField, Min(.01f)] private float clockStepSeconds = .25f;
    [SerializeField, Min(1f)] private float maximumDaySeconds = 3600f;
    [SerializeField] private bool trainCouriers;
    [SerializeField,Min(0)] private float trainingReserve=200;
    [SerializeField,Min(1)] private int comparisonDays=30;
    public bool TrainCouriers=>trainCouriers;
    public decimal TrainingReserve=>(decimal)trainingReserve;
    public int ComparisonDays=>comparisonDays;
    [SerializeField] private bool batchDispatch;
    [SerializeField] private bool hireApplicants;
    [SerializeField] private bool adjustPromise;
    [SerializeField, Min(0)] private float hiringReserve = 200;
    [SerializeField, Min(0)] private float promiseAdjustment = 15;
    [SerializeField, Range(0,100)] private float shortenReputation = 90;
    [SerializeField, Min(1)] private int extendLateCount = 3;
    public bool BatchDispatch => batchDispatch;
    public bool HireApplicants => hireApplicants;
    public bool AdjustPromise => adjustPromise;
    public decimal HiringReserve => (decimal)hiringReserve;
    public float AdjustedPromise(float previous,int late,float reputation) => late>=extendLateCount ? previous+promiseAdjustment : late==0 && reputation>=shortenReputation ? previous-promiseAdjustment : previous;
    public int DaysPerPolicy => daysPerPolicy;
    public float[] PromiseSeconds => (float[])promiseSeconds.Clone();
    public int RandomSeed => randomSeed;
    public float ClockStepSeconds => clockStepSeconds;
    public float MaximumDaySeconds => maximumDaySeconds;
}
