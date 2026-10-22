using System;
using UnityEngine;

public enum FacilityType { Oven, Bike, Advertising, Expansion }
[Serializable] public sealed class FacilitySaveData {public int oven,bike,expansion;}

public sealed class FacilityManager : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private float[] ovenPrices={600,1000};
    [SerializeField] private float[] bikePrices={500,900};
    [SerializeField] private float[] expansionPrices={800,1400};
    [SerializeField] private float[] advertisingPrices={100,200,350};
    [SerializeField] private float[] advertisingDemand={1,1.10f,1.20f,1.30f};
    [SerializeField] private float[] bikeSpeed={1,1.08f,1.16f};
    [SerializeField] private float[] bikeRisk={1,.9f,.8f};
    [SerializeField,Min(0)] private float ovenMaintenance=30,bikeMaintenance=20,expansionMaintenance=40;
    [SerializeField,Min(1)] private int initialOvens=1,initialBikeLevel=1,initialMaximumPeople=4;
    [SerializeField] private int implementationStage=3;
    public int ImplementationStage=>implementationStage;
    public int OvenUpgrades {get;private set;}
    public int BikeUpgrades {get;private set;}
    public int ShopUpgrades {get;private set;}
    public int Advertisement {get;private set;}
    public int Ovens=>initialOvens+OvenUpgrades;
    public int BikeLevel=>initialBikeLevel+BikeUpgrades;
    public int MaximumPeople=>initialMaximumPeople+ShopUpgrades;
    public float SpeedMultiplier=>bikeSpeed[BikeUpgrades];
    public float RiskMultiplier=>bikeRisk[BikeUpgrades];
    public float DemandMultiplier=>clock.Phase==DayPhase.Closed || clock.Phase==DayPhase.Ended?1:advertisingDemand[Advertisement];
    public decimal Maintenance=>BusinessEconomy.Money((decimal)ovenMaintenance*OvenUpgrades+(decimal)bikeMaintenance*BikeUpgrades+(decimal)expansionMaintenance*ShopUpgrades);
    public FacilitySaveData LastOperatingState {get;private set;}=new FacilitySaveData();
    public void BeginBusiness()=>LastOperatingState=Capture();
    public string LastMessage {get;private set;}="";
    public event Action Changed;
    public bool IsMaximum(FacilityType type)=>type switch {FacilityType.Oven=>OvenUpgrades>=ovenPrices.Length,FacilityType.Bike=>BikeUpgrades>=bikePrices.Length,FacilityType.Expansion=>ShopUpgrades>=expansionPrices.Length,_=>Advertisement>0};
    public decimal Price(FacilityType type,int advertisementTier=1)
    {
        if(IsMaximum(type))return 0;
        float value=type switch{FacilityType.Oven=>ovenPrices[OvenUpgrades],FacilityType.Bike=>bikePrices[BikeUpgrades],FacilityType.Expansion=>expansionPrices[ShopUpgrades],_=>advertisingPrices[Mathf.Clamp(advertisementTier,1,advertisingPrices.Length)-1]};
        return BusinessEconomy.Money((decimal)value);
    }
    public string Description(FacilityType type)
    {
        int nextBike=Mathf.Min(BikeUpgrades+1,bikeSpeed.Length-1);
        return type switch
        {
            FacilityType.Oven=>$"OVENS / {Ovens}\nNext {Price(type):F2} / +1 oven / maintenance +{ovenMaintenance:F2}/day\n"+Rejection(type),
            FacilityType.Bike=>$"BIKES / Lv {BikeLevel}\nNext {Price(type):F2} / speed x{bikeSpeed[nextBike]:F2} / risk x{bikeRisk[nextBike]:F2}\nMaintenance +{bikeMaintenance:F2}/day\n"+Rejection(type),
            FacilityType.Expansion=>$"SHOP / Team limit {MaximumPeople}\nNext {Price(type):F2} / +1 staff slot / maintenance +{expansionMaintenance:F2}/day\n"+Rejection(type),
            _=>$"ADVERTISING / Today {Advertisement}\n"+string.Join(" / ",System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,advertisingPrices.Length),i=>$"{advertisingPrices[i]:F2}: x{advertisingDemand[i+1]:F2}"))+"\nToday only\n"+Rejection(type)
        };
    }
    public string Rejection(FacilityType type,int advertisementTier=1)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "Buy during preparation only.";
        if(IsMaximum(type))return type==FacilityType.Advertising?"Advertising already booked today.":"Already at maximum.";
        if(advertisementTier<1 || advertisementTier>advertisingPrices.Length)return "Invalid advertising tier.";
        if(economy.Cash<Price(type,advertisementTier))return "Not enough cash.";
        return "";
    }
    public bool Purchase(FacilityType type,int advertisementTier=1)
    {
        LastMessage=Rejection(type,advertisementTier);if(LastMessage.Length>0){Changed?.Invoke();return false;}
        decimal fee=Price(type,advertisementTier);economy.SpendPreparation(fee);economy.Ledger.FacilityPurchases+=fee;
        switch(type){case FacilityType.Oven:OvenUpgrades++;break;case FacilityType.Bike:BikeUpgrades++;break;case FacilityType.Expansion:ShopUpgrades++;break;default:Advertisement=advertisementTier;break;}
        LastMessage="Purchased: "+type;Changed?.Invoke();return true;
    }
    public void NewGame(){OvenUpgrades=BikeUpgrades=ShopUpgrades=Advertisement=0;LastMessage="";}
    public void PrepareDay(){Advertisement=0;LastMessage="";}
    public FacilitySaveData Capture()=>new FacilitySaveData{oven=OvenUpgrades,bike=BikeUpgrades,expansion=ShopUpgrades};
    public bool Valid(FacilitySaveData data)=>data!=null && data.oven>=0 && data.oven<=ovenPrices.Length && data.bike>=0 && data.bike<=bikePrices.Length && data.expansion>=0 && data.expansion<=expansionPrices.Length;
    public int MaximumPeopleFor(FacilitySaveData data)=>initialMaximumPeople+data.expansion;
    public void Restore(FacilitySaveData data){if(!Valid(data))throw new ArgumentException("Invalid facility levels.");OvenUpgrades=data.oven;BikeUpgrades=data.bike;ShopUpgrades=data.expansion;Advertisement=0;Changed?.Invoke();}
}
