using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class M5Validation
{
    [MenuItem("Tools/Pizza Delivery/Validate M5 (Play Mode)")]
    public static void RunMenu() => Debug.Log(Run());
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var orders=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var rep=UnityEngine.Object.FindFirstObjectByType<ReputationManager>();
        var dynamics=UnityEngine.Object.FindFirstObjectByType<DeliveryDynamics>();
        var score=UnityEngine.Object.FindFirstObjectByType<ScoreManager>();
        var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("M5 validation failed: "+name);checks++;};
        check(campaign.DayNumber==1 && clock.Phase==DayPhase.BeforeOpening,"initial preparation day 1");
        clock.AutomaticTick=false;campaign.StartBusiness();
        string legacy=M3Validation.Run();
        check(clock.Phase==DayPhase.Open,"opening button starts business");
        spawner.enabled=false;
        check(economy.Cash==300 && economy.DailyRevenue==0,"initial cash 300");
        foreach(bool late in new[]{false,true})
        {
            var person=roster.People.First(p=>p.Name=="Aoi");
            var order=new Order("Goal1","Revenue test",clock.ElapsedSeconds,late?0:100);
            orders.AddOrder(order);orders.SelectOnly(order);roster.SelectForDispatch(person);
            var suggestion=RoadPathfinder.Suggest(map,orders.SelectedOrders,person);
            decimal beforeCash=economy.Cash,beforeRevenue=economy.DailyRevenue;
            var route=new ManualDeliveryRoute(map);route.TryReplace(suggestion.EdgeIds);
            check(flow.Dispatch(route.BuildCompletion(orders.SelectedOrders,person),person),"actual revenue trip dispatched");
            check(economy.Cash==beforeCash-20,"materials charged immediately on dispatch");
            var delivery=person.CurrentTrip.Deliveries[0];
            while(flow.ActiveTrips.Count>0)clock.Advance(.25f);
            int expected=score.ScoreArrival(delivery.ArrivesAt,order.Deadline,person.Service);
            check(economy.Cash-beforeCash==expected-20 && economy.DailyRevenue-beforeRevenue==expected,"arrival credits exact tip/late-adjusted score");
            check(late ? expected<100 : expected==116,"late reduction or on-time service tip included");
        }
        check(economy.DailyWage(roster.People.First(p=>p.Name=="Aoi"))==220,"Aoi wage formula");
        check(economy.DailyWage(roster.People.First(p=>p.Name=="Ren"))==184,"Ren wage formula");
        check(economy.Ledger.Materials==40 && economy.Ledger.Distance>0,"actual dispatched materials and completed road distance");
        var sampleOrder=new[]{new Order("Goal1","Distance",0,100)};
        var samplePerson=roster.People.First(p=>p.Name=="Aoi");
        var suggested=RoadPathfinder.Suggest(map,sampleOrder,samplePerson);
        var shortRoute=new ManualDeliveryRoute(map);shortRoute.TryReplace(suggested.EdgeIds);
        var clean=new DeliveryTrip(map,shortRoute.BuildCompletion(sampleOrder,samplePerson),samplePerson,0,5);
        var detour=new ManualDeliveryRoute(map);detour.TryReplace(new[]{"Store-N1","Store-N1"}.Concat(suggested.EdgeIds));
        var longer=new DeliveryTrip(map,detour.BuildCompletion(sampleOrder,samplePerson),samplePerson,0,5);
        check(longer.DistanceAt(longer.ReturnsAt)>clean.DistanceAt(clean.ReturnsAt),"detour increases actual distance and fuel");
        var paused=new DeliveryTrip(map,shortRoute.BuildCompletion(sampleOrder,samplePerson),samplePerson,0,5,dynamics,()=>0d);
        paused.AdvanceAccidents(0);
        check(paused.AccidentCount==1 && paused.DistanceAt(5)==0 && paused.DistanceAt(10)==0,"accident stop adds no distance");
        paused.AdvanceAccidents(10000);
        check(Mathf.Abs(paused.DistanceAt(paused.ReturnsAt)-clean.DistanceAt(clean.ReturnsAt))<.001f,"accidents do not increase total fuel distance");
        decimal beforeSettlement=economy.Cash;
        var retained=roster.People.ToArray();rep.RecordArrival(0,0);
        orders.AddOrder(new Order("Goal1","Wait",clock.ElapsedSeconds,60));
        score.AddScore(10);stats.RecordDelivery(10,0);
        dynamics.ApplyTripFatigue(retained[0],1,retained[0].Endurance,100);
        clock.Advance(clock.BusinessDuration+1);
        check(clock.Phase==DayPhase.Ended && flow.ActiveTrips.Count==0,"settlement after close and all returns");
        var ledger=economy.Ledger;
        check(ledger.Settled && ledger.FixedCosts==150 && ledger.Wages==roster.People.Sum(p=>economy.DailyWage(p)),"settlement pays all wages and fixed cost");
        check(ledger.Fuel==economy.FuelCost(ledger.Distance),"fuel uses actual distance");
        check(economy.Cash==beforeSettlement-ledger.Fuel-ledger.Wages-ledger.FixedCosts,"materials are not deducted twice");
        check(economy.Cash==ledger.OpeningCash+ledger.Profit,"profit reconciles to cash");
        decimal carriedCash=economy.Cash;economy.SettleDay();
        check(economy.Cash==carriedCash,"settlement is applied only once");
        float value=rep.Value;int total=campaign.TotalScore;int daily=stats.TotalScore;
        campaign.NextDay();
        check(economy.Cash==carriedCash && economy.DailyRevenue==0,"cash carries and revenue resets");
        check(campaign.DayNumber==2 && clock.Phase==DayPhase.BeforeOpening,"next day returns to preparation");
        check(rep.Value==value && total==daily,"reputation and cumulative score carried");
        check(roster.People.SequenceEqual(retained),"roster objects and abilities carried");
        check(roster.People.All(p=>p.CurrentStamina==1 && p.CurrentTrip==null && p.IsAvailable),"stamina and trips reset");
        check(orders.OrderCount==0 && orders.SelectedOrders.Count==0,"orders and selection reset");
        check(score.Score==0 && stats.TotalScore==0 && stats.DeliveredCount==0 && clock.ElapsedSeconds==0,"daily statistics and clock reset");
        campaign.StartBusiness();check(clock.Phase==DayPhase.Open,"second business can start");
        return legacy+$" / PASS: {checks} M5 checks.";
    }
}
