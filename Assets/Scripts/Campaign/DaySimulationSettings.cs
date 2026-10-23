using UnityEngine;

/// <summary>Editor autoplay parameters; economic and delivery rules remain on their existing components.</summary>
public sealed class DaySimulationSettings : MonoBehaviour
{
    [SerializeField, Min(1)] private int daysPerPolicy = 10;
    [SerializeField] private float[] promiseSeconds = {30f,60f,120f};
    [SerializeField] private int randomSeed = 20261022;
    [SerializeField, Min(.01f)] private float clockStepSeconds = .25f;
    [SerializeField, Min(1f)] private float maximumDaySeconds = 3600f;
    [SerializeField,Min(-1)] private int initialKitchenStaff=-1;
    [SerializeField] private bool hireKitchenStaff;
    [SerializeField] private bool substituteWhenEmpty=true;
    [SerializeField,Range(0,1)] private float substituteSpeed=.5f,substituteCare=.5f;
    public int InitialKitchenStaff=>initialKitchenStaff;
    public bool HireKitchenStaff=>hireKitchenStaff;
    public bool SubstituteWhenEmpty=>substituteWhenEmpty;
    public KitchenStaff Substitute()=>new KitchenStaff("自動プレイ代役",substituteSpeed,substituteCare);
    [SerializeField] private bool buyFacilities;
    [SerializeField,Min(0)] private float facilityReserve=100;
    public bool BuyFacilities=>buyFacilities;
    public decimal FacilityReserve=>(decimal)facilityReserve;
    [SerializeField] private bool trainCouriers;
    [SerializeField,Min(0)] private float trainingReserve=100;
    [SerializeField,Min(1)] private int comparisonDays=30;
    public bool TrainCouriers=>trainCouriers;
    public decimal TrainingReserve=>(decimal)trainingReserve;
    public int ComparisonDays=>comparisonDays;
    [SerializeField] private bool batchDispatch;
    [SerializeField] private bool hireApplicants;
    [SerializeField] private bool adjustPromise;
    [SerializeField, Min(0)] private float hiringReserve = 100;
    [SerializeField, Min(0)] private float promiseAdjustment = 10;
    [SerializeField, Range(0,100)] private float shortenReputation = 90;
    [SerializeField, Min(1)] private int extendLateCount = 3;
    [SerializeField, Min(1)] private int promiseTrendDays = 2;
    [SerializeField, Min(0)] private float comparisonExtraInitialCash = 600;
    [SerializeField] private string resultsDirectory = "Docs/Simulations/M6_Adjustment";
    [SerializeField] private CourierAbility[] trainingPriority = {
        CourierAbility.Driving, CourierAbility.Capacity, CourierAbility.Safety, CourierAbility.Service };
    public float ComparisonExtraInitialCash => comparisonExtraInitialCash;
    public string ResultsDirectory => resultsDirectory;
    public int PromiseTrend(int late, float reputation)
        => late >= extendLateCount ? 1 : late == 0 && reputation >= shortenReputation ? -1 : 0;
    public int NextTrendStreak(int previousTrend, int previousStreak, int late, float reputation)
    {
        int trend=PromiseTrend(late,reputation);
        return trend==0 ? 0 : trend==previousTrend ? previousStreak+1 : 1;
    }
    public int TrainingPriority(CourierAbility ability) => System.Array.IndexOf(trainingPriority, ability);
    public CourierAbility? PreferredTrainingAbility(DeliveryPerson person)
    {
        foreach (var ability in trainingPriority)
            if ((ability != CourierAbility.Capacity || person.Capacity == 1)
                && person.Ability(ability) < person.Potential(ability)) return ability;
        return null;
    }
    public bool BatchDispatch => batchDispatch;
    public bool HireApplicants => hireApplicants;
    public bool AdjustPromise => adjustPromise;
    public decimal HiringReserve => (decimal)hiringReserve;
    public float AdjustedPromise(float previous, int late, float reputation, int consecutiveDays)
        => consecutiveDays >= promiseTrendDays ? previous + PromiseTrend(late, reputation) * promiseAdjustment : previous;
    public int DaysPerPolicy => daysPerPolicy;
    public float[] PromiseSeconds => (float[])promiseSeconds.Clone();
    public int RandomSeed => randomSeed;
    public float ClockStepSeconds => clockStepSeconds;
    public float MaximumDaySeconds => maximumDaySeconds;
}
