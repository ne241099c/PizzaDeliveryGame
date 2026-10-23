using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;

/// <summary>Focused regression for unrestricted recruitment, title resume and post-closing playback.</summary>
public static class StaffRouteResumeValidation
{
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static byte[] Bytes(string path)=>File.Exists(path)?File.ReadAllBytes(path):null;
    private static bool Same(byte[] a,byte[] b)=>a==null?b==null:b!=null && a.SequenceEqual(b);
    public static string Run()
    {
        using var sampleMap=new SampleMapValidationScope();
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        int count=0;void Check(bool ok,string message){if(!ok)throw new Exception(message);count++;}
        var save=Find<CampaignSaveManager>();
        var campaign=Find<CampaignManager>();
        var clock=Find<GameClock>();
        var menu=Find<GameMenu>();
        var router=Find<ScreenRouter>();
        var roster=Find<CourierRoster>();
        var crew=Find<KitchenCrew>();
        var recruitment=Find<RecruitmentManager>();
        var economy=Find<BusinessEconomy>();
        var spawner=Find<OrderSpawner>();
        var list=Find<OrderBook>();
        var planner=Find<DeliveryPlanner>();
        var flow=Find<DeliveryFlowController>();
        string originalOverride=save.SavePathOverride,normalPath=save.SavePath,recordPath=save.RecordsPath;
        byte[] before=Bytes(normalPath),records=Bytes(recordPath);
        string folder=Path.Combine(Path.GetTempPath(),"PizzaResumeReview-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        using(var suppression=save.SuppressSaving())
        {
            try
            {
                save.StartNewGame();clock.AutomaticTick=false;spawner.enabled=false;
                var funding=new SerializedObject(economy);float initial=funding.FindProperty("initialCash").floatValue;
                funding.FindProperty("initialCash").floatValue=100000;
                funding.ApplyModifiedPropertiesWithoutUndo();
                economy.NewGame();
                funding.FindProperty("initialCash").floatValue=initial;
                funding.ApplyModifiedPropertiesWithoutUndo();
                // More than the old limit, and more than the original finite name pool.
                while(roster.People.Count<20)
                {
                    recruitment.GenerateDay();
                    foreach(var applicant in recruitment.Applicants.ToArray())if(roster.People.Count<20)Check(recruitment.Hire(applicant),"hiring beyond the former limit");
                }
                Check(recruitment.TeamCount>4 && roster.People.Count==20,"20 couriers plus kitchen staff without expansion");
                Check(roster.People.Select(p=>p.Name).Distinct().Count()==20,"generated names remain unique past the original pool");
                recruitment.GenerateDay();
                var data=save.Capture();data.day=7;string json=JsonUtility.ToJson(data);
                Check(save.TryParse(json,out var loaded,out _) && loaded.people.Count==20,"large roster save is accepted");
                var invalid=save.Capture();
                invalid.crew.Add(new KitchenStaff(roster.People[0].Name,.5f,.5f));
                Check(!save.TryParse(JsonUtility.ToJson(invalid),out _,out _),"duplicate identity is still rejected");
                string fixture=Path.Combine(folder,"campaign.json");File.WriteAllText(fixture,json);save.SavePathOverride=fixture;
                // The title's 続きから starts the Game scene with LaunchMode.Continue, which calls Continue.
                save.Continue();
                Check(router.Current==ScreenId.Hub && clock.Phase==DayPhase.BeforeOpening && campaign.DayNumber==7,"Continue reaches saved preparation");
                Check(roster.People.Count==20,"Continue restores every employee");
                clock.AutomaticTick=true;campaign.StartBusiness();spawner.enabled=false;save.Continue();
                Check(router.Current==ScreenId.Hub && clock.Phase==DayPhase.BeforeOpening && clock.AutomaticTick,"Continue after leaving business restores clock and hub");
                Check(flow.ActiveTrips.Count==0 && list.OrderCount==0,"old trips and orders do not survive resume");
                Check(File.ReadAllText(fixture)==json,"Continue does not overwrite its input save");
                // One outstanding trip at closing. Keep the game-time schedule intact; advance wall time faster.
                save.StartNewGame();clock.AutomaticTick=false;spawner.enabled=false;campaign.StartBusiness();
                clock.Advance(clock.BusinessDuration-1);
                var order=M7Validation.ReadyOrder("Goal1","Closing review",clock.ElapsedSeconds,1000);
                list.AddOrder(order);
                Find<HandoverManager>().Bag(order,roster.People[0]);
                planner.ApplySuggestion();
                planner.Dispatch();
                var trip=roster.People[0].CurrentTrip;
                Check(trip!=null,"late-day departure exists");float returnsAt=trip.ReturnsAt,at=clock.ElapsedSeconds;
                clock.AdvanceAutomatically(.5f);Check(Mathf.Abs(clock.ElapsedSeconds-at-.5f)<.001f,"open hours use normal wall speed");
                clock.AdvanceAutomatically(1f);
                Check(clock.Phase==DayPhase.Closed && Mathf.Abs(clock.ElapsedSeconds-clock.BusinessDuration-.5f*clock.ClosedPlaybackSpeed)<.001f,"frame crossing closing only accelerates its closed remainder");
                Check(Mathf.Approximately(trip.ReturnsAt,returnsAt),"fast playback preserves scheduled arrival and return times");
                float beforeTick=clock.ElapsedSeconds;
                clock.Advance(1);
                Check(Mathf.Abs(clock.ElapsedSeconds-beforeTick-1)<.001f,"deterministic Advance still uses game seconds");
                if(clock.Phase==DayPhase.Closed)clock.AdvanceAutomatically(Mathf.Max(0,returnsAt-clock.ElapsedSeconds)/clock.ClosedPlaybackSpeed+.1f);
                Check(clock.Phase==DayPhase.Ended && flow.ActiveTrips.Count==0,"accelerated playback waits for actual return before settlement");
                // UI fixture used by the separate protected visual review.
            }
            finally
            {
                save.SavePathOverride=originalOverride;
                save.StartNewGame();
                clock.AutomaticTick=false;
                CampaignSaveManager.SessionSavingLocked=true;
                Directory.Delete(folder,true);
            }
        }
        Check(Same(before,Bytes(normalPath)) && Same(records,Bytes(recordPath)),"normal campaign and records remain byte-identical");
        return $"PASS: {count} staff/resume/closing checks.";
    }
    private static IDisposable visualProtection;
    private static string normalCampaignPath,normalRecordsPath;
    private static byte[] normalCampaign,normalRecords;
    public static string BeginVisualReview()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        if(visualProtection!=null)throw new InvalidOperationException("Review already running.");
        var save=Find<CampaignSaveManager>();
        normalCampaignPath=save.SavePath;
        normalRecordsPath=save.RecordsPath;
        normalCampaign=Bytes(normalCampaignPath);
        normalRecords=Bytes(normalRecordsPath);
        visualProtection=save.SuppressSaving();
        save.StartNewGame();var clock=Find<GameClock>();clock.AutomaticTick=false;Find<OrderSpawner>().enabled=false;
        var roster=Find<CourierRoster>();for(int i=0;i<6;i++)roster.AddPerson(new DeliveryPerson("Review "+(i+1),.6f));
        Find<CampaignManager>().StartBusiness();var list=Find<OrderBook>();var handover=Find<HandoverManager>();
        var first=M7Validation.ReadyOrder("Goal1","Route one",0,120);
        var second=M7Validation.ReadyOrder("Goal2","Route two",0,120);
        list.AddOrder(first);
        list.AddOrder(second);
        handover.Bag(first,roster.People[0]);
        handover.Bag(second,roster.People[0]);
        Find<BusinessView>().ShowZone(ShopZone.Dispatch,true);
        return "Protected visual review: eight courier lanes, outbound and return.";
    }
    public static string CheckVisualReview()
    {
        if(!Application.isPlaying || visualProtection==null)throw new InvalidOperationException("Protected review not running.");
        var business=Find<BusinessView>();LineRenderer[] strokes;
        // Route strokes live under the map, outside Canvas.
        strokes=UnityEngine.Object.FindObjectsByType<LineRenderer>(FindObjectsSortMode.None).Where(l=>l.name=="RouteLegStroke" && l.enabled).ToArray();
        if(!strokes.Any(l=>l.startColor==UITheme.Active.tomato) || !strokes.Any(l=>l.startColor==UITheme.Active.info))throw new Exception("outbound/return colours missing");
        if(!UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="RouteDirectionDot"))throw new Exception("moving direction dots missing");
        if(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="RouteDirection" || t.name=="Arrow" && t.parent!=null && t.parent.name=="RouteHandle"))throw new Exception("triangle route markers remain");
        var scroll=business.GetComponentsInChildren<ScrollRect>(true).First(s=>s.name=="LaneViewport");
        if(scroll.content.rect.width<=scroll.viewport.rect.width || business.GetComponentsInChildren<RectTransform>(true).Count(t=>t.name.StartsWith("Lane_"))!=8)throw new Exception("large roster lane scrolling missing");
        if(business.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name.StartsWith("Lane_")).Any(t=>t.rect.width<UITheme.Active.deliveryLaneMinWidth))throw new Exception("lanes became too narrow");
        return "PASS: coloured routes, round direction markers and eight scrollable lanes.";
    }
    public static string EndVisualReview()
    {
        if(visualProtection==null)return "Review not running.";
        try
        {
            var save=Find<CampaignSaveManager>();
            save.StartNewGame();
            Find<GameClock>().AutomaticTick=false;
            CampaignSaveManager.SessionSavingLocked=true;
            if(!Same(normalCampaign,Bytes(normalCampaignPath)) || !Same(normalRecords,Bytes(normalRecordsPath)))throw new Exception("normal save or records changed during visual review");
            return "PASS: normal campaign and records unchanged after visual review.";
        }
        finally{visualProtection.Dispose();visualProtection=null;}
    }

}
