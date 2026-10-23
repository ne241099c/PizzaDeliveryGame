using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>FGH runs the real preparation, baking, delivery and accounting twice; no balance adjustment.</summary>
public static class M7SimulationValidation
{
    private static readonly List<SimulationDayRecord> records=new();
    public static IReadOnlyList<SimulationDayRecord> Results=>records;
    private static DaySimulationSession session;
    private static DaySimulationSettings settings;
    private static int index,repeat,checks;
    private static string firstCsv,savePath;private static byte[] saveBefore;
    private static readonly string[] names={"F","G","H"};
    public static string Status {get;private set;}="Not run";
    public static string Summary {get;private set;}="";
    [MenuItem("Tools/Pizza Delivery/Compare M7 F-G-H (Play Mode)")]
    public static void Begin()
    {
        if(!Application.isPlaying || session!=null)throw new InvalidOperationException("Enter Play and finish the previous comparison.");
        settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        savePath=save.SavePath;
        saveBefore=File.Exists(savePath)?File.ReadAllBytes(savePath):null;
        index=repeat=checks=0;records.Clear();Summary="";StartCase();EditorApplication.update+=Tick;
    }
    private static void StartCase()
    {
        session=new DaySimulationSession(settings,true,true,index!=2,names[index],settings.ComparisonDays,new[]{index==2?30f:60f},index==2,null,index!=0);
        Status=$"Running {names[index]} repeat {repeat+1}/2";
    }
    private static void Check(bool ok,string name){if(!ok)throw new Exception("M7 simulation: "+name);checks++;}
    private static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Play stopped.");
            var timer=Stopwatch.StartNew();do{session.Step();}while(!session.Finished && timer.ElapsedMilliseconds<12);
            if(!session.Finished)return;
            if(repeat==0){firstCsv=session.ToCsv();session.Dispose();repeat=1;StartCase();return;}
            Check(firstCsv==session.ToCsv(),names[index]+" identical seed reproduces every CSV field");
            Check(session.Records.Count==settings.ComparisonDays || session.Records.Last().Bankrupt,"30 days or bankruptcy only");
            var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();decimal cash=economy.InitialCash;
            int expectedRoster=2;foreach(var row in session.Records)
            {
                Check(row.Profit==row.Revenue-row.Materials-row.Fuel-row.Wages-row.FixedCosts-row.Repairs,"profit reconciliation");
                cash+=row.Profit-row.PreparationCosts;
                Check(cash==row.Cash,"cash reconciliation including equipment and hiring");
                Check(row.Repairs==economy.RepairCost(row.Accidents),"repair expense formula");
                Check(row.EquipmentMaintenance==row.FixedCosts-economy.BaseFixedCost(row.Day),"maintenance included once");
                Check(row.EquipmentMaintenance>=0 && row.EquipmentMaintenance<=180,"equipment maintenance bounds");
                Check(row.OvenLevel>=1 && row.OvenLevel<=3 && row.BikeLevel>=1 && row.BikeLevel<=3 && row.ShopLevel>=0 && row.ShopLevel<=2,"equipment levels");
                Check(row.OvenUtilization>=0 && row.OvenUtilization<=1.0001f && row.MaximumCookingQueue>=0,"queue and clipped oven utilization");
                var kitchen=UnityEngine.Object.FindFirstObjectByType<CookingManager>();
                Check(row.QualityBonus==row.TwoStarOnTime*kitchen.TwoStarBonus+row.ThreeStarOnTime*kitchen.ThreeStarBonus,"autoplay quality bonus reconciles with stars");
                Check(row.Promise>=row.MinimumPromise-.001f,"physical promise lower bound");
                Check(row.Spawned==row.Delivered+row.Cancelled,"all orders resolved");
                Check(index!=0 || row.FacilityPurchases==0,"F buys no equipment");
                Check(row.FacilityPurchases<=row.PreparationCosts && (row.FacilityPurchases==0)==string.IsNullOrEmpty(row.FacilityPurchase),"one preparation purchase recorded");
                expectedRoster+=row.Hired;Check(row.RosterSize==expectedRoster,"roster growth equals actual hires without a headcount cap");
            }
            records.AddRange(session.Records);session.Dispose();session=null;index++;repeat=0;
            if(index<names.Length){StartCase();return;}
            byte[] after=File.Exists(savePath)?File.ReadAllBytes(savePath):null;
            Check(saveBefore==null?after==null:after!=null && saveBefore.SequenceEqual(after),"normal player save unchanged");
            var dir=Path.Combine(Directory.GetParent(Application.dataPath).FullName,"Docs/Simulations/M7/FGH");Directory.CreateDirectory(dir);
            foreach(var group in records.GroupBy(r=>r.Policy))File.WriteAllText(Path.Combine(dir,group.Key+".csv"),SimulationDayRecord.Header+"\n"+string.Join("\n",group.Select(r=>r.Csv))+"\n");
            Summary=string.Join("\n",records.GroupBy(r=>r.Policy).Select(g=>
            {
                var last=g.Last();var day20=g.FirstOrDefault(r=>r.Day==20);
                return $"{g.Key}: days {g.Count()}, cash {last.Cash:F2}, total profit {g.Sum(r=>r.Profit):F2}, day20 {(day20==null?"not reached":day20.Cash.ToString("F2"))}, bankrupt {last.Bankrupt}, late {g.Sum(r=>r.Late)}, max queue {g.Max(r=>r.MaximumCookingQueue)}, oven utilization {g.Average(r=>r.OvenUtilization):F3}, equipment spending {g.Sum(r=>r.FacilityPurchases):F2}, maintenance {g.Sum(r=>r.EquipmentMaintenance):F2}, requested {g.First().RequestedPromise:F0}s, actual promise {g.Min(r=>r.Promise):F2}-{g.Max(r=>r.Promise):F2}s";
            }));
            File.WriteAllText(Path.Combine(dir,"README.md"),"# M7 F/G/H 測定\n\nSeed "+settings.RandomSeed+"。各方針を同じシードで2回実行し全CSVフィールド一致。数値変更なし。\n\n```text\n"+Summary+"\n```\n\n設備購入は雇用・訓練より先に、窯→バイク→広告(小)→店舗の順で購入可能なものを1日1つ選ぶ。広告を毎日買えるため、広告より下位の店舗拡張に進まない場合がある。Hの30秒指定は往復+調理の物理下限へ補正される。破産で終了した場合は20日目を比較不能と記す。全設備最大時の維持費180/日はC-4の式・実処理で検証済み。目標未達でも数値は調整せず、ユーザー判断待ち。\n");
            Status=$"PASS: {checks} M7 simulation checks";EditorApplication.update-=Tick;UnityEngine.Debug.Log(Status+"\n"+Summary);
        }
        catch(Exception e)
        {
            Status="FAIL: "+e.Message;
            session?.Dispose();
            session=null;
            EditorApplication.update-=Tick;
            UnityEngine.Debug.LogException(e);
        }
    }
}
