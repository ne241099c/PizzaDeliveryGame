using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>Time-sliced seed audit. Suppression covers every frame through cleanup and normal files are compared.</summary>
public static class CitySeedValidation
{
    public static string Status {get;private set;}="Not run";
    public static bool Running {get;private set;}
    private static IDisposable protection;
    private static CampaignSaveManager save;private static GameClock clock;
    private static byte[] campaign,records;private static string campaignPath,recordsPath;
    private static int seed,fallback,retried;private static StringBuilder csv;
    public static string Begin()
    {
        if(!Application.isPlaying || Running)throw new InvalidOperationException("Play required; only one audit at a time.");
        save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        campaignPath=save.SavePath;recordsPath=save.RecordsPath;campaign=Bytes(campaignPath);records=Bytes(recordsPath);protection=save.SuppressSaving();clock.AutomaticTick=false;
        csv=new StringBuilder("seed,blocks,attempt,fallback,nodes,edges,destinations,total_distance,dead_end_fraction,shortcut_destinations,min_saving,max_saving,near,middle,far,near_pairs,bridges,one_way,min_spacing\n");seed=fallback=retried=0;Running=true;Status="Running 0/1000";EditorApplication.update+=Tick;return Status;
    }
    private static byte[] Bytes(string path)=>File.Exists(path)?File.ReadAllBytes(path):null;
    private static bool Same(byte[] a,byte[] b)=>a==null?b==null:b!=null && System.Linq.Enumerable.SequenceEqual(a,b);
    private static void Tick()
    {
        try
        {
            if(!Application.isPlaying)throw new Exception("Play stopped during city audit");
            var watch=Stopwatch.StartNew();var s=new CityGenerationSettings();
            do
            {
                // The full range of complexity, not only the small initial town.
                int blocks=s.stageBlocks[seed%s.stageBlocks.Length];var city=CityGenerator.Generate(seed,blocks,s);
                try
                {
                    var m=city.metrics;if(m.failures.Count>0)throw new Exception($"seed {seed}: "+string.Join("/",m.failures));
                    if(city.fallback)fallback++;if(city.attempt>0)retried++;
                    csv.AppendLine(FormattableString.Invariant($"{seed},{blocks},{city.attempt},{city.fallback},{m.nodes},{m.edges},{m.destinations},{m.totalDistance},{m.deadEndFraction},{m.shortcutDestinations},{m.minimumSaving},{m.maximumSaving},{m.near},{m.middle},{m.far},{m.nearPairs},{m.bridges},{m.oneWay},{m.minimumSpacing}"));
                }
                finally{UnityEngine.Object.Destroy(city.map);}
                seed++;
            }while(seed<1000 && watch.ElapsedMilliseconds<8);
            Status=$"Running {seed}/1000";
            if(seed<1000)return;
            s.retryCount=0;var forced=CityGenerator.Generate(72,3,s);if(!forced.fallback || forced.metrics.failures.Count>0)throw new Exception("forced fallback did not produce a safe city");UnityEngine.Object.Destroy(forced.map);
            Directory.CreateDirectory("Docs/MapGeneration");File.WriteAllText("Docs/MapGeneration/Seeds.csv",csv.ToString());
            Status=$"PASS: 1000 seeds, retries {retried}, fallback {fallback}; forced fallback PASS.";File.WriteAllText("Docs/MapGeneration/Seeds_Summary.txt",Status+"\nColumns include scale, shortcut value, distance distribution, pairs and bridges.\n");Cleanup();
        }
        catch(Exception ex){Status="FAIL: "+ex.Message;UnityEngine.Debug.LogError(Status);Cleanup();}
    }
    private static void Cleanup()
    {
        EditorApplication.update-=Tick;Running=false;
        try
        {
            if(Application.isPlaying){save.StartNewGame();clock.AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<GameMenu>().ReturnTitle();}
            if(!Same(campaign,Bytes(campaignPath)) || !Same(records,Bytes(recordsPath)))Status="FAIL: normal save changed during city audit";
        }
        finally{protection?.Dispose();protection=null;}
    }
}
