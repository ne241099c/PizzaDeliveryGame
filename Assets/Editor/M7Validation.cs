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
        // One apprentice (speed 0.4): assembly 6x1.06, oven 4, packing 2x1.06. Two benches, one oven.
        var mio=UnityEngine.Object.FindFirstObjectByType<KitchenCrew>().People[0];float work=cook.StaffWorkSeconds(mio),pack=cook.StaffPackSeconds(mio),bake=cook.BakeSeconds;
        Check(Mathf.Abs(work-6.36f)<.001f && Mathf.Abs(pack-2.12f)<.001f && bake==4,"apprentice step times");
        var reference=new KitchenStaff("Reference",.5f,.5f);Check(Mathf.Abs(cook.StaffWorkSeconds(reference)+bake+cook.StaffPackSeconds(reference)-cook.CookSeconds)<.001f,"standard staff total equals cook seconds for the promise minimum");
        var first=new Order("Goal1","First pizza",0,100);var second=new Order("Goal2","Second pizza",0,100);list.AddOrder(first);list.AddOrder(second);
        Check(first.Baking==BakingState.Cooking && second.Baking==BakingState.Waiting,"single staff takes the oldest ticket first");
        Check(!list.SelectOnly(first) && !list.SelectOnly(second),"uncooked orders cannot be selected");Check(economy.Ledger.Materials==0,"no materials charged while cooking");
        clock.Advance(work+.001f);Check(cook.JobFor(first).Stage==PizzaStage.Baking && second.Baking==BakingState.Cooking && Mathf.Abs(cook.JobFor(second).StartedAt-work)<.001f,"assembled pizza enters the oven and the next ticket starts at the exact time");
        clock.Advance(2*work+pack-.01f-clock.ElapsedSeconds);Check(!first.IsReady,"packing waits for the staff member");
        clock.Advance(.02f);Check(first.IsReady && first.Stars>=2 && cook.JobFor(second).Stage==PizzaStage.Baking && Mathf.Abs(cook.JobFor(second).OvenStartedAt-2*work)<.001f,"first pizza packed after the second assembly; staff always earn the bake point");
        Check(list.SelectOnly(first),"packed order can be selected");Check(first.Deadline==100,"cooking does not rewrite deadline");
        clock.Advance(2*work+bake+pack+.01f-clock.ElapsedSeconds);Check(second.IsReady,"second pizza completes");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();
        var expired=new Order("Goal1","Expires on the bench",0,5);var next=new Order("Goal2","Next pizza",0,100);list.AddOrder(expired);list.AddOrder(next);clock.Advance(5);
        Check(expired.Status==OrderStatus.Cancelled && !list.Contains(expired),"cooking order expires at deadline");
        Check(cook.JobFor(next)!=null && Mathf.Abs(cook.JobFor(next).StartedAt-5)<.001f,"expired pizza frees the staff member at expiry");clock.Advance(work+bake+pack+.01f);Check(next.IsReady,"replacement cooking completes");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();var closing=new Order("Goal1","Closing cooking",299,100);clock.Advance(299);list.AddOrder(closing);clock.Advance(1);
        Check(closing.Status==OrderStatus.Cancelled && list.OrderCount==0,"closing cancels cooking and queue");
        Check(Mathf.Abs(spawner.MinimumPromiseSeconds-(spawner.ShortestRoundTripSeconds("Goal2")*1.1f+cook.CookSeconds))<.001f,"round trip and cooking minimum");
        spawner.SetPromiseSeconds(30);Check(spawner.PromiseSeconds>=spawner.MinimumPromiseSeconds,"requested 30 respects physical minimum");campaign.NewGame();
        ValidateQuality(Check);ValidateFacilities(Check);ValidateEffects(Check);
        return $"PASS: {count} M7 checks.";
    }
    private static void ValidateQuality(Action<bool,string> check)
    {
        var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();var crew=UnityEngine.Object.FindFirstObjectByType<KitchenCrew>();var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();var reputation=UnityEngine.Object.FindFirstObjectByType<ReputationManager>();
        cook.ManualControlEnabled=true;
        // Hand-made pizzas only: no kitchen staff today.
        Order Start(float promise){campaign.NewGame();crew.Restore(new KitchenStaff[0]);spawner.enabled=false;campaign.StartBusiness();var order=new Order("Goal1","Hand made",0,promise);list.AddOrder(order);return order;}
        void Assemble(Order order,float sauce,Topping topping){check(cook.Claim(order,0) && cook.AddDough(0) && cook.AddSauce(0,sauce) && cook.AddCheese(0) && cook.AddTopping(0,topping),"manual assembly steps in order");}
        foreach(bool late in new[]{false,true})
        {
            var order=Start(late?6:1000);Assemble(order,.75f,Topping.Basil);
            check(cook.PutInOven(0,0) && !cook.CanTakeOut(0),"raw pizza cannot be taken out");clock.Advance(cook.BakeSeconds);
            check(cook.TakeOut(0) && cook.Box(cook.JobFor(order)) && cook.CloseBox(cook.JobFor(order)) && order.IsReady && order.Stars==3,"sauce, correct topping and bake in the band give three stars");
            check(cook.BonusForArrival(order,order.Deadline)==20 && cook.BonusForArrival(order,order.Deadline+.01f)==0,"three-star bonus only on time inclusive");
            float before=reputation.Value;
            list.SelectOnly(order);roster.SelectForDispatch(roster.People[0]);planner.DispatchWithSuggestion();var trip=roster.People[0].CurrentTrip;
            check(trip!=null,"hand-made pizza dispatched through real flow");clock.Advance(trip.ReturnsAt-clock.ElapsedSeconds+1);
            check(economy.Ledger.QualityBonus==(late?0:20) && stats.ThreeStarOnTime==(late?0:1),"real ledger quality bonus without double credit");
            check(late || reputation.Value>=before+1.5f-.001f,"on-time three-star adds the quality reputation bonus");
        }
        var two=Start(1000);Assemble(two,.5f,Topping.Pepperoni);check(cook.PutInOven(0,0),"oven accepts assembled pizza");clock.Advance(cook.BakeSeconds);cook.TakeOut(0);
        var job=cook.JobFor(two);check(!job.SaucePoint && !job.ToppingPoint && job.BakePoint && job.Stars==2,"thin sauce and wrong topping leave one point and two stars");
        check(cook.Box(job) && cook.CloseBox(job) && two.Stars==2 && cook.BonusForArrival(two,5)==8,"two-star bonus");
        var burnt=Start(1000);Assemble(burnt,1,Topping.Basil);cook.PutInOven(0,0);clock.Advance(cook.BurnAt+.01f);check(cook.TakeOut(0) && cook.JobFor(burnt).Burnt && cook.JobFor(burnt).Stars==1,"forgotten pizza burns to one star");
        var a=Start(1000);var b=new Order("Goal2","Second",0,1000);list.AddOrder(b);
        check(cook.Claim(a,0) && cook.ClaimRejection(b,0).Length>0 && !cook.Claim(b,0) && cook.Claim(b,1),"a workbench holds one pizza");
        cook.AddDough(0);cook.AddSauce(0,1);cook.AddCheese(0);cook.AddTopping(0,Topping.Basil);cook.AddDough(1);cook.AddSauce(1,1);cook.AddCheese(1);cook.AddTopping(1,Topping.Basil);
        check(cook.PutInOven(0,0) && !cook.PutInOven(1,0),"an oven holds one pizza");
        // Staff do not take a ticket the player claimed.
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();
        var busy=new Order("Goal1","Staff first",0,1000);list.AddOrder(busy);check(cook.JobFor(busy).Worker==PizzaWorker.Staff,"staff take tickets by default");
        var mine=new Order("Goal2","Mine",0,1000);list.AddOrder(mine);check(mine.Baking==BakingState.Waiting && cook.Claim(mine,1),"player claims a waiting ticket on a free bench");
        var theirs=new Order("Goal1","Theirs",0,1000);list.AddOrder(theirs);clock.Advance(cook.StaffWorkSeconds(crew.People[0])+.01f);
        check(cook.JobFor(mine).Worker==PizzaWorker.Player && cook.JobFor(theirs)!=null && cook.JobFor(theirs).Worker==PizzaWorker.Staff,"staff skip the ticket the player claimed");
        // The counter rack holds six packed boxes.
        var full=Start(1000);for(int i=0;i<cook.RackCapacity;i++)list.AddOrder(ReadyOrder("Goal2","Rack "+i,0,1000));Assemble(full,1,Topping.Basil);cook.PutInOven(0,0);clock.Advance(cook.BakeSeconds);cook.TakeOut(0);cook.Box(cook.JobFor(full));
        check(!cook.CloseBox(cook.JobFor(full)) && cook.CloseRejection(cook.JobFor(full)).Contains("いっぱい"),"full counter rack blocks closing a box");
        cook.ManualControlEnabled=false;check(!cook.Claim(new Order("Goal1","Off",0,10),1),"manual cooking can be disabled for autoplay");cook.ManualControlEnabled=true;
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
        var legacy=save.Capture();legacy.version=1;legacy.facilities=null;legacy.facilityPurchaseCosts=null;check(save.TryParse(JsonUtility.ToJson(legacy),out var migrated,out _) && migrated.version==3 && migrated.facilities.oven==0 && migrated.facilities.workbench==0 && migrated.crew.Count==1 && migrated.crew[0].Name=="Mio","old version one migrates in memory to version three with the starting apprentice");
        campaign.StartBusiness();check(!facilities.Purchase(FacilityType.Oven) && facilities.LastMessage.Contains("preparation"),"business blocks purchases");UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;clock.Advance(clock.BusinessDuration+1000);check(economy.Ledger.EquipmentMaintenance==90 && economy.Ledger.FixedCosts==240,"maintenance in real settlement");
        campaign.NewGame();check(facilities.Ovens==1 && facilities.BikeLevel==1 && facilities.MaximumPeople==4,"new game resets facilities");
    }
    private static void ValidateEffects(Action<bool,string> check)
    {
        var f=UnityEngine.Object.FindFirstObjectByType<FacilityManager>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var dynamics=UnityEngine.Object.FindFirstObjectByType<DeliveryDynamics>();var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();var recruitment=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();
        campaign.NewGame();float baseTime=map.GetTravelTime("Store-N1",roster.People[0]),minimum=spawner.MinimumPromiseSeconds;
        var order=spawner.CreateOrder("Goal1",0);float deadline=order.Deadline;
        f.Restore(new FacilitySaveData{oven=1,bike=1,expansion=1});
        check(Mathf.Abs(map.GetTravelTime("Store-N1",roster.People[0])*1.08f-baseTime)<.0001f,"bike level two speed formula");check(Mathf.Abs(dynamics.EquipmentRiskMultiplier-.9f)<.0001f,"bike level two risk formula");check(recruitment.MaximumPeople==5,"shop level one team limit");check(spawner.MinimumPromiseSeconds<minimum && order.Deadline==deadline,"upgrades update minimum without rewriting old deadlines");
        f.Restore(new FacilitySaveData{oven=2,bike=2,expansion=2});check(Mathf.Abs(map.GetTravelTime("Store-N1",roster.People[0])*1.16f-baseTime)<.0001f && Mathf.Abs(dynamics.EquipmentRiskMultiplier-.8f)<.0001f,"bike level three absolute multipliers");check(recruitment.MaximumPeople==6 && f.Maintenance==180,"maximum slots and maintenance");
        var funding=new SerializedObject(economy);float initial=funding.FindProperty("initialCash").floatValue;funding.FindProperty("initialCash").floatValue=10000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();funding.FindProperty("initialCash").floatValue=initial;funding.ApplyModifiedPropertiesWithoutUndo();
        var people=roster.People.ToList();while(recruitment.TeamCount<6){if(recruitment.Applicants.Count==0)recruitment.GenerateDay();check(recruitment.Hire(recruitment.Applicants[0]),"actual hire into expanded team");}check(recruitment.HireRejection(recruitment.Applicants.FirstOrDefault()).Length>0,"expanded maximum still rejects overflow");
        float interval=spawner.ExpectedInterval;check(f.Purchase(FacilityType.Advertising,3) && Mathf.Abs(spawner.ExpectedInterval*1.3f-interval)<.001f,"advertising demand applied once");
        campaign.StartBusiness();spawner.enabled=false;
        var pizzas=Enumerable.Range(0,4).Select(i=>new Order("Goal1","Oven "+i,0,200)).ToArray();foreach(var pizza in pizzas)list.AddOrder(pizza);
        // The apprentice takes pizza 0; the player bakes pizzas 1 and 2 by hand; pizza 3 waits for a free cook.
        check(cook.OvenCount==3 && cook.JobFor(pizzas[0]).Worker==PizzaWorker.Staff && pizzas[3].Baking==BakingState.Waiting,"one staff member, three ovens");
        foreach(int i in new[]{1,2}){check(cook.Claim(pizzas[i],1) && cook.AddDough(1) && cook.AddSauce(1,1) && cook.AddCheese(1) && cook.AddTopping(1,Topping.Basil) && cook.PutInOven(1,i-1),"hand-made pizza "+i+" enters oven "+(i-1));}
        clock.Advance(cook.StaffWorkSeconds(UnityEngine.Object.FindFirstObjectByType<KitchenCrew>().People[0])+.01f);
        check(Enumerable.Range(0,3).All(i=>cook.OvenJob(i)!=null) && cook.JobFor(pizzas[0]).Oven==2,"three ovens bake at the same time");
        check(cook.JobFor(pizzas[3])!=null && cook.JobFor(pizzas[3]).Worker==PizzaWorker.Staff,"freed staff member starts the waiting ticket");
        var route=new ManualDeliveryRoute(map);route.TryReplace(RoadPathfinder.Suggest(map,new[]{pizzas[0]},roster.People[0]).EdgeIds);var plan=route.BuildCompletion(new[]{pizzas[0]},roster.People[0]);var trip=new DeliveryTrip(map,plan,roster.People[0],clock.ElapsedSeconds,clock.ToRealSeconds(1),dynamics,()=>0);
        float returns=trip.ReturnsAt;f.Restore(new FacilitySaveData());check(trip.ReturnsAt==returns && Mathf.Abs(trip.EquipmentRiskAtDispatch-.8f)<.0001f,"speed and risk frozen at dispatch");
        check(economy.RepairCost(0)==0 && economy.RepairCost(3)==90,"repair cost exact formula");stats.RecordAccident();stats.RecordAccident();clock.Advance(clock.BusinessDuration+1000);check(economy.Ledger.Repairs==60,"real settlement charges repairs");check(f.DemandMultiplier==1,"advertising ends on closing");
        campaign.NewGame();f.Restore(new FacilitySaveData{oven=1,bike=1,expansion=1});campaign.StartBusiness();spawner.enabled=false;clock.Advance(clock.BusinessDuration+1000);check(campaign.IsBankrupt && f.Ovens==1 && f.BikeLevel==1 && f.MaximumPeople==4 && f.LastOperatingState.oven==1,"bankruptcy resets equipment after recording operating levels");campaign.NewGame();
    }
}
