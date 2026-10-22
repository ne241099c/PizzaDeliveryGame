using System;
using UnityEngine;
using UnityEditor;
public static class M7Validation
{
    // Older route/finance tests start with an explicitly baked pizza; fresh orders use the default Waiting state.
    public static Order ReadyOrder(string destination,string name,float spawned,float promise)=>new Order(destination,name,spawned,promise,BakingState.Ready);
    [MenuItem("Tools/Pizza Delivery/Validate M7 (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("M7: "+name);count++;}
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var cook=UnityEngine.Object.FindFirstObjectByType<CookingManager>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;campaign.StartBusiness();
        var first=new Order("Goal1","First pizza",0,100);var second=new Order("Goal2","Second pizza",0,100);list.AddOrder(first);list.AddOrder(second);
        Check(first.Baking==BakingState.Cooking && second.Baking==BakingState.Waiting,"single oven FIFO queue");
        Check(!list.SelectOnly(first) && !list.SelectOnly(second),"uncooked orders cannot be selected");Check(economy.Ledger.Materials==0,"no materials charged while cooking");
        clock.Advance(12);Check(first.IsReady && second.Baking==BakingState.Cooking && Mathf.Abs(second.CookStartedAt-12)<.001f,"first completion starts second at exact time");
        Check(list.SelectOnly(first),"baked order can be selected");Check(first.Deadline==100,"cooking does not rewrite deadline");
        clock.Advance(12);Check(second.IsReady,"second pizza completes");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();
        var expired=new Order("Goal1","Expires in oven",0,5);var next=new Order("Goal2","Next pizza",0,100);list.AddOrder(expired);list.AddOrder(next);clock.Advance(5);
        Check(expired.Status==OrderStatus.Cancelled && !list.Contains(expired),"cooking order expires at deadline");
        Check(next.Baking==BakingState.Cooking && Mathf.Abs(next.CookStartedAt-5)<.001f,"expired pizza frees oven at expiry");clock.Advance(12);Check(next.IsReady,"replacement cooking completes");
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();var closing=new Order("Goal1","Closing cooking",299,100);clock.Advance(299);list.AddOrder(closing);clock.Advance(1);
        Check(closing.Status==OrderStatus.Cancelled && list.OrderCount==0,"closing cancels cooking and queue");
        Check(Mathf.Abs(spawner.MinimumPromiseSeconds-(spawner.ShortestRoundTripSeconds("Goal2")*1.1f+cook.CookSeconds))<.001f,"round trip and cooking minimum");
        spawner.SetPromiseSeconds(30);Check(spawner.PromiseSeconds>=spawner.MinimumPromiseSeconds,"requested 30 respects physical minimum");campaign.NewGame();
        return $"PASS: {count} M7 checks.";
    }
}
