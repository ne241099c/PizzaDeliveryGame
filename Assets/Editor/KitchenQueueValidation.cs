using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class KitchenQueueValidation
{
    public static string Run()
    {
        using var sampleMap=new SampleMapValidationScope();
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();var crew=UnityEngine.Object.FindFirstObjectByType<KitchenCrew>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception(name);count++;}
        try
        {
            campaign.NewGame();crew.Restore(Array.Empty<KitchenStaff>());clock.AutomaticTick=false;spawner.enabled=false;campaign.StartBusiness();
            var pizzas=Enumerable.Range(0,3).Select(i=>new Order("Goal1","Queue "+i,0,150)).ToArray();
            foreach(var order in pizzas){list.AddOrder(order);Check(cook.Claim(order,0) && cook.AddDough(0) && cook.AddSauce(0,1) && cook.AddCheese(0) && cook.AddTopping(0,Topping.Basil) && cook.PutInOven(0,0),"pizza accepted while oven is occupied");}
            Check(cook.OvenJob(0).Order==pizzas[0] && cook.OvenQueue.Select(j=>j.Order).SequenceEqual(pizzas.Skip(1)),"shared queue is FIFO");
            Check(cook.BenchJob(0)==null,"queued pizza frees workbench");
            clock.Advance(cook.BakeSeconds);cook.TakeOut(0);
            Check(pizzas[0].IsReady && cook.OvenJob(0).Order==pizzas[1],"taking out first starts second immediately");
            list.RemoveOrders(new[]{pizzas[2]});Check(!cook.OvenQueue.Any(),"removed order leaves oven queue");
            var field=new SerializedObject(map);float rate=field.FindProperty("deliverySpeedMultiplier").floatValue;float faster=map.GetTravelTime("Store-N1",1);
            field.FindProperty("deliverySpeedMultiplier").floatValue=1;field.ApplyModifiedPropertiesWithoutUndo();float original=map.GetTravelTime("Store-N1",1);
            field.FindProperty("deliverySpeedMultiplier").floatValue=rate;field.ApplyModifiedPropertiesWithoutUndo();
            Check(Mathf.Abs(faster*rate-original)<.0001f && rate>1,"delivery multiplier reduces every travel time consistently");
            return $"PASS: {count} kitchen queue and delivery speed checks.";
        }
        finally{save.StartNewGame();clock.AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>().ReturnTitle();}
    }
}
