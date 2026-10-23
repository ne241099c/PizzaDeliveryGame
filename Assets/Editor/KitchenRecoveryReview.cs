using System;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Short native UI review with saving suppressed across every captured frame.</summary>
public static class KitchenRecoveryReview
{
    private static IDisposable protection;
    private static byte[] campaignBytes,recordBytes;
    private static int? seed;
    private static bool sample;
    private static byte[] Read(string path)=>File.Exists(path)?File.ReadAllBytes(path):null;
    private static bool Same(byte[] a,byte[] b)=>a==null?b==null:b!=null && a.SequenceEqual(b);
    public static string Begin()
    {
        if(!Application.isPlaying || protection!=null)throw new InvalidOperationException("Play required, no review already active");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();campaignBytes=Read(save.SavePath);recordBytes=Read(save.RecordsPath);protection=save.SuppressSaving();
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();seed=map.SeedOverride;sample=map.UseSampleForValidation;map.SeedOverride=1401;map.UseSampleForValidation=false;
        save.StartNewGame();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();clock.AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;
        UnityEngine.Object.FindFirstObjectByType<KitchenCrew>().Restore(Array.Empty<KitchenStaff>());UnityEngine.Object.FindFirstObjectByType<CampaignManager>().StartBusiness();
        var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var list=UnityEngine.Object.FindFirstObjectByType<OrderBook>();
        var burnt=new Order("B0_H1","Review burnt",0,150);var wrong=new Order("B1_H1","Review wrong",0,150,kind:PizzaKind.Pepperoni);list.AddOrder(burnt);list.AddOrder(wrong);
        foreach(var o in new[]{burnt,wrong}){cook.Claim(o,0);cook.AddDough(0);cook.AddSauce(0,1);cook.AddCheese(0);cook.AddTopping(0,Topping.Basil);cook.PutInOven(0,0);clock.Advance(o==burnt?cook.BurnAt+.1f:cook.BakeSeconds);cook.TakeOut(0);}
        UnityEngine.Object.FindFirstObjectByType<BusinessView>().ShowZone(ShopZone.Kitchen,true);
        return "Review active, saving suppressed";
    }
    public static string ValidateAndDrop()
    {
        if(protection==null)throw new InvalidOperationException("Begin review first");
        var business=UnityEngine.Object.FindFirstObjectByType<BusinessView>();var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();
        var reviews=business.GetComponentsInChildren<RectTransform>(true).Where(r=>r.name=="Failure").ToArray();
        if(reviews.Length!=2 || reviews.Any(r=>r.Find("Serve")==null || r.Find("Redo")==null))throw new Exception("two failure choices must be visible");
        var job=cook.Jobs.First(j=>j.Burnt);var zone=business.GetComponentsInChildren<DropZone>(true).First(z=>z.name=="Trash");var hand=UnityEngine.Object.FindFirstObjectByType<ShopHand>();
        var source=reviews[0].parent.Find("Pizza").GetComponent<DragSource>();if(source.Payload()==null)throw new Exception("failed pizza must be draggable");
        var payload=new DragPayload{Kind=DragKind.Pizza,Job=job,Order=job.Order,Sprite="pizza_burnt"};
        Vector2 at=RectTransformUtility.WorldToScreenPoint(null,zone.transform.TransformPoint(((RectTransform)zone.transform).rect.center));
        hand.BeginAt(payload,at);hand.MoveAt(at);if(!zone.Glow.enabled)throw new Exception("trash does not highlight");hand.EndAt(at);
        if(!cook.IsRedo(job.Order) || cook.JobFor(job.Order)?.Stage!=PizzaStage.Dough)throw new Exception("actual UI drop did not remake pizza");
        return "PASS: 5 native UI checks (choices, buttons, drag source, glow, raycast drop)";
    }
    public static string End()
    {
        if(protection==null)return "Review inactive";
        try
        {
            var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();map.SeedOverride=seed;map.UseSampleForValidation=sample;
            var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();save.StartNewGame();UnityEngine.Object.FindFirstObjectByType<GameClock>().AutomaticTick=false;UnityEngine.Object.FindFirstObjectByType<GameMenu>().ReturnTitle();
            if(!Same(campaignBytes,Read(save.SavePath)) || !Same(recordBytes,Read(save.RecordsPath)))throw new Exception("Normal campaign or records changed");
            return "PASS: normal save and records unchanged";
        }
        finally{protection.Dispose();protection=null;}
    }
}
