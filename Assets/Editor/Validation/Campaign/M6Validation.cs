using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static partial class M6Validation
{
    public static int Checks { get; private set; }
    public static void Check(bool ok,string name) { if(!ok)throw new Exception("M6: "+name);Checks++; }
    public static void Near(float actual,float expected,string name) => Check(Mathf.Abs(actual-expected)<.001f,name+" actual "+actual+" expected "+expected);
    [MenuItem("Tools/Pizza Delivery/Validate M6 (Play Mode)")]
    public static void Menu() => Debug.Log(Run());
    private static void ValidatePreparationButtons()
    {
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var training=UnityEngine.Object.FindFirstObjectByType<TrainingManager>();var hub=UnityEngine.Object.FindFirstObjectByType<HubView>();
        var recruitment=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();
        // The hub's cards are rebuilt from the current roster, so each button acts on the person shown.
        StickerButton Button(string card,string name)
        {
            hub.RefreshNow();
            var root=hub.Content.GetComponentsInChildren<RectTransform>(true).First(r=>r.name==card);
            return root.GetComponentsInChildren<StickerButton>(true).First(b=>b.name==name);
        }
        campaign.StartBusiness();campaign.NewGame();hub.ShowTab(0);var person=roster.People[0];
        Button("Courier_"+person.Name,"Train").Button.onClick.Invoke();
        hub.GetComponentsInChildren<StickerButton>(true).First(b=>b.name=="Train0").Button.onClick.Invoke();
        Check(training.HasAssignment(person) && economy.Cash==130,"hub training dialog trains the person on the card");
        Button("Courier_"+person.Name,"CancelTraining").Button.onClick.Invoke();
        Check(economy.Cash==300 && person.IsAvailable,"hub cancel button refunds");
        Button("Courier_"+person.Name,"Fire").Button.onClick.Invoke();
        Check(roster.People.Count==1 && !roster.People.Contains(person),"hub fire button targets the person on the card");
        campaign.NewGame();
        var funding=new SerializedObject(economy);
        funding.FindProperty("initialCash").floatValue=5000;
        funding.ApplyModifiedPropertiesWithoutUndo();
        economy.NewGame();
        funding.FindProperty("initialCash").floatValue=300;
        funding.ApplyModifiedPropertiesWithoutUndo();
        hub.ShowTab(1);var applicant=recruitment.Applicants[0].Person;Button("Applicant_"+applicant.Name,"Hire").Button.onClick.Invoke();
        Check(roster.People.Count==3 && roster.People.Contains(applicant),"hub hire button hires the applicant on the card");
        hub.ShowTab(0);
        campaign.NewGame();
        Check(roster.People.Count==2 && clock.Phase==DayPhase.BeforeOpening,"button integration returns clean campaign");
    }
    private static void ValidateRecruitment()
    {
        var generator=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();generator.SeedOverride=123;generator.ResetRandom();
        Check(generator.TypeForDraw(0)==RecruitArchetype.Newcomer && generator.TypeForDraw(.299)==RecruitArchetype.Newcomer && generator.TypeForDraw(.301)==RecruitArchetype.Experienced && generator.TypeForDraw(.601)==RecruitArchetype.Standard,"type probability thresholds 30/30/40");
        var profiles=Enumerable.Range(0,2000).Select(i=>generator.GenerateApplicant("Profile"+i)).ToArray();
        foreach(var type in (RecruitArchetype[])Enum.GetValues(typeof(RecruitArchetype)))
        {
            var people=profiles.Where(a=>a.Type==type).ToArray();float expected=type==RecruitArchetype.Standard?.4f:.3f;
            Check(Mathf.Abs(people.Length/2000f-expected)<.05f,"fixed sample type frequency "+type);
            float low=type==RecruitArchetype.Newcomer?.2f:type==RecruitArchetype.Experienced?.5f:.3f;
            float high=type==RecruitArchetype.Newcomer?.5f:type==RecruitArchetype.Experienced?.8f:.7f;
            Check(people.All(a=>a.Baselines.All(v=>v>=low && v<=high)),"type baseline range "+type);
            float min=type==RecruitArchetype.Newcomer?.3f:type==RecruitArchetype.Experienced?.05f:.15f;
            float max=type==RecruitArchetype.Newcomer?.5f:type==RecruitArchetype.Experienced?.15f:.3f;
            Check(people.All(a=>Enumerable.Range(0,5).All(i=>
            {
                float ability=a.Person.Ability((CourierAbility)i),potential=a.Person.Potential((CourierAbility)i);
                return potential>=ability && potential<=1 && potential>=Mathf.Min(1,ability+min)-.0001f && potential<=Mathf.Min(1,ability+max)+.0001f;
            })),"type potential range "+type);
        }
        var newcomer=profiles.First(a=>a.Type==RecruitArchetype.Newcomer).Person;
        Check(newcomer.Capacity==1 && generator.HiringCost(newcomer)>UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>().DailyWage(newcomer)*3,"newcomer has training room premium");
        Check(profiles.Where(a=>a.Type==RecruitArchetype.Newcomer).All(a=>a.Person.Capacity==1),"newcomers begin with capacity one");
        var copy=newcomer.Clone();
        Check(copy.Archetype==newcomer.Archetype && Enumerable.Range(0,6).All(i=>copy.Potential((CourierAbility)i)==newcomer.Potential((CourierAbility)i)),"hired profile copy preserves type and ceilings");
        UnityEngine.Object.FindFirstObjectByType<CampaignManager>().NewGame();
    }
    private static void ValidateTraining()
    {
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        spawner.enabled=false;
        var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>();
        var training=UnityEngine.Object.FindFirstObjectByType<TrainingManager>();
        var hiring=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();
        var aoi=roster.People[0];
        Check(training.Cost(aoi,CourierAbility.Driving)==170 && training.Cost(aoi,CourierAbility.Capacity)==300,"training costs");
        Check(!training.Request(aoi,CourierAbility.Capacity) && training.LastMessage.Contains("potential"),"capped ability rejected");
        var funding=new SerializedObject(economy);
        funding.FindProperty("initialCash").floatValue=0;
        funding.ApplyModifiedPropertiesWithoutUndo();
        economy.NewGame();
        Check(!training.Request(aoi,CourierAbility.Driving) && training.LastMessage.Contains("cash"),"insufficient training cash rejected");
        funding.FindProperty("initialCash").floatValue=5000;
        funding.ApplyModifiedPropertiesWithoutUndo();
        economy.NewGame();
        funding.FindProperty("initialCash").floatValue=300;
        funding.ApplyModifiedPropertiesWithoutUndo();
        decimal cash=economy.Cash;
        Check(training.Request(aoi,CourierAbility.Driving) && economy.Cash==cash-170 && !aoi.IsAvailable,"training debits cash and blocks courier");
        Check(!hiring.Fire(aoi) && hiring.LastMessage.Contains("training"),"training prevents firing");
        Check(!training.Request(aoi,CourierAbility.Service),"second training rejected");
        Check(training.Cancel(aoi) && economy.Cash==cash && aoi.IsAvailable,"cancel fully refunds and restores availability");
        Check(training.Request(aoi,CourierAbility.Driving),"cancel permits replacement request");campaign.StartBusiness();
        Check(!roster.SelectForDispatch(aoi) && roster.SelectForDispatch(roster.People[1]),"other couriers available during training");
        clock.Advance(clock.BusinessDuration*.5f-.25f);
        Near(aoi.Driving,.6f,"ability unchanged before half day");
        Check(!aoi.IsAvailable,"training blocks first half");
        clock.Advance(.25f);
        Near(aoi.Driving,.7f,"training grants gain at half day");
        Check(roster.SelectForDispatch(aoi),"trained courier selectable in second half");
        Check(!training.Cancel(aoi) && !training.Request(aoi,CourierAbility.Service),"business cannot cancel or reorder training");
        clock.Advance(1000);
        Check(economy.Ledger.Wages==260.4m+economy.KitchenWages,"training day pays normal opening courier wage plus the apprentice");
        campaign.NewGame();
        var config=new SerializedObject(training);
        config.FindProperty("trainingDurationRatio").floatValue=1;
        config.ApplyModifiedPropertiesWithoutUndo();
        aoi=roster.People[0];
        Check(training.Request(aoi,CourierAbility.Driving),"full-day training request");
        campaign.StartBusiness();
        clock.Advance(clock.BusinessDuration-.25f);
        Check(!aoi.IsAvailable,"ratio one unavailable until closing");
        clock.Advance(.25f);
        Near(aoi.Driving,.7f,"ratio one completes at closing");
        Check(clock.Phase==DayPhase.Ended,"full-day training permits settlement");
        config.FindProperty("trainingDurationRatio").floatValue=.5f;config.ApplyModifiedPropertiesWithoutUndo();campaign.NewGame();
    }
    private static void ValidateExperience()
    {
        var rules=UnityEngine.Object.FindFirstObjectByType<CourierProgression>();
        var p=new DeliveryPerson("XP",.5f,.5f,2,.5f,.5f,.5f);p.SetPotential(CourierAbility.Driving,.55f);
        rules.AwardExperience(p,CourierAbility.Driving,420);
        Near(p.Driving,.54f,"multiple XP thresholds");
        Near(p.Experience(CourierAbility.Driving),20,"XP remainder");
        rules.AwardExperience(p,CourierAbility.Driving,200);
        Near(p.Driving,.55f,"growth clamps to potential");
        Near(p.Experience(CourierAbility.Driving),0,"XP cleared at potential");
        rules.AwardExperience(p,CourierAbility.Driving,1000);Near(p.Experience(CourierAbility.Driving),0,"capped ability cannot accumulate XP");
        rules.AwardExperience(p,CourierAbility.Capacity,10000);Check(p.Capacity==2,"capacity does not gain XP");
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();var orders=new[]{M7Validation.ReadyOrder("Goal1","XP",0,100)};
        var route=new ManualDeliveryRoute(map);
        route.TryReplace(new[]{"Store-N1","N1-N2-Backstreet","N2-Goal1","N2-Goal1","N1-N2-Backstreet","Store-N1"});
        var trip=new DeliveryTrip(map,route.BuildCompletion(orders,p),p,0,5);
        Near(rules.ExperienceEarned(trip,CourierAbility.Driving),trip.DistanceAt(trip.ReturnsAt)*.3f*rules.EarnedExperienceMultiplier,"driving XP distance");
        Near(rules.ExperienceEarned(trip,CourierAbility.LocalKnowledge),30*rules.EarnedExperienceMultiplier,"knowledge counts repeated backstreets");
        Near(rules.ExperienceEarned(trip,CourierAbility.Service),15*rules.EarnedExperienceMultiplier,"on-time service XP");
        Near(rules.ExperienceEarned(trip,CourierAbility.Endurance),trip.ReturnsAt*.35f*rules.EarnedExperienceMultiplier,"endurance outside time");
        Near(rules.ExperienceEarned(trip,CourierAbility.Safety),24*rules.EarnedExperienceMultiplier,"safe roads XP");
        var dynamics=UnityEngine.Object.FindFirstObjectByType<DeliveryDynamics>();
        var accident=new DeliveryTrip(map,route.BuildCompletion(orders,p),p,0,5,dynamics,()=>0);accident.AdvanceAccidents(10000);
        Near(rules.ExperienceEarned(accident,CourierAbility.Safety),0,"accident roads give no safety XP");
        Near(rules.ExperienceEarned(accident,CourierAbility.Endurance),accident.ReturnsAt*.35f*rules.EarnedExperienceMultiplier,"accident stop included in endurance XP");
        var lateOrders=new[]{M7Validation.ReadyOrder("Goal1","Late",0,0)};
        var late=new DeliveryTrip(map,route.BuildCompletion(lateOrders,p),p,0,5);
        Near(rules.ExperienceEarned(late,CourierAbility.Service),5*rules.EarnedExperienceMultiplier,"late service XP");
        rules.PrepareDay();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        spawner.enabled=false;
        var funding=new SerializedObject(economy);
        funding.FindProperty("initialCash").floatValue=5000;
        funding.ApplyModifiedPropertiesWithoutUndo();
        economy.NewGame();
        funding.FindProperty("initialCash").floatValue=300;
        funding.ApplyModifiedPropertiesWithoutUndo();
        var person=UnityEngine.Object.FindFirstObjectByType<CourierRoster>().People[0];campaign.StartBusiness();
        rules.AwardExperience(person,CourierAbility.Driving,200);Near(person.Driving,.62f,"actual roster gains ability during business");
        clock.Advance(clock.BusinessDuration+1);
        Check(economy.Ledger.Wages==260.4m+economy.KitchenWages && economy.KitchenWages==48,"current-day courier (260.4) and apprentice (48) wages are frozen at opening");
        campaign.NextDay();Near(person.Driving,.62f,"growth carries to next day");
        campaign.StartBusiness();
        clock.Advance(clock.BusinessDuration+1);
        Check(economy.Ledger.Wages==260.88m+economy.KitchenWages,"grown courier wage (260.88) applies to next settlement with the apprentice wage");
        campaign.NewGame();
    }
    private static void ValidatePotentials()
    {
        var roster=UnityEngine.Object.FindFirstObjectByType<CourierRoster>().People;
        foreach(var p in roster)for(int i=0;i<5;i++)
        {
            var a=(CourierAbility)i;
            Near(p.Potential(a),Mathf.Min(1,p.Ability(a)+.25f),"initial potential bonus");
            Near(p.Experience(a),0,"initial experience");
        }
        var fixture=new DeliveryPerson("Clamp",.5f,.5f,2,.5f,.5f,.5f);
        fixture.SetPotential(CourierAbility.Driving,.6f);fixture.Driving=1;Near(fixture.Driving,.6f,"ability setter clamps to potential");
        fixture.SetPotential(CourierAbility.Capacity,1);fixture.Capacity=2;Check(fixture.Capacity==1,"capacity upper bound");
        fixture.SetExperience(CourierAbility.Service,123);
        var copy=fixture.Clone();
        Near(copy.Experience(CourierAbility.Service),123,"copy preserves experience");
        Near(copy.Potential(CourierAbility.Driving),.6f,"copy preserves potential");
        var hub=UnityEngine.Object.FindFirstObjectByType<HubView>();hub.ShowTab(0);hub.RefreshNow();
        var card=hub.Content.GetComponentsInChildren<RectTransform>(true).First(r=>r.name.StartsWith("Courier_"));
        Check(Enumerable.Range(0,5).All(i=>card.Find("AbilityBar"+i)!=null && card.Find("AbilityBar"+i).GetComponent<UiBar>()!=null),"ability bars with a potential marker on courier cards");
    }
    private static void ValidateAdjustment(DaySimulationSettings settings)
    {
        Check(settings.NextTrendStreak(-1,1,0,95)==2,"same trend accumulates");
        Check(settings.NextTrendStreak(-1,2,3,50)==1,"opposite trend resets streak");
        Check(settings.NextTrendStreak(-1,2,1,95)==0,"neutral day clears streak");
        Check(settings.NextTrendStreak(0,0,0,95)==1,"success after neutral begins new streak");
        Near(settings.AdjustedPromise(50,0,95,3),40,"third consecutive day uses rolling two-day window");
        var trainee=new DeliveryPerson("Priority",.5f,.3f,1,.4f,.6f,.4f);
        Check(settings.PreferredTrainingAbility(trainee)==CourierAbility.Driving,"train driving before lowest ability");
        trainee.SetPotential(CourierAbility.Driving,trainee.Driving);
        Check(settings.PreferredTrainingAbility(trainee)==CourierAbility.Capacity,"capacity one after capped driving");
        trainee.Capacity=2;
        Check(settings.PreferredTrainingAbility(trainee)==CourierAbility.Capacity,"capacity two can train a third box");
        trainee.Capacity=3;
        Check(settings.PreferredTrainingAbility(trainee)==CourierAbility.Safety,"skip capped capacity three");
        trainee.SetPotential(CourierAbility.Safety,trainee.Safety);
        Check(settings.PreferredTrainingAbility(trainee)==CourierAbility.Service,"service follows safety");
        trainee.SetPotential(CourierAbility.Service,trainee.Service);
        Check(!settings.PreferredTrainingAbility(trainee).HasValue,"do not train knowledge or endurance as fallback");
        trainee.Capacity=1;trainee.SetPotential(CourierAbility.Capacity,1);
        Check(!settings.PreferredTrainingAbility(trainee).HasValue,"capped capacity one skipped");
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        Near(map.GetEdge("N1-N2-Backstreet").Distance,11,"adjusted backstreet distance");
        foreach(float knowledge in new[]{.3f,.6f,.9f})
        {
            var p=new DeliveryPerson("Knowledge",.5f,knowledge,2,.5f,.5f,.5f);
            foreach(string goal in new[]{"Goal1","Goal2"})
            {
                var route=RoadPathfinder.Suggest(map,new[]{M7Validation.ReadyOrder(goal,"Knowledge",0,60)},p);
                bool usesBackstreet=route.EdgeIds.Any(id=>map.GetEdge(id).RoadType==RoadType.Backstreet);
                Check(usesBackstreet==(knowledge>.5f),"knowledge chooses road at "+knowledge+" / "+goal);
                // Times at the original speed; the shared delivery speed multiplier (1.2 since the order increase) divides them.
                float expected=knowledge<.5f?6.833333f:knowledge<.8f?6.58f:5.92f;
                Near(route.TotalTime,(expected+(goal=="Goal2"?.4f:0))/map.DeliverySpeedMultiplier,"shortest round trip "+knowledge+" / "+goal);
            }
        }
    }
    public static string Run()
    {
        using var sampleMap=new SampleMapValidationScope();
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var savingScope=save?.SuppressSaving();
        Checks=0;string previous=M5Validation.Run()+" / "+M4PlayValidation.CheckReset();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        Check(economy.FixedCost(1)==90 && economy.FixedCost(2)==105 && economy.FixedCost(30)==525,"fixed cost growth");
        var settings=UnityEngine.Object.FindFirstObjectByType<DaySimulationSettings>();
        Near(settings.AdjustedPromise(60,0,90,1),60,"one successful day cannot shorten promise");
        Near(settings.AdjustedPromise(60,0,90,2),50,"two successful days shorten by ten");
        Near(settings.AdjustedPromise(60,3,50,1),60,"one late day cannot extend promise");
        Near(settings.AdjustedPromise(60,3,50,2),70,"two late days extend by ten");
        Near(settings.AdjustedPromise(60,1,100,2),60,"neutral results preserve promise");
        Check(settings.PromiseTrend(0,89)==0 && settings.PromiseTrend(0,90)==-1
            && settings.PromiseTrend(3,100)==1 && settings.PromiseTrend(2,100)==0,"trend condition boundaries");
        Check(settings.TrainingReserve==100 && settings.HiringReserve==100,"reserves lowered to one hundred");
        Near(settings.ComparisonExtraInitialCash,600,"E2 experimental cash");
        ValidateAdjustment(settings);
        var fixture=new GameObject("Busy fixture");fixture.SetActive(false);
        try {var stats=fixture.AddComponent<DayStats>();Near(stats.BusyRatio(2,300),0,"zero busy ratio");}
        finally {UnityEngine.Object.DestroyImmediate(fixture);}
        ValidatePotentials();ValidateExperience();ValidateTraining();ValidateRecruitment();ValidateSaveGame();ValidatePreparationButtons();
        return previous+$" / PASS: {Checks} M6 checks.";
    }
}
