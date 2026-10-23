using System;
using UnityEngine;

public enum FacilityType { Oven, Bike, Advertising, Expansion, Workbench, Menu }
[Serializable] public sealed class FacilitySaveData {public int oven,bike,expansion,workbench,menu;}

public sealed class FacilityManager : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private BusinessEconomy economy;
    [SerializeField] private float[] ovenPrices={600,1000};
    [SerializeField] private float[] bikePrices={500,900};
    [SerializeField] private float[] expansionPrices={800,1400};
    [SerializeField] private float[] workbenchPrices={400,700};
    [SerializeField] private float[] menuPrices={400,700};
    [SerializeField, Min(0)] private float menuMaintenance=10;
    [SerializeField] private float[] advertisingPrices={100,200,350};
    [SerializeField] private float[] advertisingDemand={1,1.10f,1.20f,1.30f};
    [SerializeField] private float[] bikeSpeed={1,1.08f,1.16f};
    [SerializeField] private float[] bikeRisk={1,.9f,.8f};
    [SerializeField,Min(0)] private float ovenMaintenance=30,bikeMaintenance=20,expansionMaintenance=40,workbenchMaintenance=15;
    [SerializeField,Min(1)] private int initialOvens=1,initialBikeLevel=1,initialWorkbenches=2;
    [SerializeField,Min(1)] private int maximumEquipmentLevel=10;
    [SerializeField,Min(1)] private float additionalUpgradePriceMultiplier=1.6f;
    [SerializeField,Min(0)] private float additionalBikeSpeedPerLevel=.08f;
    [SerializeField,Min(0)] private float additionalBikeRiskReduction=.1f;
    [SerializeField,Range(0,1)] private float minimumBikeRisk=.2f;
    [SerializeField] private int implementationStage=3;
    public int ImplementationStage=>implementationStage;
    public int OvenUpgrades {get;private set;}
    public int BikeUpgrades {get;private set;}
    public int ShopUpgrades {get;private set;}
    public int WorkbenchUpgrades {get;private set;}
    public int MenuUpgrades {get;private set;}
    public int UnlockedMenuCount=>Mathf.Max(MenuUpgrades+1,FindFirstObjectByType<CampaignManager>()?.Rewards.UnlockedMenuKinds??1);
    public bool IsUnlocked(PizzaKind kind)=>(int)kind<=MenuUpgrades || (int)kind<(FindFirstObjectByType<CampaignManager>()?.Rewards.UnlockedMenuKinds??1);
    public int Workbenches=>initialWorkbenches+WorkbenchUpgrades;
    public int Advertisement {get;private set;}
    public int Ovens=>initialOvens+OvenUpgrades;
    public int BikeLevel=>initialBikeLevel+BikeUpgrades;
    public int MaximumPeople=>int.MaxValue; // Legacy API; staffing is unlimited.
    public float SpeedMultiplier=>BikeSpeed(BikeUpgrades);
    public float RiskMultiplier=>BikeRisk(BikeUpgrades);
    public float DemandMultiplier=>clock.Phase==DayPhase.Closed || clock.Phase==DayPhase.Ended?1:advertisingDemand[Advertisement];
    public decimal Maintenance=>BusinessEconomy.Money((decimal)ovenMaintenance*OvenUpgrades+(decimal)bikeMaintenance*BikeUpgrades+(decimal)expansionMaintenance*ShopUpgrades+(decimal)workbenchMaintenance*WorkbenchUpgrades+(decimal)menuMaintenance*MenuUpgrades);
    public FacilitySaveData LastOperatingState {get;private set;}=new FacilitySaveData();
    public void BeginBusiness()=>LastOperatingState=Capture();
    public string LastMessage {get;private set;}="";
    public event Action Changed;
    public bool IsMaximum(FacilityType type)=>type switch {FacilityType.Oven=>OvenUpgrades>=MaximumUpgrades(type),FacilityType.Bike=>BikeUpgrades>=MaximumUpgrades(type),FacilityType.Expansion=>ShopUpgrades>=expansionPrices.Length,FacilityType.Workbench=>WorkbenchUpgrades>=MaximumUpgrades(type),FacilityType.Menu=>MenuUpgrades>=menuPrices.Length,_=>Advertisement>0};
    public float MaintenanceOf(FacilityType type)=>type switch{FacilityType.Oven=>ovenMaintenance,FacilityType.Bike=>bikeMaintenance,FacilityType.Expansion=>expansionMaintenance,FacilityType.Workbench=>workbenchMaintenance,FacilityType.Menu=>menuMaintenance,_=>0};
    public int MaximumUpgrades(FacilityType type)=>type switch
    {
        FacilityType.Oven=>Mathf.Max(ovenPrices.Length,maximumEquipmentLevel-initialOvens),
        FacilityType.Bike=>Mathf.Max(bikePrices.Length,maximumEquipmentLevel-initialBikeLevel),
        FacilityType.Workbench=>Mathf.Max(workbenchPrices.Length,maximumEquipmentLevel-initialWorkbenches),
        FacilityType.Menu=>menuPrices.Length,FacilityType.Expansion=>expansionPrices.Length,_=>0
    };
    public int MaximumLevel(FacilityType type)=>MaximumUpgrades(type)+(type switch
    {FacilityType.Oven=>initialOvens,FacilityType.Bike=>initialBikeLevel,FacilityType.Workbench=>initialWorkbenches,_=>1});
    private float BikeSpeed(int upgrades)=>bikeSpeed[Mathf.Min(upgrades,bikeSpeed.Length-1)]+Mathf.Max(0,upgrades-bikeSpeed.Length+1)*additionalBikeSpeedPerLevel;
    private float BikeRisk(int upgrades)=>Mathf.Max(minimumBikeRisk,bikeRisk[Mathf.Min(upgrades,bikeRisk.Length-1)]-Mathf.Max(0,upgrades-bikeRisk.Length+1)*additionalBikeRiskReduction);
    private float UpgradePrice(float[] prices,int upgrades)=>prices[Mathf.Min(upgrades,prices.Length-1)]*Mathf.Pow(additionalUpgradePriceMultiplier,Mathf.Max(0,upgrades-prices.Length+1));
    public float NextBikeSpeed=>BikeSpeed(Mathf.Min(BikeUpgrades+1,MaximumUpgrades(FacilityType.Bike)));
    public float NextBikeRisk=>BikeRisk(Mathf.Min(BikeUpgrades+1,MaximumUpgrades(FacilityType.Bike)));
    public int AdvertisingTiers=>advertisingPrices.Length;
    public float AdvertisingDemand(int tier)=>advertisingDemand[Mathf.Clamp(tier,0,advertisingDemand.Length-1)];
    public decimal Price(FacilityType type,int advertisementTier=1)
    {
        if(IsMaximum(type))return 0;
        float value=type switch{FacilityType.Oven=>UpgradePrice(ovenPrices,OvenUpgrades),FacilityType.Bike=>UpgradePrice(bikePrices,BikeUpgrades),FacilityType.Expansion=>expansionPrices[ShopUpgrades],FacilityType.Workbench=>UpgradePrice(workbenchPrices,WorkbenchUpgrades),FacilityType.Menu=>menuPrices[MenuUpgrades],_=>advertisingPrices[Mathf.Clamp(advertisementTier,1,advertisingPrices.Length)-1]};
        return BusinessEconomy.Money((decimal)value);
    }
    public string Description(FacilityType type)
    {
        return type switch
        {
            FacilityType.Oven=>$"OVENS / {Ovens}\nNext {Price(type):F2} / +1 oven / maintenance +{ovenMaintenance:F2}/day\n"+Rejection(type),
            FacilityType.Bike=>$"BIKES / Lv {BikeLevel}\nNext {Price(type):F2} / speed x{NextBikeSpeed:F2} / risk x{NextBikeRisk:F2}\nMaintenance +{bikeMaintenance:F2}/day\n"+Rejection(type),
            FacilityType.Menu=>$"メニュー拡張 / {MenuUpgrades+1}種類 / 次 {Price(type):F0} / 維持費 +{menuMaintenance}/日",
            FacilityType.Expansion=>"SHOP / Staff hiring has no limit. Legacy expansion data is retained.",
            FacilityType.Workbench=>$"WORKBENCHES / {Workbenches}\nNext {Price(type):F2} / +1 workbench / maintenance +{workbenchMaintenance:F2}/day\n"+Rejection(type),
            _=>$"ADVERTISING / Today {Advertisement}\n"+string.Join(" / ",System.Linq.Enumerable.Select(System.Linq.Enumerable.Range(0,advertisingPrices.Length),i=>$"{advertisingPrices[i]:F2}: x{advertisingDemand[i+1]:F2}"))+"\nToday only\n"+Rejection(type)
        };
    }
    public string Rejection(FacilityType type,int advertisementTier=1)
    {
        if(clock.Phase!=DayPhase.BeforeOpening)return "Buy during preparation only.";
        var goals=FindFirstObjectByType<CampaignManager>();
        if(goals!=null && (type==FacilityType.Oven || type==FacilityType.Bike || type==FacilityType.Workbench) && (type==FacilityType.Oven?Ovens:type==FacilityType.Bike?BikeLevel:Workbenches)>=goals.Rewards.UnlockedEquipmentLevel)return "店グレードを上げると次の設備が解放されます。";
        if(type==FacilityType.Menu && goals!=null && goals.Rewards.UnlockedMenuKinds>MenuUpgrades+1)return "店の昇格ですでに解放されています。";
        if(IsMaximum(type))return type==FacilityType.Advertising?"Advertising already booked today.":"Already at maximum.";
        if(advertisementTier<1 || advertisementTier>advertisingPrices.Length)return "Invalid advertising tier.";
        if(economy.Cash<Price(type,advertisementTier))return "Not enough cash.";
        return "";
    }
    public bool Purchase(FacilityType type,int advertisementTier=1)
    {
        LastMessage=Rejection(type,advertisementTier);if(LastMessage.Length>0){Changed?.Invoke();return false;}
        decimal fee=Price(type,advertisementTier);economy.SpendPreparation(fee);economy.Ledger.FacilityPurchases+=fee;
        switch(type)
        {
            case FacilityType.Oven:OvenUpgrades++;
            break;
            case FacilityType.Bike:BikeUpgrades++;
            break;
            case FacilityType.Expansion:ShopUpgrades++;
            break;
            case FacilityType.Workbench:WorkbenchUpgrades++;
            break;
            case FacilityType.Menu:MenuUpgrades++;
            break;
            default:Advertisement=advertisementTier;
            break;
        }
        LastMessage="Purchased: "+type;Changed?.Invoke();return true;
    }
    public void NewGame(){OvenUpgrades=BikeUpgrades=ShopUpgrades=WorkbenchUpgrades=MenuUpgrades=Advertisement=0;LastMessage="";}
    public void PrepareDay(){Advertisement=0;LastMessage="";}
    public FacilitySaveData Capture()=>new FacilitySaveData{oven=OvenUpgrades,bike=BikeUpgrades,expansion=ShopUpgrades,workbench=WorkbenchUpgrades,menu=MenuUpgrades};
    public bool Valid(FacilitySaveData data)=>data!=null && data.oven>=0 && data.oven<=MaximumUpgrades(FacilityType.Oven) && data.bike>=0 && data.bike<=MaximumUpgrades(FacilityType.Bike) && data.expansion>=0 && data.expansion<=expansionPrices.Length && data.workbench>=0 && data.workbench<=MaximumUpgrades(FacilityType.Workbench) && data.menu>=0 && data.menu<=menuPrices.Length;
    public int MaximumPeopleFor(FacilitySaveData data)=>int.MaxValue;
    public void Restore(FacilitySaveData data)
    {
        if(!Valid(data))throw new ArgumentException("Invalid facility levels.");
        OvenUpgrades=data.oven;
        BikeUpgrades=data.bike;
        ShopUpgrades=data.expansion;
        WorkbenchUpgrades=data.workbench;
        MenuUpgrades=data.menu;
        Advertisement=0;
        Changed?.Invoke();
    }
}
