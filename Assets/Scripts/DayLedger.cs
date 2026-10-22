/// <summary>One day's confirmed revenue and expenses. Cash movements are applied by BusinessEconomy.</summary>
public sealed class DayLedger
{
    public decimal OpeningCash { get; }
    public decimal PreparationCosts { get; internal set; }
    public decimal FacilityPurchases {get;internal set;}
    public decimal EquipmentMaintenance {get;internal set;}
    public decimal QualityBonus {get;internal set;}
    public decimal Revenue { get; internal set; }
    public decimal Materials { get; internal set; }
    public decimal Distance { get; internal set; }
    public decimal Fuel { get; internal set; }
    public decimal Wages { get; internal set; }
    public decimal FixedCosts { get; internal set; }
    public decimal Repairs {get;internal set;}
    public decimal Profit => Revenue-Materials-Fuel-Wages-FixedCosts-Repairs;
    public bool Settled { get; internal set; }
    public DayLedger(decimal openingCash) => OpeningCash=openingCash;
}
