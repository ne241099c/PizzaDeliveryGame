using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Legacy menu aliases now validate the current campaign, not obsolete four-courier reset rules.</summary>
public static class M4PlayValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate M4 Play Flow")]
    public static void RunMenu() => Debug.Log(Run());
    public static string Run() => M5Validation.Run();
    [MenuItem("Tools/Pizza Delivery/Validate Reset (Play Mode)")]
    public static void ResetMenu() => Debug.Log(CheckReset());
    public static string CheckReset()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();
        var rep=UnityEngine.Object.FindFirstObjectByType<ReputationManager>();
        var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();
        var orders=UnityEngine.Object.FindFirstObjectByType<OrderBook>();
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        bool[] checks={campaign.DayNumber==1 && !campaign.IsBankrupt,clock.Phase==DayPhase.BeforeOpening && clock.ElapsedSeconds==0,
            economy.Cash==300 && economy.DailyRevenue==0,rep.Value==50,
            roster.People.Select(p=>p.Name).SequenceEqual(new[]{"Aoi","Ren"}),
            roster.People.All(p=>p.IsAvailable && p.CurrentStamina==1 && p.CurrentTrip==null),
            stats.TotalScore==0 && stats.DeliveredCount==0 && orders.OrderCount==0 && flow.ActiveTrips.Count==0,
            campaign.TotalRevenue==0 && campaign.BestDailyRevenue==0};
        if(checks.Any(ok=>!ok))throw new Exception("Current campaign reset validation failed.");
        return $"PASS: {checks.Length} campaign reset checks.";
    }
}
