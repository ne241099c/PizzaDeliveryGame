using System;
using System.Linq;
using UnityEngine;

public static class CityOrderValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
        var save=Find<CampaignSaveManager>();using var scope=save.SuppressSaving();var map=Find<MapManager>();var clock=Find<GameClock>();var spawn=Find<OrderSpawner>();var f=Find<FacilityManager>();var list=Find<OrderBook>();
        int? seed=map.SeedOverride;bool sample=map.UseSampleForValidation;int checks=0;
        void Check(bool ok,string text){if(!ok)throw new Exception(text);checks++;}
        try
        {
            map.UseSampleForValidation=false;map.SeedOverride=1401;save.StartNewGame();clock.AutomaticTick=false;spawn.enabled=true;
            Check(Enumerable.Range(0,100).All(i=>spawn.CreateOrder("B1_H1",0).Kind==PizzaKind.Margherita),"locked recipes never spawn");
            f.Restore(new FacilitySaveData{menu=2});spawn.SeedOverride=72;spawn.ResetRandom();var seafood=map.City.blocks.First(b=>b.template.kind==DistrictKind.Riverside).addresses[0];
            var kinds=Enumerable.Range(0,600).Select(i=>spawn.CreateOrder(seafood,0).Kind).ToArray();
            Check(kinds.Count(k=>k==PizzaKind.Seafood)>kinds.Count(k=>k==PizzaKind.Pepperoni),"river district recipe preference");
            Check(Mathf.Abs(spawn.CityDemandFactor-map.City.blocks.Average(b=>b.template.demand))<.001f,"district demand controls frequency");
            Find<CampaignManager>().StartBusiness();clock.Advance(80);Check(list.Orders.All(o=>map.HasNode(o.DestinationNodeId)),"only open destinations spawn");
            Check(list.Orders.Any(o=>o.DestinationNodeId.StartsWith("B")),"orders reach new districts");
            return $"PASS: {checks} district order checks.";
        }
        finally{map.SeedOverride=seed;map.UseSampleForValidation=sample;spawn.SeedOverride=null;save.StartNewGame();clock.AutomaticTick=false;CampaignSaveManager.SessionSavingLocked=true;}
    }
}
