using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Runs the default comparison twice through the game and compares every CSV field.</summary>
public static class M5SimulationValidation
{
    private static DaySimulationSession session;
    private static DaySimulationSettings settings;
    private static string firstCsv;
    private static SimulationDayRecord[] firstRecords;
    private static int pass,checks;
    public static string Status { get; private set; }="Not run";
    public static string LastSummary { get; private set; }="";
    [MenuItem("Tools/Pizza Delivery/Validate M5 Simulation (Play Mode)")]
    public static void Begin()
    {
        if(session!=null)throw new InvalidOperationException("Simulation validation is already running.");
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        if(settings==null)throw new InvalidOperationException("Missing DaySimulationSettings.");
        checks=0;pass=0;firstCsv=null;firstRecords=null;LastSummary="";Status="Running comparison 1/2";
        session=new DaySimulationSession(settings);session.DayCompleted+=LogRecord;
        EditorApplication.update+=Tick;
    }
    private static void Check(bool ok,string name)
    {
        if(!ok)throw new Exception("M5 simulation validation failed: "+name);
        checks++;
    }
    private static void LogRecord(SimulationDayRecord record) => UnityEngine.Debug.Log($"M5 simulation pass {pass+1}: "+record.Csv);
    private static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new InvalidOperationException("Play stopped during simulation validation.");
            var watch=Stopwatch.StartNew();
            do {session.Step();} while(!session.Finished && watch.Elapsed.TotalMilliseconds<12);
            Status=$"Running comparison {pass+1}/2 / {session.Progress}";
            if(!session.Finished)return;
            if(pass==0)
            {
                firstCsv=session.ToCsv();firstRecords=session.Records.ToArray();session.Dispose();pass++;
                session=new DaySimulationSession(settings);session.DayCompleted+=LogRecord;return;
            }
            Check(firstCsv==session.ToCsv(),"same seed reproduces all daily CSV values");
            foreach(float promise in settings.PromiseSeconds)
            {
                var rows=firstRecords.Where(r=>Mathf.Abs(r.Promise-promise)<.01f).ToArray();
                Check(rows.Length>0 && rows.Length<=settings.DaysPerPolicy,"record count for promise "+promise);
                Check(rows.Length==settings.DaysPerPolicy || rows.Last().Bankrupt,"only bankruptcy may stop early "+promise);
                Check(rows.Select(r=>r.Day).SequenceEqual(Enumerable.Range(1,rows.Length)),"consecutive days "+promise);
                Check(rows.All(r=>r.Wages==404 && r.FixedCosts==UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>().FixedCost(r.Day)),"fixed Aoi/Ren team wages and default fixed costs "+promise);
                decimal cash=300;
                foreach(var r in rows)
                {
                    Check(r.Profit==r.Revenue-r.Materials-r.Fuel-r.Wages-r.FixedCosts,"ledger profit "+promise+" day "+r.Day);
                    cash+=r.Profit-r.PreparationCosts;Check(cash==r.Cash,"cash carry "+promise+" day "+r.Day);
                    Check(r.Delivered>=r.Late && r.Delivered>=0 && r.Cancelled>=0 && r.Accidents>=0 && r.Reputation>=0 && r.Reputation<=100,"daily counters and reputation bounds");
                }
            }
            var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
            var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
            Check(campaign.IsBankrupt || campaign.DayNumber==settings.DaysPerPolicy,"final policy stops at day limit or bankruptcy");
            Check(economy.Ledger.Settled,"last day fully settled");
            string root=Directory.GetParent(Application.dataPath).FullName;
            string directory=Path.Combine(root,"Docs/M6_Simulations/legacy");Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"seed-"+settings.RandomSeed+".csv");File.WriteAllText(path,firstCsv);
            LastSummary=string.Join("\n",settings.PromiseSeconds.Select(p=>
            {
                var rows=firstRecords.Where(r=>Mathf.Abs(r.Promise-p)<.01f).ToArray();var last=rows.Last();
                return $"Promise {p:F0}s: {rows.Length} days / revenue {rows.Sum(r=>r.Revenue):F2} / profit {rows.Sum(r=>r.Profit):F2} / cash {last.Cash:F2} / reputation {last.Reputation:F1} / bankrupt {last.Bankrupt} / delivered {rows.Sum(r=>r.Delivered)} / late {rows.Sum(r=>r.Late)} / accidents {rows.Sum(r=>r.Accidents)}";
            }));
            File.WriteAllText(Path.Combine(directory,"README.md"),"# M5 自動プレイ結果\n\nSeed: "+settings.RandomSeed+"; step: "+settings.ClockStepSeconds+"秒。"+string.Join("/",settings.PromiseSeconds)+"秒を各"+settings.DaysPerPolicy+"日上限、Aoi/Ren固定、雇用なし。破産した設定は早期終了。\n\n```text\n"+LastSummary+"\n```\n\n同じ条件で2回走らせ、CSV全フィールドが一致。既存の注文・推奨出発・事故・到着採点・精算を使用。M4-9の人による調整の代わりにはしない。\n");
            Status=$"PASS: {checks} M5 simulation checks / CSV: {path}";
            UnityEngine.Debug.Log(Status+"\n"+LastSummary);Stop();
        }
        catch(Exception e)
        {
            Status="FAIL: "+e.Message;UnityEngine.Debug.LogException(e);Stop();
        }
    }
    private static void Stop() {EditorApplication.update-=Tick;session?.Dispose();session=null;}
}
