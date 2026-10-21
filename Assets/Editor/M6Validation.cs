using System;
using UnityEditor;
using UnityEngine;
public static partial class M6Validation
{
    public static int Checks { get; private set; }
    public static void Check(bool ok,string name) { if(!ok)throw new Exception("M6: "+name);Checks++; }
    public static void Near(float actual,float expected,string name) => Check(Mathf.Abs(actual-expected)<.001f,name+" actual "+actual+" expected "+expected);
    [MenuItem("Tools/Pizza Delivery/Validate M6 (Play Mode)")]
    public static void Menu() => Debug.Log(Run());
    private static void ValidateTraining()
    {
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();spawner.enabled=false;
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();var training=UnityEngine.Object.FindFirstObjectByType<TrainingManager>();var hiring=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();
        var aoi=roster.People[0];Check(training.Cost(aoi,CourierAbility.Driving)==270 && training.Cost(aoi,CourierAbility.Capacity)==400,"training costs");
        Check(!training.Request(aoi,CourierAbility.Capacity) && training.LastMessage.Contains("potential"),"capped ability rejected");
        var funding=new SerializedObject(economy);funding.FindProperty("initialCash").floatValue=0;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();
        Check(!training.Request(aoi,CourierAbility.Driving) && training.LastMessage.Contains("cash"),"insufficient training cash rejected");
        funding.FindProperty("initialCash").floatValue=5000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();funding.FindProperty("initialCash").floatValue=300;funding.ApplyModifiedPropertiesWithoutUndo();
        decimal cash=economy.Cash;Check(training.Request(aoi,CourierAbility.Driving) && economy.Cash==cash-270 && !aoi.IsAvailable,"training debits cash and blocks courier");
        Check(!hiring.Fire(aoi) && hiring.LastMessage.Contains("training"),"training prevents firing");
        Check(!training.Request(aoi,CourierAbility.Service),"second training rejected");
        Check(training.Cancel(aoi) && economy.Cash==cash && aoi.IsAvailable,"cancel fully refunds and restores availability");
        Check(training.Request(aoi,CourierAbility.Driving),"cancel permits replacement request");campaign.StartBusiness();
        Check(!roster.SelectForDispatch(aoi) && roster.SelectForDispatch(roster.People[1]),"other couriers available during training");
        clock.Advance(149.75f);Near(aoi.Driving,.6f,"ability unchanged before half day");Check(!aoi.IsAvailable,"training blocks first half");
        clock.Advance(.25f);Near(aoi.Driving,.7f,"training grants gain at half day");Check(roster.SelectForDispatch(aoi),"trained courier selectable in second half");
        Check(!training.Cancel(aoi) && !training.Request(aoi,CourierAbility.Service),"business cannot cancel or reorder training");
        clock.Advance(1000);Check(economy.Ledger.Wages==404,"training day pays normal opening wage");campaign.NewGame();
        var config=new SerializedObject(training);config.FindProperty("trainingDurationRatio").floatValue=1;config.ApplyModifiedPropertiesWithoutUndo();
        aoi=roster.People[0];Check(training.Request(aoi,CourierAbility.Driving),"full-day training request");campaign.StartBusiness();clock.Advance(299.75f);Check(!aoi.IsAvailable,"ratio one unavailable until closing");
        clock.Advance(.25f);Near(aoi.Driving,.7f,"ratio one completes at closing");Check(clock.Phase==DayPhase.Ended,"full-day training permits settlement");
        config.FindProperty("trainingDurationRatio").floatValue=.5f;config.ApplyModifiedPropertiesWithoutUndo();campaign.NewGame();
    }
    private static void ValidateExperience()
    {
        var rules=UnityEngine.Object.FindFirstObjectByType<CourierProgression>();
        var p=new DeliveryPerson("XP",.5f,.5f,2,.5f,.5f,.5f);p.SetPotential(CourierAbility.Driving,.55f);
        rules.AwardExperience(p,CourierAbility.Driving,420);Near(p.Driving,.54f,"multiple XP thresholds");Near(p.Experience(CourierAbility.Driving),20,"XP remainder");
        rules.AwardExperience(p,CourierAbility.Driving,200);Near(p.Driving,.55f,"growth clamps to potential");Near(p.Experience(CourierAbility.Driving),0,"XP cleared at potential");
        rules.AwardExperience(p,CourierAbility.Driving,1000);Near(p.Experience(CourierAbility.Driving),0,"capped ability cannot accumulate XP");
        rules.AwardExperience(p,CourierAbility.Capacity,10000);Check(p.Capacity==2,"capacity does not gain XP");
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var orders=new[]{new Order("Goal1","XP",0,100)};
        var route=new ManualDeliveryRoute(map);route.TryReplace(new[]{"Store-N1","N1-N2-Backstreet","N2-Goal1","N2-Goal1","N1-N2-Backstreet","Store-N1"});
        var trip=new DeliveryTrip(map,route.BuildCompletion(orders,p),p,0,5);
        Near(rules.ExperienceEarned(trip,CourierAbility.Driving),trip.DistanceAt(trip.ReturnsAt)*.3f,"driving XP distance");
        Near(rules.ExperienceEarned(trip,CourierAbility.LocalKnowledge),30,"knowledge counts repeated backstreets");
        Near(rules.ExperienceEarned(trip,CourierAbility.Service),15,"on-time service XP");
        Near(rules.ExperienceEarned(trip,CourierAbility.Endurance),trip.ReturnsAt*.5f,"endurance outside time");
        Near(rules.ExperienceEarned(trip,CourierAbility.Safety),24,"safe roads XP");
        var dynamics=UnityEngine.Object.FindFirstObjectByType<DeliveryDynamics>();
        var accident=new DeliveryTrip(map,route.BuildCompletion(orders,p),p,0,5,dynamics,()=>0);accident.AdvanceAccidents(10000);
        Near(rules.ExperienceEarned(accident,CourierAbility.Safety),0,"accident roads give no safety XP");
        Near(rules.ExperienceEarned(accident,CourierAbility.Endurance),accident.ReturnsAt*.5f,"accident stop included in endurance XP");
        var lateOrders=new[]{new Order("Goal1","Late",0,0)};var late=new DeliveryTrip(map,route.BuildCompletion(lateOrders,p),p,0,5);Near(rules.ExperienceEarned(late,CourierAbility.Service),5,"late service XP");
        rules.PrepareDay();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();spawner.enabled=false;
        var funding=new SerializedObject(economy);funding.FindProperty("initialCash").floatValue=5000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();funding.FindProperty("initialCash").floatValue=300;funding.ApplyModifiedPropertiesWithoutUndo();
        var person=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>().People[0];campaign.StartBusiness();
        rules.AwardExperience(person,CourierAbility.Driving,200);Near(person.Driving,.62f,"actual roster gains ability during business");
        clock.Advance(clock.BusinessDuration+1);Check(economy.Ledger.Wages==404,"current-day wages are frozen at opening");
        campaign.NextDay();Near(person.Driving,.62f,"growth carries to next day");
        campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1);Check(economy.Ledger.Wages==404.8m,"grown wage applies to next settlement");
        campaign.NewGame();
    }
    private static void ValidatePotentials()
    {
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>().People;
        foreach(var p in roster)for(int i=0;i<5;i++)
        {var a=(CourierAbility)i;Near(p.Potential(a),Mathf.Min(1,p.Ability(a)+.25f),"initial potential bonus");Near(p.Experience(a),0,"initial experience");}
        var fixture=new DeliveryPerson("Clamp",.5f,.5f,2,.5f,.5f,.5f);
        fixture.SetPotential(CourierAbility.Driving,.6f);fixture.Driving=1;Near(fixture.Driving,.6f,"ability setter clamps to potential");
        fixture.SetPotential(CourierAbility.Capacity,1);fixture.Capacity=2;Check(fixture.Capacity==1,"capacity upper bound");
        fixture.SetExperience(CourierAbility.Service,123);var copy=fixture.Clone();Near(copy.Experience(CourierAbility.Service),123,"copy preserves experience");Near(copy.Potential(CourierAbility.Driving),.6f,"copy preserves potential");
        var views=UnityEngine.Object.FindObjectsByType<CourierAbilitiesView>(FindObjectsInactive.Include,FindObjectsSortMode.None);Check(views.Length>=3,"ability bars on business and preparation rows");
    }
    public static string Run()
    {
        Checks=0;string previous=M5Validation.Run()+" / "+M4PlayValidation.CheckReset();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        Check(economy.FixedCost(1)==150 && economy.FixedCost(2)==165 && economy.FixedCost(30)==585,"fixed cost growth");
        var settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        Near(settings.AdjustedPromise(60,0,90),45,"shorter promise with no late and high reputation");
        Near(settings.AdjustedPromise(60,3,50),75,"longer promise with late orders");
        Near(settings.AdjustedPromise(60,1,100),60,"other cases preserve promise");
        var fixture=new GameObject("Busy fixture");fixture.SetActive(false);
        try {var stats=fixture.AddComponent<DayStats>();Near(stats.BusyRatio(2,300),0,"zero busy ratio");}
        finally {UnityEngine.Object.DestroyImmediate(fixture);}
        ValidatePotentials();ValidateExperience();ValidateTraining();
        return previous+$" / PASS: {Checks} M6 checks.";
    }
}
