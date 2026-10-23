using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class PizzaMenuValidation
{
    public static string Run()
    {
        using var sampleMap=new SampleMapValidationScope();
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
        var save=Find<CampaignSaveManager>();using var scope=save.SuppressSaving();var campaign=Find<CampaignManager>();var clock=Find<GameClock>();var crew=Find<KitchenCrew>();var cook=Find<CookingManager>();var list=Find<OrderListUI>();var f=Find<FacilityManager>();var spawner=Find<OrderSpawner>();var score=Find<ScoreManager>();
        int? originalSeed=cook.SeedOverride;int count=0;void Check(bool ok,string text){if(!ok)throw new Exception("Menu: "+text);count++;}
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;
            Check(f.MenuUpgrades==0 && f.IsUnlocked(PizzaKind.Margherita) && !f.IsUnlocked(PizzaKind.Pepperoni),"initial menu");
            var e=Find<BusinessEconomy>();var so=new SerializedObject(e);float initial=so.FindProperty("initialCash").floatValue;so.FindProperty("initialCash").floatValue=10000;so.ApplyModifiedPropertiesWithoutUndo();e.NewGame();so.FindProperty("initialCash").floatValue=initial;so.ApplyModifiedPropertiesWithoutUndo();
            decimal cash=e.Cash;Check(f.Price(FacilityType.Menu)==400 && f.Purchase(FacilityType.Menu) && e.Cash==cash-400 && f.Maintenance==10,"first unlock costs 400, maintenance 10");
            Check(f.Price(FacilityType.Menu)==700 && f.Purchase(FacilityType.Menu) && f.IsUnlocked(PizzaKind.Seafood) && f.Maintenance==20,"second unlock and maintenance");
            Check(f.IsMaximum(FacilityType.Menu) && !f.Purchase(FacilityType.Menu),"no third unlock");
            var kinds=Enumerable.Range(0,60).Select(i=>spawner.CreateOrder("Goal1",i).Kind).Distinct().ToArray();Check(kinds.Length==3,"spawner uses unlocked menu");
            var data=save.Capture();Check(save.TryParse(JsonUtility.ToJson(data),out var parsed,out var reason) && parsed.facilities.menu==2,"menu round trips in save: "+reason);
            campaign.NewGame();campaign.RestorePreparation(data);Check(f.MenuUpgrades==2,"restore menu");
            crew.Restore(Array.Empty<KitchenStaff>());campaign.StartBusiness();
            foreach(PizzaKind kind in Enum.GetValues(typeof(PizzaKind)))
            {
                var o=new Order("Goal1","Recipe",clock.ElapsedSeconds,150,kind:kind);list.AddOrder(o);
                Check(cook.Claim(o,0) && cook.AddDough(0) && cook.AddSauce(0,1) && cook.AddCheese(0),"manual recipe begins "+kind);
                foreach(var topping in kind==PizzaKind.Seafood?o.RequiredToppings.Reverse():o.RequiredToppings)Check(cook.AddTopping(0,topping),"required topping "+topping);
                Check(cook.JobFor(o).ToppingPoint && cook.PutInOven(0,0),"correct ingredients permit bake "+kind);
                clock.Advance(cook.BakeDuration(o));Check(cook.TakeOut(0) && o.IsReady && o.Stars==3,"recipe bakes and auto packs "+kind);
                int points=kind==PizzaKind.Seafood?150:kind==PizzaKind.Pepperoni?120:100;
                Check(score.BasePoints(o)==points && score.ScoreArrival(o,o.Deadline)==points && score.TipForArrival(o,o.Deadline,1)==points/5,"recipe score and tip "+kind);
            }
            cook.SeedOverride=1;campaign.NewGame();f.Restore(new FacilitySaveData{menu=2});campaign.StartBusiness();spawner.enabled=false;
            var seafood=new Order("Goal1","Staff seafood",0,150,kind:PizzaKind.Seafood);list.AddOrder(seafood);var member=crew.People[0];
            clock.Advance(cook.StaffWorkSeconds(member,seafood)+cook.BakeDuration(seafood)+.01f);
            Check(seafood.IsReady && cook.JobFor(seafood).AddedToppings.SequenceEqual(seafood.RequiredToppings),"staff completes both seafood ingredients");
            Check(cook.BakeDuration(seafood)==cook.BakeSeconds+1,"seafood bakes one second longer");
            return $"PASS: {count} pizza menu checks.";
        }
        finally{cook.SeedOverride=originalSeed;save.StartNewGame();clock.AutomaticTick=false;Find<TitleMenuUI>().ReturnTitle();}
    }
}
