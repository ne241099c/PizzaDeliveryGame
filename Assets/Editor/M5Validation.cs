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
        int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("M5 validation failed: "+name);checks++;};
        check(campaign.DayNumber==1 && clock.Phase==DayPhase.BeforeOpening,"initial preparation day 1");
        clock.AutomaticTick=false;campaign.StartBusiness();
        string legacy=M3Validation.Run();
        check(clock.Phase==DayPhase.Open,"opening button starts business");
        spawner.enabled=false;
        var retained=roster.People.ToArray();rep.RecordArrival(0,0);
        orders.AddOrder(new Order("Goal1","Wait",clock.ElapsedSeconds,60));
        score.AddScore(10);stats.RecordDelivery(10,0);
        dynamics.ApplyTripFatigue(retained[0],1,retained[0].Endurance,100);
        clock.Advance(clock.BusinessDuration+1);
        check(clock.Phase==DayPhase.Ended && flow.ActiveTrips.Count==0,"settlement after close and all returns");
        float value=rep.Value;int total=campaign.TotalScore;
        campaign.NextDay();
        check(campaign.DayNumber==2 && clock.Phase==DayPhase.BeforeOpening,"next day returns to preparation");
        check(rep.Value==value && total==10,"reputation and cumulative score carried");
        check(roster.People.SequenceEqual(retained),"roster objects and abilities carried");
        check(roster.People.All(p=>p.CurrentStamina==1 && p.CurrentTrip==null && p.IsAvailable),"stamina and trips reset");
        check(orders.OrderCount==0 && orders.SelectedOrders.Count==0,"orders and selection reset");
        check(score.Score==0 && stats.TotalScore==0 && stats.DeliveredCount==0 && clock.ElapsedSeconds==0,"daily statistics and clock reset");
        campaign.StartBusiness();check(clock.Phase==DayPhase.Open,"second business can start");
        return legacy+$" / PASS: {checks} M5 checks.";
    }
}
