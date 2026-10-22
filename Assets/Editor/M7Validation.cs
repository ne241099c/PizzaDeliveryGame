using System;
using System.Linq;
using UnityEngine;
using UnityEditor;
public static class M7Validation
{
    // Older route/finance tests start with an explicitly baked pizza; fresh orders use the default Waiting state.
    public static Order ReadyOrder(string destination,string name,float spawned,float promise)=>new Order(destination,name,spawned,promise,BakingState.Ready);
    [MenuItem("Tools/Pizza Delivery/Validate M7 (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("M7: "+name);count++;}
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;campaign.StartBusiness();
        var first=new Order("Goal1","First pizza",0,100);var second=new Order("Goal2","Second pizza",0,100);list.AddOrder(first);list.AddOrder(second);
        Check(first.Baking==BakingState.Cooking && second.Baking==BakingState.Waiting,"single oven FIFO queue");
        Check(!list.SelectOnly(first) && !list.SelectOnly(second),"uncooked orders cannot be selected");Check(economy.Ledger.Materials==0,"no materials charged while cooking");
        clock.Advance(12);Check(first.IsReady && second.Baking==BakingState.Cooking && Mathf.Abs(second.CookStartedAt-12)<.001f,"first completion starts second at exact time");
        Check(list.SelectOnly(first),"baked order can be selected");Check(first.Deadline==100,"cooking does not rewrite deadline");
        clock.Advance(12);Check(second.IsReady,"second pizza completes");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();
        var expired=new Order("Goal1","Expires in oven",0,5);var next=new Order("Goal2","Next pizza",0,100);list.AddOrder(expired);list.AddOrder(next);clock.Advance(5);
        Check(expired.Status==OrderStatus.Cancelled && !list.Contains(expired),"cooking order expires at deadline");
        Check(next.Baking==BakingState.Cooking && Mathf.Abs(next.CookStartedAt-5)<.001f,"expired pizza frees oven at expiry");clock.Advance(12);Check(next.IsReady,"replacement cooking completes");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();var closing=new Order("Goal1","Closing cooking",299,100);clock.Advance(299);list.AddOrder(closing);clock.Advance(1);
        Check(closing.Status==OrderStatus.Cancelled && list.OrderCount==0,"closing cancels cooking and queue");
        Check(Mathf.Abs(spawner.MinimumPromiseSeconds-(spawner.ShortestRoundTripSeconds("Goal2")*1.1f+cook.CookSeconds))<.001f,"round trip and cooking minimum");
        spawner.SetPromiseSeconds(30);Check(spawner.PromiseSeconds>=spawner.MinimumPromiseSeconds,"requested 30 respects physical minimum");campaign.NewGame();
        ValidateQuality(Check);ValidateFacilities(Check);
        return $"PASS: {count} M7 checks.";
    }
    private static void ValidateQuality(Action<bool,string> check)
    {
        var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        check(!cook.IsPerfectProgress(.8499f) && cook.IsPerfectProgress(.85f) && cook.IsPerfectProgress(.95f) && !cook.IsPerfectProgress(.9501f),"perfect-band exact formula boundaries");
        cook.ManualControlEnabled=true;
        foreach(bool late in new[]{false,true})
        {
            campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();var order=new Order("Goal1","Perfect delivery",0,late?12:1000);list.AddOrder(order);clock.Advance(cook.CookSeconds*.84f);
            check(!cook.TryTakeOut(order) && order.Baking==BakingState.Cooking,"early press leaves pizza cooking");clock.Advance(cook.CookSeconds*.06f);
            list.RefreshCookingUI();var content=(Transform)new SerializedObject(list).FindProperty("orderListContent").objectReferenceValue;
            var card=content.GetComponentsInChildren<CookingCardUI>(true).First(v=>v.BoundOrder==order);var takeOut=(UnityEngine.UI.Button)new SerializedObject(card).FindProperty("takeOut").objectReferenceValue;
            check(takeOut.interactable,"real take-out button enables during band");takeOut.onClick.Invoke();check(order.IsReady && order.Quality==BakeQuality.Perfect,"real card button performs timed perfect extraction");
            check(cook.BonusForArrival(order,order.Deadline)==15 && cook.BonusForArrival(order,order.Deadline+.01f)==0,"perfect bonus only on-time inclusive");
            list.SelectOnly(order);roster.SelectForDispatch(roster.People[0]);planner.DispatchWithSuggestion();var trip=roster.People[0].CurrentTrip;
            check(trip!=null,"perfect pizza dispatched through real flow");clock.Advance(trip.ReturnsAt-clock.ElapsedSeconds+1);
            check(economy.Ledger.PerfectBakeBonus==(late?0:15),"real ledger perfect bonus without double credit");
        }
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();var automatic=new Order("Goal1","Automatic",0,100);list.AddOrder(automatic);clock.Advance(cook.CookSeconds);check(automatic.IsReady && automatic.Quality==BakeQuality.Standard && cook.BonusForArrival(automatic,12)==0,"ignored baking automatically standard without penalty");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();var last=new Order("Goal1","Late extraction",0,100);list.AddOrder(last);clock.Advance(cook.CookSeconds*.97f);check(cook.TryTakeOut(last) && last.Quality==BakeQuality.Standard,"press after perfect band yields standard");
        campaign.NewGame();
    }
    private static void ValidateFacilities(Action<bool,string> check)
    {
        var facilities=UnityEngine.Object.FindFirstObjectByType<FacilityManager>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        campaign.NewGame();check(facilities.Ovens==1 && facilities.BikeLevel==1 && facilities.MaximumPeople==4 && facilities.Maintenance==0,"initial facilities");
        check(!facilities.Purchase(FacilityType.Oven) && facilities.LastMessage.Contains("cash"),"insufficient equipment cash rejected");
        var funding=new SerializedObject(economy);funding.FindProperty("initialCash").floatValue=10000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();funding.FindProperty("initialCash").floatValue=300;funding.ApplyModifiedPropertiesWithoutUndo();
        decimal cash=economy.Cash;check(facilities.Purchase(FacilityType.Oven) && economy.Cash==cash-600 && economy.Ledger.FacilityPurchases==600,"oven purchase debits preparation once");
        check(facilities.Purchase(FacilityType.Bike) && facilities.Purchase(FacilityType.Expansion),"bike and shop purchases");check(facilities.Maintenance==90 && economy.FixedCost(1)==240,"maintenance included once in fixed cost");
        check(facilities.Purchase(FacilityType.Advertising,2) && !facilities.Purchase(FacilityType.Advertising,1),"one advertising choice per day");
        string json=save.SnapshotJson();check(save.TryParse(json,out var data,out _) && data.facilities.oven==1 && data.facilities.bike==1 && data.facilities.expansion==1,"facility save schema validates");
        campaign.NewGame();campaign.RestorePreparation(data);check(facilities.Ovens==2 && facilities.BikeLevel==2 && facilities.MaximumPeople==5 && facilities.Advertisement==0,"restore equipment but not advertising");check(economy.Ledger.FacilityPurchases==CampaignSaveManager.Decimal(data.facilityPurchaseCosts) && economy.Cash==CampaignSaveManager.Decimal(data.cash),"facility spending and preparation money round trip");
        var legacy=save.Capture();legacy.version=1;legacy.facilities=null;legacy.facilityPurchaseCosts=null;check(save.TryParse(JsonUtility.ToJson(legacy),out var migrated,out _) && migrated.version==2 && migrated.facilities.oven==0,"old version one migrates in memory");
        campaign.StartBusiness();check(!facilities.Purchase(FacilityType.Oven) && facilities.LastMessage.Contains("preparation"),"business blocks purchases");UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;clock.Advance(clock.BusinessDuration+1000);check(economy.Ledger.EquipmentMaintenance==90 && economy.Ledger.FixedCosts==240,"maintenance in real settlement");
        campaign.NewGame();check(facilities.Ovens==1 && facilities.BikeLevel==1 && facilities.MaximumPeople==4,"new game resets facilities");
    }
}
