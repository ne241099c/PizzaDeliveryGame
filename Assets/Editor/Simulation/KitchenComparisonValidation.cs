using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

public static class KitchenComparisonValidation
{
    public static string Status {get;private set;}="Not run";
    private static IDisposable protection;private static DaySimulationSession session;
    private static int policy;private static string csv;
    private static CampaignSaveManager save;
    private static string path,recordsPath;private static byte[] before,records;
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    private static byte[] Bytes(string p)=>File.Exists(p)?File.ReadAllBytes(p):null;
    private static bool Same(byte[] a,byte[] b)=>a==null?b==null:b!=null && a.SequenceEqual(b);
    public static string Regression()
    {
        if(!Application.isPlaying)throw new Exception("Play required");
        var save=Find<CampaignSaveManager>();using var scope=save.SuppressSaving();var cooking=Find<CookingManager>();var original=cooking.EditorSubstitute;var clock=Find<GameClock>();clock.AutomaticTick=false;
        int count=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);count++;}
        try
        {
            using(var trial=new DaySimulationSession(Find<DaySimulationSettings>(),daysOverride:1,promisesOverride:new[]{60f},initialCashOverride:10000,citySeedOverride:1401,kitchenStaffOverride:0,hireKitchenOverride:false,substituteOverride:true))
            {
                trial.Step();Check(Find<KitchenCrew>().Count==0 && cooking.EditorSubstitute!=null,"substitute is outside the roster");
                Check(!cooking.ManualControlEnabled && cooking.IsStaffBusy(cooking.EditorSubstitute),"real automatic cooking drives substitute");
                var economy=Find<BusinessEconomy>();Check(economy.TeamWages==Find<CourierRoster>().People.Sum(p=>economy.DailyWage(p)),"substitute has no wages");
                clock.Advance(cooking.CookSeconds+.1f);Check(Find<OrderBook>().Orders.Any(o=>o.IsReady),"substitute completes and packs real pizza");
            }
            Check(cooking.EditorSubstitute==original,"substitute restored after simulation");return $"PASS: {count} kitchen autoplay policy checks.";
        }
        finally{save.StartNewGame();clock.AutomaticTick=false;CampaignSaveManager.SessionSavingLocked=true;}
    }
    public static string Begin()
    {
        if(!Application.isPlaying || protection!=null)throw new Exception("Play required; comparison already active");
        save=Find<CampaignSaveManager>();path=save.SavePath;recordsPath=save.RecordsPath;before=Bytes(path);records=Bytes(recordsPath);protection=save.SuppressSaving();Find<GameClock>().AutomaticTick=false;
        policy=0;csv=SimulationDayRecord.Header+"\n";Start();EditorApplication.update+=Tick;return Status;
    }
    private static void Start()
    {
        string name=new[]{"J","K","L"}[policy];
        session=new DaySimulationSession(Find<DaySimulationSettings>(),batchOverride:true,hireOverride:false,adjustOverride:false,name:name,daysOverride:30,promisesOverride:new[]{60f},trainOverride:false,facilityOverride:policy>0,citySeedOverride:1401,cityBlocksOverride:3,kitchenStaffOverride:policy==2?2:1,hireKitchenOverride:false);
        session.DayCompleted+=r=>{if(r.KitchenStaff!=(policy==2?2:1))throw new Exception("comparison staff count changed");csv+=r.Csv+"\n";};Status="Running "+name;
    }
    private static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Play stopped during kitchen comparison");
            var watch=Stopwatch.StartNew();do{session.Step();}while(!session.Finished && watch.ElapsedMilliseconds<8);
            if(!session.Finished)return;session.Dispose();session=null;policy++;
            if(policy<3){Start();return;}
            Directory.CreateDirectory("Docs/Simulations/MapGeneration");File.WriteAllText("Docs/Simulations/MapGeneration/Kitchen_JKL.csv",csv);Status="PASS: J/K/L recorded with 30-day limit; bankruptcy ends a run.";Cleanup();
        }
        catch(Exception e){Status="FAIL: "+e.Message;UnityEngine.Debug.LogError(Status);Cleanup();}
    }
    private static void Cleanup()
    {
        EditorApplication.update-=Tick;
        try{session?.Dispose();session=null;save.StartNewGame();Find<GameClock>().AutomaticTick=false;CampaignSaveManager.SessionSavingLocked=true;if(!Same(before,Bytes(path)) || !Same(records,Bytes(recordsPath)))Status="FAIL: normal save changed";}
        finally{protection?.Dispose();protection=null;}
    }
}
