using UnityEngine;
using TMPro;

/// <summary>Only confirmed arrival revenue credits cash; daily scores may also contain penalties.</summary>
public sealed class BusinessEconomy : MonoBehaviour
{
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DeliveryPersonSelector roster;
    [SerializeField, Min(0)] private float materialCostPerOrder = 20f;
    [SerializeField, Min(0)] private float fuelCostPerDistance = .4f;
    [SerializeField, Min(0)] private float wageBase = 50f;
    [SerializeField, Min(0)] private float wageAbilityFactor = 40f;
    [SerializeField, Min(0)] private float wageCapacityFactor = 30f;
    [SerializeField, Min(0)] private float dailyFixedCost = 150f;
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField, Min(0)] private float initialCash = 300f;
    public decimal Cash { get; private set; }
    public DayLedger Ledger { get; private set; }
    public decimal DailyRevenue => Ledger.Revenue;
    public decimal OpeningCash => Ledger.OpeningCash;
    private void Awake() => NewGame();
    private void OnEnable() { flow.RevenueEarned+=CreditArrival; flow.Dispatched+=ChargeMaterials; flow.Returned+=RecordDistance; }
    private void OnDisable() { flow.RevenueEarned-=CreditArrival; flow.Dispatched-=ChargeMaterials; flow.Returned-=RecordDistance; }
    private void Update() => cashText.text=$"Cash: {Cash:F2} / Sales: {DailyRevenue:F2}";
    private void CreditArrival(int points) { Cash+=points; Ledger.Revenue+=points; }
    public void NewGame() { Cash=(decimal)initialCash; PrepareDay(); }
    public void PrepareDay() => Ledger=new DayLedger(Cash);
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
    public void SettleDay()
    {
        if(Ledger.Settled)return;
        Ledger.Fuel=FuelCost(Ledger.Distance);
        foreach(var person in roster.People)Ledger.Wages+=DailyWage(person);
        Ledger.FixedCosts=Money((decimal)dailyFixedCost);
        Cash-=Ledger.Fuel+Ledger.Wages+Ledger.FixedCosts;
        Ledger.Settled=true;
    }
}
