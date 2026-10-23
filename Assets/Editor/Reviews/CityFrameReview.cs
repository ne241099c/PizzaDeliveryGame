using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Native frame measurements while normal saving stays suppressed across all review frames.</summary>
public static class CityFrameReview
{
    private static readonly List<double> frames=new();private static IDisposable protection;private static MapManager map;private static int? previousSeed;private static string phase;
    public static string Status {get;private set;}="Not run";
    private static byte[] campaign,records;private static string campaignPath,recordPath;
    public static string Begin()
    {
        if(!Application.isPlaying || protection!=null)throw new InvalidOperationException("Play required");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();campaignPath=save.SavePath;recordPath=save.RecordsPath;campaign=Bytes(campaignPath);records=Bytes(recordPath);protection=save.SuppressSaving();
        map=UnityEngine.Object.FindFirstObjectByType<MapManager>();previousSeed=map.SeedOverride;map.SeedOverride=1401;save.StartNewGame();UnityEngine.Object.FindFirstObjectByType<GameClock>().AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;UnityEngine.Object.FindFirstObjectByType<CampaignManager>().StartBusiness();UnityEngine.Object.FindFirstObjectByType<BusinessView>().ShowZone(ShopZone.Dispatch,true);
        var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();var person=UnityEngine.Object.FindFirstObjectByType<CourierRoster>().People[0];foreach(var destination in new[]{"Goal1","Goal2"}){var order=new Order(destination,"Review",0,120,BakingState.Ready);list.AddOrder(order);handover.Bag(order,person);}
        map.StartCoroutine(Measure());return Status="Measuring normal town";
    }
    private static byte[] Bytes(string path)=>File.Exists(path)?File.ReadAllBytes(path):null;
    private static bool Same(byte[] a,byte[] b)=>a==null?b==null:b!=null && a.SequenceEqual(b);
    private static IEnumerator Sample()
    {
        frames.Clear();for(int i=0;i<15;i++)yield return null;
        double last=Time.realtimeSinceStartupAsDouble;
        for(int i=0;i<90;i++){yield return null;double now=Time.realtimeSinceStartupAsDouble;frames.Add((now-last)*1000);last=now;}
    }
    private static string Line(string name,int edges){var sorted=frames.OrderBy(f=>f).ToArray();return FormattableString.Invariant($"{name},{edges},{sorted[sorted.Length/2]:F3},{sorted[(int)(sorted.Length*.95)]:F3},{1000/frames.Average():F2}");}
    private static IEnumerator Measure()
    {
        Directory.CreateDirectory("Docs/Simulations/MapGeneration");Directory.CreateDirectory("Docs/UI_Screens/generated-city");var csv="view,edges,median_ms,p95_ms,mean_fps\n";
        yield return Sample();int normalEdges=map.Edges.Count;csv+=Line("normal",normalEdges)+"\n";ReviewFrameCapture.Begin("Docs/UI_Screens/generated-city/normal.png");yield return new WaitForSecondsRealtime(.25f);
        int blocks=map.City.blocks.Count*10;var big=CityGenerator.Build(1401,blocks,map.CitySettings);while(big.map.Edges.Count<normalEdges*10){UnityEngine.Object.Destroy(big.map);blocks++;big=CityGenerator.Build(1401,blocks,map.CitySettings);}map.LoadMap(big.map);UnityEngine.Object.FindFirstObjectByType<BusinessView>().FitMap();Status="Measuring 10x town";
        yield return Sample();csv+=Line("ten-times",map.Edges.Count)+"\n";ReviewFrameCapture.Begin("Docs/UI_Screens/generated-city/ten-times.png");yield return new WaitForSecondsRealtime(.25f);File.WriteAllText("Docs/Simulations/MapGeneration/FrameTimes.csv",csv);UnityEngine.Object.Destroy(big.map);Status="PASS: normal and 10x frame measurements written; protected visual review remains active.";
    }
    public static string End()
    {
        if(protection==null)return "Review not active";
        try{map.StopAllCoroutines();map.SeedOverride=previousSeed;var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();save.StartNewGame();UnityEngine.Object.FindFirstObjectByType<GameClock>().AutomaticTick=false;CampaignSaveManager.SessionSavingLocked=true;if(!Same(campaign,Bytes(campaignPath)) || !Same(records,Bytes(recordPath)))throw new Exception("normal save changed");return "PASS: normal campaign and records unchanged";}
        finally{protection.Dispose();protection=null;}
    }
}
