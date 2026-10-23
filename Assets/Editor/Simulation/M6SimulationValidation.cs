using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class M6SimulationValidation
{
    private sealed class Case { public string Name;public bool Batch,Hire,Adjust,Train;public int Days=10;public float Promise=60;public float? InitialCash; }
    private static List<Case> cases;
    private static readonly List<SimulationDayRecord> records=new();
    private static DaySimulationSession session;
    private static DaySimulationSettings settings;
    private static int index,pass,checks;private static bool fullComparison;
    public static IReadOnlyList<SimulationDayRecord> Results=>records;
    private static string firstCsv,savePath;private static byte[] saveBefore;
    public static string Status {get;private set;}="Not run";
    public static string Summary {get;private set;}="";
    [MenuItem("Tools/Pizza Delivery/Validate M6 Simulation (Play Mode)")]
    public static void Begin()=>BeginSuite(false);
    [MenuItem("Tools/Pizza Delivery/Compare M6 A-E + E2 (Play Mode)")]
    public static void BeginComparisons()=>BeginSuite(true);
    private static void BeginSuite(bool full)
    {
        if(session!=null || !Application.isPlaying)throw new InvalidOperationException("Enter Play and finish any running simulation first.");
        settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();cases=new();
        var saving=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();savePath=saving?.SavePath;saveBefore=savePath!=null && File.Exists(savePath)?File.ReadAllBytes(savePath):null;
        fullComparison=full;
        if(full)
        {
            int days=settings.ComparisonDays;
            cases.Add(new Case{Name="A",Days=days});cases.Add(new Case{Name="B",Days=days,Adjust=true});
            cases.Add(new Case{Name="C",Days=days,Batch=true,Hire=true,Adjust=true});
            cases.Add(new Case{Name="D",Days=days,Batch=true,Hire=true,Adjust=true,Train=true});
            cases.Add(new Case{Name="E",Days=days,Promise=30,Train=true});
            cases.Add(new Case{Name="E2",Days=days,Promise=30,Train=true,InitialCash=settings.ComparisonExtraInitialCash});
        }
        else foreach(bool batch in new[]{false,true})foreach(bool hire in new[]{false,true})foreach(bool adjust in new[]{false,true})
            cases.Add(new Case{Name=$"batch{batch}-hire{hire}-adjust{adjust}",Batch=batch,Hire=hire,Adjust=adjust});
        index=pass=checks=0;records.Clear();Summary="";StartCase();EditorApplication.update+=Tick;
    }
    private static void StartCase()
    {
        var c=cases[index];session=new DaySimulationSession(settings,c.Batch,c.Hire,c.Adjust,c.Name,c.Days,new[]{c.Promise},c.Train,c.InitialCash);
        session.DayCompleted+=r=>{if(pass==0)UnityEngine.Debug.Log("M6 "+c.Name+": "+r.Csv);};
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
            decimal cash=c.InitialCash.HasValue?(decimal)c.InitialCash.Value:economy.InitialCash;int hired=0;
            int previousTrend=0,streak=0;
            float expectedPromise=c.Promise;
            var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
            foreach(var r in session.Records)
            {
                expectedPromise=Mathf.Clamp(expectedPromise,spawner.MinimumPromiseSeconds,spawner.MaximumPromiseSeconds);
                Check(Mathf.Abs(r.Promise-expectedPromise)<.001f,"promise follows consecutive-day policy");
                streak=settings.NextTrendStreak(previousTrend,streak,r.Late,r.Reputation);
                previousTrend=settings.PromiseTrend(r.Late,r.Reputation);
                if(c.Adjust)expectedPromise=settings.AdjustedPromise(r.Promise,r.Late,r.Reputation,streak);
                Check(r.FixedCosts==economy.FixedCost(r.Day),"rising fixed costs");
                Check(r.Profit==r.Revenue-r.Materials-r.Fuel-r.Wages-r.FixedCosts-r.Repairs,"profit");
                cash+=r.Profit-r.PreparationCosts;Check(cash==r.Cash,"cash including preparation costs");
                Check(r.Spawned==r.Delivered+r.Cancelled && r.Stock<=r.Cancelled,"spawned and closing stock");
                Check(r.BusyRatio>=0 && r.BusyRatio<=1.0001f,"busy ratio clips overtime");
                Check(!string.IsNullOrEmpty(r.Couriers) && !string.IsNullOrEmpty(r.ExperienceGrowth) && !string.IsNullOrEmpty(r.Potentials),"daily abilities experience and ceilings");
                Check(c.Train?r.TrainingCosts>=0:r.TrainingCosts==0,"training policy and costs");
                var kitchen=UnityEngine.Object.FindFirstObjectByType<CookingManager>();Check(r.QualityBonus==r.TwoStarOnTime*kitchen.TwoStarBonus+r.ThreeStarOnTime*kitchen.ThreeStarBonus,"quality bonus equals on-time two/three-star pizzas");
                hired+=r.Hired;Check(r.RosterSize==2+hired,"roster growth");
            }
            Check(economy.InitialCash==300,"E2 does not change configured starting cash");
            records.AddRange(session.Records);session.Dispose();session=null;
            index++;pass=0;
            if(index<cases.Count){StartCase();return;}
            byte[] after=savePath!=null && File.Exists(savePath)?File.ReadAllBytes(savePath):null;Check(saveBefore==null?after==null:after!=null && saveBefore.SequenceEqual(after),"simulations preserve normal player save");
            var dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,settings.ResultsDirectory);Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir,fullComparison?"comparisons.csv":"policies.csv"),SimulationDayRecord.Header+"\n"+string.Join("\n",records.Select(r=>r.Csv))+"\n");
            if(fullComparison)foreach(var group in records.GroupBy(r=>r.Policy))File.WriteAllText(Path.Combine(dir,group.Key+".csv"),SimulationDayRecord.Header+"\n"+string.Join("\n",group.Select(r=>r.Csv))+"\n");
            Summary=string.Join("\n",records.GroupBy(r=>r.Policy).Select(g=>$"{g.Key}: days {g.Count()}, cash {g.Last().Cash:F2}, reputation {g.Last().Reputation:F1}, busy {g.Average(r=>r.BusyRatio):F3}, hired {g.Sum(r=>r.Hired)}, training {g.Count(r=>r.TrainingCosts>0)}, bankrupt {g.Last().Bankrupt}"));
            Status=$"PASS: {checks} M6 simulation checks";UnityEngine.Debug.Log(Status+"\n"+Summary);EditorApplication.update-=Tick;
        }
        catch(Exception e){Status="FAIL: "+e.Message;session?.Dispose();session=null;EditorApplication.update-=Tick;UnityEngine.Debug.LogException(e);}
    }
}
