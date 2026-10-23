using System;
using System.Linq;
using UnityEngine;
using TMPro;

public static class CityViewValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        using var scope=save.SuppressSaving();
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        int? previous=map.SeedOverride;
        int checks=0;void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
        try
        {
            map.SeedOverride=1401;
            save.StartNewGame();
            clock.AutomaticTick=false;
            UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;
            UnityEngine.Object.FindFirstObjectByType<CampaignManager>().StartBusiness();
            var business=UnityEngine.Object.FindFirstObjectByType<BusinessView>();
            business.ShowZone(ShopZone.Dispatch,true);
            business.FitMap();
            Check(map.RenderChunkCount>1,"map has spatial render chunks");
            Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(t=>t.name=="DistrictRiver"),"river follows generated riverside district");
            Check(UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t=>t.name=="Bridge")>=2,"two bridge decorations");
            Check(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None).Any(t=>t.name.StartsWith("DistrictName_")),"district captions");
            Check(map.TodayCityInfo.Contains("街区3") && map.TomorrowCityInfo(5).Contains("新しい街区"),"current information and next-day preview");
            Check(UiKit.Art("tray_squid")!=null && UiKit.Art("pizza_squid")!=null,"seafood ingredient sprites imported");
            var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();
            var handover=UnityEngine.Object.FindFirstObjectByType<HandoverManager>();
            var person=UnityEngine.Object.FindFirstObjectByType<CourierRoster>().People[0];
            var order=new Order("B1_H1","Cache review",0,120,BakingState.Ready);
            list.AddOrder(order);
            handover.Bag(order,person);
            business.SyncRoute();
            var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlanner>();
            var outward=RoadPathfinder.FindShortestPath(map.Edges,map.StoreNodeId,order.DestinationNodeId,e=>e.Distance);
            var home=RoadPathfinder.FindShortestPath(map.Edges,order.DestinationNodeId,map.StoreNodeId,e=>e.Distance);
            Check(planner.Route.EdgeIds.SequenceEqual(outward.Concat(home)),"cached path matches production path search");
            int revision=map.MapRevision;
            map.LoadMap(map.City.map);
            business.SyncRoute();
            Check(map.MapRevision>revision && planner.Route.EdgeIds.SequenceEqual(outward.Concat(home)),"map reload invalidates path cache");
            return $"PASS: {checks} city view checks.";
        }
        finally{map.SeedOverride=previous;save.StartNewGame();clock.AutomaticTick=false;CampaignSaveManager.SessionSavingLocked=true;}
    }
}
