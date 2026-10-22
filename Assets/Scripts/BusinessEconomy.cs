using UnityEngine;
using TMPro;

/// <summary>Only confirmed arrival revenue credits cash; daily scores may also contain penalties.</summary>
public sealed class BusinessEconomy : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    private readonly System.Collections.Generic.Dictionary<string,decimal> openingWages=new();
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField, Min(0)] private float materialCostPerOrder = 20f;
    [SerializeField, Min(0)] private float fuelCostPerDistance = .4f;
    [SerializeField, Min(0)] private float wageBase = 50f;
    [SerializeField, Min(0)] private float wageAbilityFactor = 40f;
    [SerializeField, Min(0)] private float wageCapacityFactor = 30f;
    [SerializeField, Min(0)] private float dailyFixedCost = 150f;
    [SerializeField, Min(0)] private float fixedCostGrowthPerDay = 25f;
    private int dayNumber=1;
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField, Min(0)] private float initialCash = 300f;
    public decimal InitialCash => (decimal)initialCash;
    public decimal Cash { get; private set; }
    public DayLedger Ledger { get; private set; }
    public decimal DailyRevenue => Ledger.Revenue;
    public decimal OpeningCash => Ledger.OpeningCash;
    private void Awake() => NewGame();
    private void OnEnable() { flow.RevenueEarned+=CreditArrival; flow.Dispatched+=ChargeMaterials; flow.Returned+=RecordDistance;clock.OnDayStarted+=CaptureWages; }
    private void OnDisable() { flow.RevenueEarned-=CreditArrival; flow.Dispatched-=ChargeMaterials; flow.Returned-=RecordDistance;clock.OnDayStarted-=CaptureWages; }
    private void Update() => cashText.text=$"Cash: {Cash:F2} / Sales: {DailyRevenue:F2}";
    private void CreditArrival(int points) { Cash+=points; Ledger.Revenue+=points; }
    public void RestorePreparation(decimal cash,decimal opening,decimal costs,int day)
    {Cash=cash;dayNumber=day;Ledger=new DayLedger(opening){PreparationCosts=costs};openingWages.Clear();}
    public void NewGame() { Cash=(decimal)initialCash; PrepareDay(); }
    public void PrepareDay(int day=1) { dayNumber=day; Ledger=new DayLedger(Cash); }
    private void CaptureWages() {openingWages.Clear();foreach(var person in roster.People)openingWages[person.Id]=DailyWage(person);}
    public decimal FixedCost(int day) => Money((decimal)dailyFixedCost+(decimal)fixedCostGrowthPerDay*System.Math.Max(0,day-1));
    public static decimal Money(decimal amount) => System.Math.Round(amount,2,System.MidpointRounding.AwayFromZero);
    public decimal MaterialCost(int orderCount) => Money((decimal)materialCostPerOrder*orderCount);
    public decimal FuelCost(decimal distance) => Money((decimal)fuelCostPerDistance*distance);
    public decimal DailyWage(DeliveryPerson person) => Money((decimal)wageBase
        +(decimal)wageAbilityFactor*((decimal)person.Driving+(decimal)person.LocalKnowledge+(decimal)person.Service+(decimal)person.Endurance+(decimal)person.Safety)
        +(decimal)wageCapacityFactor*(person.Capacity-1));
    private void ChargeMaterials(DeliveryTrip trip)
    {
        decimal cost=MaterialCost(trip.Deliveries.Count); Cash-=cost; Ledger.Materials+=cost;
    }
    private void RecordDistance(DeliveryTrip trip) => Ledger.Distance+=(decimal)trip.DistanceAt(trip.ReturnsAt);
    public void RefundPreparation(decimal amount) {if(amount<0 || amount>Ledger.PreparationCosts)throw new System.ArgumentOutOfRangeException(nameof(amount));Cash+=amount;Ledger.PreparationCosts-=amount;}
    public void SpendPreparation(decimal amount)
    {
        if(amount<0)throw new System.ArgumentOutOfRangeException(nameof(amount));
        Cash-=amount;Ledger.PreparationCosts+=amount;
    }
    public void SettleDay()
    {
        if(Ledger.Settled)return;
        Ledger.Fuel=FuelCost(Ledger.Distance);
        foreach(var person in roster.People)Ledger.Wages+=openingWages.TryGetValue(person.Id,out var wage)?wage:DailyWage(person);
        Ledger.FixedCosts=FixedCost(dayNumber);
        Cash-=Ledger.Fuel+Ledger.Wages+Ledger.FixedCosts;
        Ledger.Settled=true;
    }
}
