using System;
using UnityEngine;

public sealed class DeliveryDynamics : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private CourierRoster roster;
    [SerializeField, Range(0,1)] private float initialStamina = 1f;
    [SerializeField, Min(0)] private float depletionPerSecond = 0.004f;
    [SerializeField, Min(0)] private float depletionBase = 1.5f;
    [SerializeField, Min(0)] private float recoveryPerSecond = 0.003f;
    [SerializeField, Min(0)] private float recoveryBase = 0.5f;
    [SerializeField, Range(0,1)] private float baseAccidentProbability = 0.05f;
    [SerializeField, Min(0)] private float mainRoadRisk = 1f;
    [SerializeField, Min(0)] private float backstreetRisk = 1.5f;
    [SerializeField, Min(0)] private float fatigueRiskScale = 1f;
    [SerializeField, Min(0)] private float accidentStopSeconds = 15f;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int randomSeed = 2718;
    public int? SeedOverride { get; set; }
    private System.Random seedSource;
    public float EquipmentRiskMultiplier=>facilities!=null?facilities.RiskMultiplier:1;
    public float AccidentStopSeconds => accidentStopSeconds;
    private void Awake() => ResetForDay();
    public void ResetForDay()
    {
        seedSource=new System.Random(SeedOverride ?? (useFixedSeed?randomSeed:Environment.TickCount));
        if(roster!=null)foreach(var person in roster.People) ResetPerson(person);
    }
    public void ResetPerson(DeliveryPerson person) {person.CurrentStamina=initialStamina;person.LastReturnedAt=0;}
    public float StaminaAfterTrip(float starting,float endurance,float duration)
        => Mathf.Clamp01(starting-Mathf.Max(0,duration)*depletionPerSecond*Mathf.Max(0,depletionBase-endurance));
    public float StaminaAfterRest(float starting,float endurance,float duration)
        => Mathf.Clamp01(starting+Mathf.Max(0,duration)*recoveryPerSecond*(recoveryBase+endurance));
    public void ApplyTripFatigue(DeliveryPerson person,float starting,float endurance,float duration)
        => person.CurrentStamina=StaminaAfterTrip(starting,endurance,duration);
    public void RecoverStamina(DeliveryPerson person,float duration)
        => person.CurrentStamina=StaminaAfterRest(person.CurrentStamina,person.Endurance,duration);
    public float AccidentProbability(float safety,float stamina,RoadType road)
        => Mathf.Clamp01(baseAccidentProbability*(1-Mathf.Clamp01(safety))
            *(road==RoadType.Backstreet?backstreetRisk:mainRoadRisk)*(1+fatigueRiskScale*(1-Mathf.Clamp01(stamina))));
    public Func<double> CreateRoller()
    {
        if(seedSource==null)seedSource=new System.Random(SeedOverride ?? (useFixedSeed?randomSeed:Environment.TickCount));
        // Each trip owns its stream: frame grouping cannot change another courier's draws.
        var random=new System.Random(seedSource.Next());return random.NextDouble;
    }
}
