using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class M6SimulationValidation
{
    private sealed class Case { public string Name;public bool Batch,Hire,Adjust;public int Days=10;public float Promise=60; }
    private static List<Case> cases;
    private static readonly List<SimulationDayRecord> records=new();
    private static DaySimulationSession session;
    private static DaySimulationSettings settings;
    private static int index,pass,checks;
    private static string firstCsv,savePath;private static byte[] saveBefore;
    public static string Status {get;private set;}="Not run";
    public static string Summary {get;private set;}="";
    [MenuItem("Tools/Pizza Delivery/Validate M6 Simulation (Play Mode)")]
    public static void Begin()
    {
        if(session!=null || !Application.isPlaying)throw new InvalidOperationException("Enter Play and finish any running simulation first.");
        settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();cases=new();
        var saving=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();savePath=saving?.SavePath;saveBefore=savePath!=null && File.Exists(savePath)?File.ReadAllBytes(savePath):null;
        foreach(bool batch in new[]{false,true})foreach(bool hire in new[]{false,true})foreach(bool adjust in new[]{false,true})
            cases.Add(new Case{Name=$"batch{batch}-hire{hire}-adjust{adjust}",Batch=batch,Hire=hire,Adjust=adjust});
        index=pass=checks=0;records.Clear();Summary="";StartCase();EditorApplication.update+=Tick;
    }
    private static void StartCase()
    {
        var c=cases[index];session=new DaySimulationSession(settings,c.Batch,c.Hire,c.Adjust,c.Name,c.Days,new[]{c.Promise});
        Status=$"Running {index+1}/{cases.Count} repeat {pass+1}/2";
    }
    private static void Check(bool ok,string name) {if(!ok)throw new Exception("M6 simulation: "+name);checks++;}
    private static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Play stopped during validation.");
            var watch=Stopwatch.StartNew();do {session.Step();}while(!session.Finished && watch.ElapsedMilliseconds<12);
            if(!session.Finished)return;
            var c=cases[index];
            if(pass==0) {firstCsv=session.ToCsv();session.Dispose();pass=1;StartCase();return;}
            Check(firstCsv==session.ToCsv(),c.Name+" reproducible CSV");
            var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
            Check(session.Records.Count==c.Days || session.Records.Last().Bankrupt,c.Name+" complete or bankrupt");
            decimal cash=300;int hired=0;
            foreach(var r in session.Records)
            {
                Check(r.FixedCosts==economy.FixedCost(r.Day),"rising fixed costs");
                Check(r.Profit==r.Revenue-r.Materials-r.Fuel-r.Wages-r.FixedCosts,"profit");
                cash+=r.Profit-r.PreparationCosts;Check(cash==r.Cash,"cash including preparation costs");
                Check(r.Spawned==r.Delivered+r.Cancelled && r.Stock<=r.Cancelled,"spawned and closing stock");
                Check(r.BusyRatio>=0 && r.BusyRatio<=1.0001f,"busy ratio clips overtime");
                hired+=r.Hired;Check(r.RosterSize==2+hired && r.RosterSize<=4,"roster growth");
            }
            records.AddRange(session.Records);session.Dispose();session=null;
            index++;pass=0;
            if(index<cases.Count){StartCase();return;}
            byte[] after=savePath!=null && File.Exists(savePath)?File.ReadAllBytes(savePath):null;Check(saveBefore==null?after==null:after!=null && saveBefore.SequenceEqual(after),"simulations preserve normal player save");
            var dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs/M6_Simulations");Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir,"policies.csv"),SimulationDayRecord.Header+"\n"+string.Join("\n",records.Select(r=>r.Csv))+"\n");
            Summary=string.Join("\n",records.GroupBy(r=>r.Policy).Select(g=>$"{g.Key}: days {g.Count()}, cash {g.Last().Cash:F2}, reputation {g.Last().Reputation:F1}, busy {g.Average(r=>r.BusyRatio):F3}, hired {g.Sum(r=>r.Hired)}, bankrupt {g.Last().Bankrupt}"));
            Status=$"PASS: {checks} M6 simulation checks";UnityEngine.Debug.Log(Status+"\n"+Summary);EditorApplication.update-=Tick;
        }
        catch(Exception e){Status="FAIL: "+e.Message;session?.Dispose();session=null;EditorApplication.update-=Tick;UnityEngine.Debug.LogException(e);}
    }
}
