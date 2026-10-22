using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>Map safety and balance measurements drive existing cooking, routing, scoring and settlement.</summary>
public static class CitySimulationValidation
{
    public static string Status {get;private set;}="Not run";
    private static IDisposable protection;private static CampaignSaveManager save;private static MapManager map;private static GameClock clock;
    private static DaySimulationSession session;private static DaySimulationSettings settings;
    private static int run;private static StringBuilder csv;private static int mainRoads,backRoads;private static int? seed,blocks;private static bool sample;
    private static byte[] campaignBytes,recordBytes;private static string campaignPath,recordsPath;
    private static byte[] Bytes(string path)=>File.Exists(path)?File.ReadAllBytes(path):null;
    private static bool Same(byte[] a,byte[] b)=>a==null?b==null:b!=null && a.SequenceEqual(b);
    public static string Begin()
    {
        if(!Application.isPlaying || protection!=null)throw new InvalidOperationException("Play required; audit already active");
        save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();map=UnityEngine.Object.FindFirstObjectByType<MapManager>();clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        campaignPath=save.SavePath;recordsPath=save.RecordsPath;campaignBytes=Bytes(campaignPath);recordBytes=Bytes(recordsPath);protection=save.SuppressSaving();clock.AutomaticTick=false;
        seed=map.SeedOverride;blocks=map.BlocksOverride;sample=map.UseSampleForValidation;
        csv=new StringBuilder("group,map_seed,blocks,mean_knowledge,main_traversals,backstreet_traversals,"+SimulationDayRecord.Header+"\n");run=0;
        UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>().Dispatched+=Track;
        StartRun();EditorApplication.update+=Tick;return Status;
    }
    private static void Track(DeliveryTrip trip)
    {
        foreach(var id in trip.EdgeIds)if(map.GetEdge(id).RoadType==RoadType.Main)mainRoads++;else backRoads++;
    }
    private static void StartRun()
    {
        mainRoads=backRoads=0;
        bool safety=run<100;int stage=safety?run%4:Mathf.Min(3,run-100);
        int count=map.CitySettings.stageBlocks[stage];
        string group=safety?"safety":"stage"+(stage+1);
        // Safety is a solvency-independent 3-day completion test. Balance uses normal configured starting cash.
        session=new DaySimulationSession(settings,batchOverride:true,hireOverride:false,adjustOverride:false,name:group,daysOverride:safety?3:30,promisesOverride:new[]{60f},trainOverride:false,initialCashOverride:safety?10000f:(float?)null,facilityOverride:!safety,citySeedOverride:safety?run:1401,cityBlocksOverride:count);
        session.DayCompleted+=Record;Status=$"Running {run+1}/104 ({group})";
    }
    private static void Record(SimulationDayRecord r)
    {
        float knowledge=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>().People.Average(p=>p.LocalKnowledge);
        csv.AppendLine(FormattableString.Invariant($"{(run<100?"safety":"balance")},{map.CityState.seed},{map.City.blocks.Count},{knowledge},{mainRoads},{backRoads},")+r.Csv);mainRoads=backRoads=0;
    }
    private static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Play stopped during simulation");
            var watch=Stopwatch.StartNew();
            do{session.Step();}while(!session.Finished && watch.ElapsedMilliseconds<8);
            if(!session.Finished)return;
            if(run<100 && (session.Records.Count!=3 || session.Records.Any(r=>r.Bankrupt)))throw new Exception($"safety seed {run} did not finish three days");
            session.Dispose();session=null;run++;
            if(run<104){StartRun();return;}
            Directory.CreateDirectory("Docs/MapGeneration");File.WriteAllText("Docs/MapGeneration/Autoplay.csv",csv.ToString());Status="PASS: 100 towns x 3 days completed; four 30-day-limit balance runs recorded (bankruptcy ends a run).";File.WriteAllText("Docs/MapGeneration/Autoplay_Summary.txt",Status+"\nSafety uses 10000 experimental cash to isolate map completion; balance uses normal cash and rules.\n");Cleanup();
        }
        catch(Exception ex){Status="FAIL: "+ex.Message;UnityEngine.Debug.LogError(Status);Cleanup();}
    }
    private static void Cleanup()
    {
        EditorApplication.update-=Tick;UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>().Dispatched-=Track;
        try
        {
            session?.Dispose();session=null;map.SeedOverride=seed;map.BlocksOverride=blocks;map.UseSampleForValidation=sample;
            if(Application.isPlaying){save.StartNewGame();clock.AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>().ReturnTitle();}
            if(!Same(campaignBytes,Bytes(campaignPath)) || !Same(recordBytes,Bytes(recordsPath)))Status="FAIL: normal save changed";
        }
        finally{protection?.Dispose();protection=null;}
    }
}
