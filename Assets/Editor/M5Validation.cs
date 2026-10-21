using System;
using System.Linq;
using UnityEngine;
using UnityEditor;

public static class M5Validation
{
    [MenuItem("Tools/Pizza Delivery/Validate M5 (Play Mode)")]
    public static void RunMenu() => Debug.Log(Run());
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var orders=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var rep=UnityEngine.Object.FindFirstObjectByType<ReputationManager>();
        var dynamics=UnityEngine.Object.FindFirstObjectByType<DeliveryDynamics>();
        var score=UnityEngine.Object.FindFirstObjectByType<ScoreManager>();
        var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        int checks=0;Action<bool,string> check=(ok,name)=>{if(!ok)throw new Exception("M5 validation failed: "+name);checks++;};
        check(campaign.DayNumber==1 && clock.Phase==DayPhase.BeforeOpening,"initial preparation day 1");
        clock.AutomaticTick=false;campaign.StartBusiness();
        string legacy=M3Validation.Run();
        check(clock.Phase==DayPhase.Open,"opening button starts business");
        spawner.enabled=false;
        check(economy.Cash==300 && economy.DailyRevenue==0,"initial cash 300");
        // Finance integration needs a solvent scenario; defaults are checked before temporary test funding.
        var funding=new SerializedObject(economy);funding.FindProperty("initialCash").floatValue=3000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();
        funding.FindProperty("initialCash").floatValue=300;funding.ApplyModifiedPropertiesWithoutUndo();
        foreach(bool late in new[]{false,true})
        {
            var person=roster.People.First(p=>p.Name=="Aoi");
            var order=new Order("Goal1","Revenue test",clock.ElapsedSeconds,late?0:100);
            orders.AddOrder(order);orders.SelectOnly(order);roster.SelectForDispatch(person);
            var suggestion=RoadPathfinder.Suggest(map,orders.SelectedOrders,person);
            decimal beforeCash=economy.Cash,beforeRevenue=economy.DailyRevenue;
            var route=new ManualDeliveryRoute(map);route.TryReplace(suggestion.EdgeIds);
            check(flow.Dispatch(route.BuildCompletion(orders.SelectedOrders,person),person),"actual revenue trip dispatched");
            check(economy.Cash==beforeCash-20,"materials charged immediately on dispatch");
            var delivery=person.CurrentTrip.Deliveries[0];
            while(flow.ActiveTrips.Count>0)clock.Advance(.25f);
            int expected=score.ScoreArrival(delivery.ArrivesAt,order.Deadline,person.Service);
            check(economy.Cash-beforeCash==expected-20 && economy.DailyRevenue-beforeRevenue==expected,"arrival credits exact tip/late-adjusted score");
            check(late ? expected<100 : expected==116,"late reduction or on-time service tip included");
        }
        check(economy.DailyWage(roster.People.First(p=>p.Name=="Aoi"))==220,"Aoi wage formula");
        check(economy.DailyWage(roster.People.First(p=>p.Name=="Ren"))==184,"Ren wage formula");
        check(economy.Ledger.Materials==40 && economy.Ledger.Distance>0,"actual dispatched materials and completed road distance");
        var sampleOrder=new[]{new Order("Goal1","Distance",0,100)};
        var samplePerson=roster.People.First(p=>p.Name=="Aoi");
        var suggested=RoadPathfinder.Suggest(map,sampleOrder,samplePerson);
        var shortRoute=new ManualDeliveryRoute(map);shortRoute.TryReplace(suggested.EdgeIds);
        var clean=new DeliveryTrip(map,shortRoute.BuildCompletion(sampleOrder,samplePerson),samplePerson,0,5);
        var detour=new ManualDeliveryRoute(map);detour.TryReplace(new[]{"Store-N1","Store-N1"}.Concat(suggested.EdgeIds));
        var longer=new DeliveryTrip(map,detour.BuildCompletion(sampleOrder,samplePerson),samplePerson,0,5);
        check(longer.DistanceAt(longer.ReturnsAt)>clean.DistanceAt(clean.ReturnsAt),"detour increases actual distance and fuel");
        var paused=new DeliveryTrip(map,shortRoute.BuildCompletion(sampleOrder,samplePerson),samplePerson,0,5,dynamics,()=>0d);
        paused.AdvanceAccidents(0);
        check(paused.AccidentCount==1 && paused.DistanceAt(5)==0 && paused.DistanceAt(10)==0,"accident stop adds no distance");
        paused.AdvanceAccidents(10000);
        check(Mathf.Abs(paused.DistanceAt(paused.ReturnsAt)-clean.DistanceAt(clean.ReturnsAt))<.001f,"accidents do not increase total fuel distance");
        decimal beforeSettlement=economy.Cash;
        var retained=roster.People.ToArray();rep.RecordArrival(0,0);
        orders.AddOrder(new Order("Goal1","Wait",clock.ElapsedSeconds,60));
        score.AddScore(10);stats.RecordDelivery(10,0);
        dynamics.ApplyTripFatigue(retained[0],1,retained[0].Endurance,100);
        clock.Advance(clock.BusinessDuration+1);
        check(clock.Phase==DayPhase.Ended && flow.ActiveTrips.Count==0,"settlement after close and all returns");
        var ledger=economy.Ledger;
        check(ledger.Settled && ledger.FixedCosts==150 && ledger.Wages==roster.People.Sum(p=>economy.DailyWage(p)),"settlement pays all wages and fixed cost");
        check(ledger.Fuel==economy.FuelCost(ledger.Distance),"fuel uses actual distance");
        check(economy.Cash==beforeSettlement-ledger.Fuel-ledger.Wages-ledger.FixedCosts,"materials are not deducted twice");
        check(economy.Cash==ledger.OpeningCash+ledger.Profit,"profit reconciles to cash");
        decimal carriedCash=economy.Cash;economy.SettleDay();
        check(economy.Cash==carriedCash,"settlement is applied only once");
        float value=rep.Value;int total=campaign.TotalScore;int daily=stats.TotalScore;
        campaign.NextDay();
        check(economy.Cash==carriedCash && economy.DailyRevenue==0,"cash carries and revenue resets");
        check(campaign.DayNumber==2 && clock.Phase==DayPhase.BeforeOpening,"next day returns to preparation");
        check(rep.Value==value && total==daily,"reputation and cumulative score carried");
        check(roster.People.SequenceEqual(retained),"roster objects and abilities carried");
        check(roster.People.All(p=>p.CurrentStamina==1 && p.CurrentTrip==null && p.IsAvailable),"stamina and trips reset");
        check(orders.OrderCount==0 && orders.SelectedOrders.Count==0,"orders and selection reset");
        check(score.Score==0 && stats.TotalScore==0 && stats.DeliveredCount==0 && clock.ElapsedSeconds==0,"daily statistics and clock reset");
        campaign.StartBusiness();check(clock.Phase==DayPhase.Open,"second business can start");
        check(!campaign.IsBankrupt && campaign.TotalRevenue==ledger.Revenue && campaign.BestDailyRevenue==ledger.Revenue,"solvent day and cumulative revenue records");
        check(!campaign.ShouldDeclareBankruptcy(0,1) && campaign.ShouldDeclareBankruptcy(-1,1),"bankruptcy is strictly below zero at settlement");
        var grace=new SerializedObject(campaign);grace.FindProperty("bankruptcyGraceDays").intValue=1;grace.ApplyModifiedPropertiesWithoutUndo();
        check(!campaign.ShouldDeclareBankruptcy(-1,1) && campaign.ShouldDeclareBankruptcy(-1,2),"configurable consecutive-day grace");
        grace.FindProperty("bankruptcyGraceDays").intValue=0;grace.ApplyModifiedPropertiesWithoutUndo();
        campaign.NewGame();spawner.enabled=false;campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1);
        check(campaign.IsBankrupt && economy.Cash<0 && clock.Phase==DayPhase.Ended,"actual empty business becomes bankrupt only at settlement");
        int bankruptDay=campaign.DayNumber;campaign.NextDay();check(campaign.DayNumber==bankruptDay,"bankruptcy blocks next day");
        check(campaign.FinancialSummary.Contains("BANKRUPT") && campaign.FinancialSummary.Contains("Materials:") && campaign.FinancialSummary.Contains("Best day:"),"bankruptcy summary includes costs and records");
        campaign.NewGame();
        check(campaign.DayNumber==1 && !campaign.IsBankrupt && campaign.TotalRevenue==0 && economy.Cash==300 && rep.Value==50,"start over resets campaign money reputation records");
        check(roster.People.Select(p=>p.Name).SequenceEqual(new[]{"Aoi","Ren"}) && roster.People.All(p=>p.CurrentStamina==1),"start over restores two-person roster and stamina");
        var recruitment=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();
        recruitment.SeedOverride=42;recruitment.ResetRandom();recruitment.GenerateDay();
        check(recruitment.Applicants.Count>=2 && recruitment.Applicants.Count<=3,"daily applicants count 2-3");
        check(recruitment.Applicants.Select(a=>a.Person.Name).Distinct().Count()==recruitment.Applicants.Count
            && !recruitment.Applicants.Any(a=>roster.People.Any(p=>p.Name==a.Person.Name)),"applicant names unique against team and each other");
        var fixture=new GameObject("Recruitment formula fixture");fixture.SetActive(false);
        try
        {
            var generator=fixture.AddComponent<RecruitmentManager>();generator.SeedOverride=17;generator.ResetRandom();
            var profiles=Enumerable.Range(0,100).Select(i=>generator.GenerateApplicant("Test"+i)).ToArray();
            check(profiles.All(a=>a.Strengths.Count==2 && a.Weaknesses.Count==2 && !a.Strengths.Intersect(a.Weaknesses).Any()),"two disjoint strengths and weaknesses");
            check(profiles.All(a=>a.Baselines.All(v=>v>=.3f && v<=.7f)),"baseline generation range");
            Func<Applicant,float[]> abilities=a=>new[]{a.Person.Driving,a.Person.LocalKnowledge,a.Person.Service,a.Person.Endurance,a.Person.Safety};
            check(profiles.All(a=>Enumerable.Range(0,5).All(i=>
            {
                float actual=abilities(a)[i],delta=actual-a.Baselines[i];
                return actual>=0 && actual<=1 && (a.Strengths.Contains(i) ? delta>=.19999f && delta<=.30001f
                    : a.Weaknesses.Contains(i) ? delta<=-.19999f && delta>=-.30001f : Mathf.Abs(delta)<.00001f);
            })),"strength and weakness adjustment bounds with remaining ability unchanged");
            check(profiles.Select(a=>a.Person.Capacity).Distinct().OrderBy(v=>v).SequenceEqual(new[]{1,2}),"fixed sample contains both capacity choices");
            generator.ResetRandom();var replay=Enumerable.Range(0,100).Select(i=>generator.GenerateApplicant("Test"+i)).ToArray();
            check(profiles.Zip(replay,(a,b)=>abilities(a).SequenceEqual(abilities(b)) && a.Person.Capacity==b.Person.Capacity).All(v=>v),"same seed reproduces applicant abilities and capacities");
        }
        finally {UnityEngine.Object.DestroyImmediate(fixture);}
        funding.FindProperty("initialCash").floatValue=0;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();
        var candidate=recruitment.Applicants[0];int teamSize=roster.People.Count;
        check(!recruitment.Hire(candidate) && recruitment.LastMessage.Contains("cash") && roster.People.Count==teamSize,"insufficient cash rejects hiring with reason");
        funding.FindProperty("initialCash").floatValue=5000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();
        funding.FindProperty("initialCash").floatValue=300;funding.ApplyModifiedPropertiesWithoutUndo();
        decimal beforeHire=economy.Cash,fee=recruitment.HiringCost(candidate.Person);
        check(recruitment.Hire(candidate) && roster.People.Contains(candidate.Person) && economy.Cash==beforeHire-fee,"hiring updates roster and charges three wage days");
        check(economy.Ledger.PreparationCosts==fee && fee==economy.DailyWage(candidate.Person)*3,"hiring cost recorded separately from business profit");
        decimal hiredWages=roster.People.Sum(p=>economy.DailyWage(p));
        campaign.StartBusiness();orders.AddOrder(new Order("Goal1","New hire",0,100));orders.SelectOnly(orders.Orders[0]);
        check(roster.SelectForDispatch(candidate.Person),"new hire selectable on the same day");
        var hireRoute=new ManualDeliveryRoute(map);hireRoute.TryReplace(RoadPathfinder.Suggest(map,orders.SelectedOrders,candidate.Person).EdgeIds);
        check(flow.Dispatch(hireRoute.BuildCompletion(orders.SelectedOrders,candidate.Person),candidate.Person),"new hire dispatches through existing flow");
        check(!recruitment.Fire(roster.People[0]) && recruitment.LastMessage.Contains("preparation"),"firing blocked during business");
        check(!recruitment.Hire(recruitment.Applicants.FirstOrDefault()) && recruitment.LastMessage.Contains("preparation"),"hiring blocked during business");
        clock.Advance(clock.BusinessDuration+1000);
        check(economy.Ledger.Wages==hiredWages,"hired courier wage included at settlement");
        check(economy.Cash==economy.OpeningCash+economy.Ledger.Profit-economy.Ledger.PreparationCosts,"cash reconciles including preparation fees");
        var oldApplicants=recruitment.Applicants.ToArray();campaign.NextDay();
        check(clock.Phase==DayPhase.BeforeOpening && !recruitment.Applicants.Intersect(oldApplicants).Any(),"unhired applicants expire next morning");
        check(roster.People.Contains(candidate.Person) && candidate.Person.CurrentStamina==1,"hired courier carried with full stamina");
        check(recruitment.Hire(recruitment.Applicants[0]) && roster.People.Count==4,"fourth courier can be hired");
        recruitment.GenerateDay();check(!recruitment.Hire(recruitment.Applicants[0]) && recruitment.LastMessage.Contains("limit"),"team limit rejects fifth hire with reason");
        decimal beforeFire=economy.Cash,severance=recruitment.SeveranceCost(roster.People[0]);
        check(recruitment.Fire(roster.People[0]) && economy.Cash==beforeFire-severance,"firing charges one daily wage and removes courier");
        while(roster.People.Count>1)recruitment.Fire(roster.People[0]);
        check(!recruitment.Fire(roster.People[0]) && recruitment.LastMessage.Contains("at least one"),"last courier cannot be fired with reason");
        campaign.NewGame();
        check(roster.People.Count==2 && economy.Cash==300 && economy.Ledger.PreparationCosts==0,"new game clears hired roster and preparation expenses");
        return legacy+$" / PASS: {checks} M5 checks.";
    }
}
