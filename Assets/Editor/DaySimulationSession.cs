using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEditor;

/// <summary>Autoplay drives the real UI selection/dispatch and GameClock; it contains no economic formulas.</summary>
public sealed class SimulationDayRecord
{
    public const string Header="seed,day,promise_seconds,revenue,materials,fuel,wages,fixed_costs,profit,cash,reputation,delivered,late,cancelled,accidents,bankrupt,stock,spawned,busy_ratio,hired,roster_size,preparation_costs,policy,couriers,daily_growth,growth_count,training,training_costs,experience_growth,potentials,requested_promise_seconds,quality_bonus,repairs,oven_level,bike_level,shop_level,maximum_cooking_queue,oven_utilization,equipment_maintenance,facility_purchases,minimum_promise_seconds,facility_purchase,two_star_on_time,three_star_on_time,kitchen_staff";
    public int Seed,Day,Delivered,Late,Cancelled,Accidents,Stock,Spawned,Hired,RosterSize;
    public int OvenLevel,BikeLevel,ShopLevel,MaximumCookingQueue,TwoStarOnTime,ThreeStarOnTime,KitchenStaff;
    public float OvenUtilization,MinimumPromise;
    public decimal EquipmentMaintenance,FacilityPurchases;
    public string FacilityPurchase="";
    public float BusyRatio;
    public decimal PreparationCosts;
    public string Policy="legacy",Couriers="",DailyGrowth="";
    public int GrowthCount;
    public string Training="",ExperienceGrowth="",Potentials="";
    public decimal TrainingCosts,QualityBonus,Repairs;
    public float Promise,Reputation,RequestedPromise;
    public decimal Revenue,Materials,Fuel,Wages,FixedCosts,Profit,Cash;
    public bool Bankrupt;
    public string Csv => string.Join(",",new object[]{Seed,Day,Promise,Revenue,Materials,Fuel,Wages,FixedCosts,Profit,Cash,Reputation,Delivered,Late,Cancelled,Accidents,Bankrupt?1:0,Stock,Spawned,BusyRatio,Hired,RosterSize,PreparationCosts,Policy,Couriers,DailyGrowth,GrowthCount,Training,TrainingCosts,ExperienceGrowth,Potentials,RequestedPromise,QualityBonus,Repairs,OvenLevel,BikeLevel,ShopLevel,MaximumCookingQueue,OvenUtilization,EquipmentMaintenance,FacilityPurchases,MinimumPromise,FacilityPurchase,TwoStarOnTime,ThreeStarOnTime,KitchenStaff}
        .Select(Cell));
    private static string Cell(object value)
    {string s=value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value.ToString();return s.IndexOfAny(new[]{',','"','\n'})>=0 ? "\""+s.Replace("\"","\"\"")+"\"" : s;}
}

public sealed class DaySimulationSession : IDisposable
{
    private readonly MapManager map;
    private readonly int? originalCitySeed,originalBlocks;
    private readonly bool originalSample;
    private readonly int? targetKitchenStaff;
    private readonly bool substituteKitchen;
    private readonly CampaignManager campaign;
    private readonly GameClock clock;
    private readonly OrderSpawner spawner;
    private readonly DeliveryDynamics dynamics;
    private readonly RecruitmentManager recruitment;
    private readonly BusinessEconomy economy;
    private readonly DeliveryFlowController flow;
    private readonly DeliveryPersonSelector roster;
    private readonly OrderListUI orders;
    private readonly DayStats stats;
    private readonly ReputationManager reputation;
    private readonly DeliveryPlannerUI planner;
    private readonly CourierProgression progression;
    private readonly TrainingManager training;
    private readonly FacilityManager facilities;
    private readonly bool buyFacilities;
    private string facilityPurchase;
    private float operatingMinimum;
    private readonly CookingManager cooking;
    private readonly bool originalManualCooking;
    private readonly bool train;private string trained;private decimal trainingCost;
    private readonly bool originalAutomaticTick,originalSpawnerEnabled;
    private readonly int? originalOrderSeed,originalDynamicsSeed,originalRecruitmentSeed,originalCookingSeed;
    private readonly CanvasGroup canvasGroup;
    private readonly bool originalInteractable,originalRaycasts;
    private readonly IDisposable savingScope;
    private readonly DaySimulationSettings settings;
    private readonly bool batch,hire,adjust;
    private readonly string policyName;
    private float currentPromise;
    private int previousTrend, trendStreak;
    private readonly float? initialCashOverride;
    private int hired;
    private readonly int days,seed;
    private readonly float[] promises;
    private readonly float step,maximumDaySeconds;
    private int policy;
    private bool disposed;
    public bool Finished { get; private set; }
    public List<SimulationDayRecord> Records { get; }=new();
    public string Progress => Finished ? $"Finished / {Records.Count} days recorded" : $"Promise {promises[policy]:F0}s / day {campaign.DayNumber}/{days}";
    public event Action<SimulationDayRecord> DayCompleted;
    private static T Find<T>() where T:UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>()
        ?? throw new InvalidOperationException("Missing scene component: "+typeof(T).Name);
    public DaySimulationSession(DaySimulationSettings settings,bool? batchOverride=null,bool? hireOverride=null,bool? adjustOverride=null,string name="comparison",int? daysOverride=null,float[] promisesOverride=null,bool? trainOverride=null,float? initialCashOverride=null,bool? facilityOverride=null,int? citySeedOverride=null,int? cityBlocksOverride=null,int? kitchenStaffOverride=null,bool substituteKitchen=false)
    {
        map=Find<MapManager>();originalCitySeed=map.SeedOverride;originalBlocks=map.BlocksOverride;originalSample=map.UseSampleForValidation;
        if(citySeedOverride.HasValue){map.UseSampleForValidation=false;map.SeedOverride=citySeedOverride;}
        if(cityBlocksOverride.HasValue)map.BlocksOverride=cityBlocksOverride;
        targetKitchenStaff=kitchenStaffOverride;this.substituteKitchen=substituteKitchen;
        buyFacilities=facilityOverride??settings.BuyFacilities;facilities=Find<FacilityManager>();
        this.initialCashOverride=initialCashOverride;
        this.settings=settings;batch=batchOverride??settings.BatchDispatch;hire=hireOverride??settings.HireApplicants;adjust=adjustOverride??settings.AdjustPromise;policyName=name;train=trainOverride??settings.TrainCouriers;
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        days=daysOverride??settings.DaysPerPolicy;promises=promisesOverride??settings.PromiseSeconds;seed=settings.RandomSeed;step=settings.ClockStepSeconds;maximumDaySeconds=settings.MaximumDaySeconds;
        if(days<1 || promises.Length==0 || promises.Any(v=>v<=0 || float.IsNaN(v) || float.IsInfinity(v))
            || step<=0 || float.IsNaN(step) || float.IsInfinity(step) || maximumDaySeconds<=0 || float.IsNaN(maximumDaySeconds) || float.IsInfinity(maximumDaySeconds))
            throw new ArgumentException("Set valid simulation days, promises and clock step in the Inspector.");
        campaign=Find<CampaignManager>();clock=Find<GameClock>();spawner=Find<OrderSpawner>();dynamics=Find<DeliveryDynamics>();recruitment=Find<RecruitmentManager>();
        economy=Find<BusinessEconomy>();flow=Find<DeliveryFlowController>();roster=Find<DeliveryPersonSelector>();orders=Find<OrderListUI>();stats=Find<DayStats>();reputation=Find<ReputationManager>();planner=Find<DeliveryPlannerUI>();progression=Find<CourierProgression>();training=Find<TrainingManager>();
        cooking=Find<CookingManager>();originalManualCooking=cooking.ManualControlEnabled;cooking.ManualControlEnabled=false;
        originalAutomaticTick=clock.AutomaticTick;originalSpawnerEnabled=spawner.enabled;
        originalOrderSeed=spawner.SeedOverride;originalDynamicsSeed=dynamics.SeedOverride;originalRecruitmentSeed=recruitment.SeedOverride;originalCookingSeed=cooking.SeedOverride;
        var canvas=Find<Canvas>();canvasGroup=canvas.GetComponent<CanvasGroup>();
        if(canvasGroup==null){canvasGroup=canvas.gameObject.AddComponent<CanvasGroup>();canvasGroup.hideFlags=HideFlags.DontSave;}
        originalInteractable=canvasGroup.interactable;originalRaycasts=canvasGroup.blocksRaycasts;
        canvasGroup.interactable=false;canvasGroup.blocksRaycasts=false;
        savingScope=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>()?.SuppressSaving();
        clock.AutomaticTick=false;spawner.enabled=true;
        StartPolicy();
    }
    private void StartPolicy()
    {
        // Separate sources prevent UI rendering/recruitment from consuming order/accident randomness.
        spawner.SeedOverride=seed;dynamics.SeedOverride=seed;recruitment.SeedOverride=seed;cooking.SeedOverride=seed;
        // E2 is an experimental initial condition only; never change the game's configured cash.
        var funding = new SerializedObject(economy);
        var initial = funding.FindProperty("initialCash");
        float originalCash = initial.floatValue;
        try
        {
            if (initialCashOverride.HasValue) { initial.floatValue=initialCashOverride.Value;funding.ApplyModifiedPropertiesWithoutUndo(); }
            campaign.NewGame();
            if(substituteKitchen)Find<KitchenCrew>().Restore(new[]{new KitchenStaff("Simulation cook",.5f,.5f)});
        }
        finally { initial.floatValue=originalCash;funding.ApplyModifiedPropertiesWithoutUndo(); }
        previousTrend=trendStreak=0;
        currentPromise=promises[policy];spawner.SetPromiseSeconds(currentPromise);
    }
    public void Step()
    {
        if(Finished || disposed)return;
        if(clock.Phase==DayPhase.BeforeOpening)
        {
            dynamics.SeedOverride=unchecked(seed^campaign.DayNumber);dynamics.ResetForDay();
            spawner.SetPromiseSeconds(currentPromise);hired=0;
            facilityPurchase="";
            if(buyFacilities)
            {
                foreach(var type in new[]{FacilityType.Oven,FacilityType.Workbench,FacilityType.Bike,FacilityType.Menu,FacilityType.Advertising})
                {
                    if(facilities.IsMaximum(type) || economy.Cash<facilities.Price(type)+settings.FacilityReserve)continue;
                    if(!facilities.Purchase(type))throw new InvalidOperationException(facilities.LastMessage);
                    facilityPurchase=type.ToString();break;
                }
            }
            if(targetKitchenStaff.HasValue)
            {
                var kitchen=Find<KitchenCrew>();
                foreach(var candidate in recruitment.KitchenApplicants.ToArray())if(kitchen.Count<targetKitchenStaff.Value && economy.Cash>=recruitment.HiringCost(candidate.Staff)+settings.HiringReserve)recruitment.Hire(candidate);
            }
            operatingMinimum=spawner.MinimumPromiseSeconds;
            if(hire)
            {
                while(recruitment.TeamCount<recruitment.MaximumPeople)
                {
                    var applicant=recruitment.Applicants.Where(a=>economy.Cash>=recruitment.HiringCost(a.Person)+settings.HiringReserve)
                        .OrderByDescending(a=>(a.Person.Driving+a.Person.LocalKnowledge+a.Person.Service+a.Person.Endurance+a.Person.Safety+a.Person.Capacity)/(double)economy.DailyWage(a.Person)).FirstOrDefault();
                    if(applicant==null || !recruitment.Hire(applicant))break;hired++;
                }
            }
            progression.TrackRoster();trained="";trainingCost=0;
            if(train)
            {
                var choice=roster.People.Where(p=>!training.HasAssignment(p))
                    .Select(p=>new{Person=p,Preferred=settings.PreferredTrainingAbility(p)})
                    .Where(c=>c.Preferred.HasValue)
                    .Select(c=>new{c.Person,Ability=c.Preferred.Value})
                    .Where(c=>economy.Cash>=training.Cost(c.Person,c.Ability)+settings.TrainingReserve)
                    .OrderBy(c=>settings.TrainingPriority(c.Ability)).FirstOrDefault();
                if(choice!=null)
                {
                    trainingCost=training.Cost(choice.Person,choice.Ability);
                    if(!training.Request(choice.Person,choice.Ability))throw new InvalidOperationException(training.LastMessage);
                    trained=choice.Person.Name+"."+choice.Ability;
                }
            }
            campaign.StartBusiness();
        }
        if(clock.Phase==DayPhase.Open)
        {
            foreach(var person in roster.People.Where(p=>p.IsAvailable && p.CanCarry(1)).ToArray())
            {
                var selected=orders.Orders.Where(o=>o.IsReady && orders.CanSelect(o)).OrderBy(o=>o.Deadline).ThenBy(o=>o.SpawnedAt).Take(batch && person.CanCarry(2)?2:1).ToArray();
                if(selected.Length==0)break;
                if(!orders.SelectForDispatch(selected) || !roster.SelectForDispatch(person))throw new InvalidOperationException("Autoplay could not select a pending order and idle courier.");
                int before=flow.ActiveTrips.Count;planner.DispatchWithSuggestion();
                if(flow.ActiveTrips.Count!=before+1)throw new InvalidOperationException("Suggested dispatch was rejected: "+flow.DispatchRejectionReason(planner.SelectedPlan,person));
            }
        }
        clock.IsPlanning=false;clock.Advance(step);
        if(clock.ElapsedSeconds>maximumDaySeconds && clock.Phase!=DayPhase.Ended)
            throw new InvalidOperationException("Simulation exceeded maximum day time; inspect active trips.");
        if(clock.Phase!=DayPhase.Ended)return;
        var ledger=economy.Ledger;
        var record=new SimulationDayRecord { Seed=seed,Day=campaign.DayNumber,Promise=spawner.PromiseSeconds,RequestedPromise=promises[policy],QualityBonus=ledger.QualityBonus,Repairs=ledger.Repairs,
            OvenLevel=facilities.LastOperatingState.oven+1,BikeLevel=facilities.LastOperatingState.bike+1,ShopLevel=facilities.LastOperatingState.expansion,MaximumCookingQueue=cooking.MaximumQueueLength,OvenUtilization=cooking.Utilization,EquipmentMaintenance=ledger.EquipmentMaintenance,FacilityPurchases=ledger.FacilityPurchases,MinimumPromise=operatingMinimum,FacilityPurchase=facilityPurchase,
            Revenue=ledger.Revenue,Materials=ledger.Materials,Fuel=ledger.Fuel,Wages=ledger.Wages,FixedCosts=ledger.FixedCosts,
            Profit=ledger.Profit,Cash=economy.Cash,Reputation=reputation.Value,Delivered=stats.DeliveredCount,Late=stats.LateCount,
            Cancelled=stats.CancelledCount,Accidents=stats.AccidentCount,Bankrupt=campaign.IsBankrupt,Stock=stats.ClosingCancelledCount,Spawned=stats.SpawnedCount,
            BusyRatio=stats.BusyRatio(roster.People.Count,clock.BusinessDuration),Hired=hired,RosterSize=roster.People.Count,
            PreparationCosts=ledger.PreparationCosts,Policy=policyName,Couriers=progression.AbilitiesCsv(),DailyGrowth=progression.GrowthCsv(),GrowthCount=progression.GrowthCount,Training=trained,TrainingCosts=trainingCost,ExperienceGrowth=progression.ExperienceGrowthCsv(),Potentials=progression.PotentialsCsv(),
            TwoStarOnTime=stats.TwoStarOnTime,ThreeStarOnTime=stats.ThreeStarOnTime,KitchenStaff=recruitment.TeamCount-roster.People.Count };
        Records.Add(record);DayCompleted?.Invoke(record);
        if(campaign.IsBankrupt || campaign.DayNumber>=days)
        {
            policy++;
            if(policy>=promises.Length){Finished=true;return;}
            StartPolicy();
        }
        else
        {
            int trend=settings.PromiseTrend(stats.LateCount,reputation.Value);
            trendStreak=settings.NextTrendStreak(previousTrend,trendStreak,stats.LateCount,reputation.Value);
            previousTrend=trend;
            if(adjust)currentPromise=settings.AdjustedPromise(spawner.PromiseSeconds,stats.LateCount,reputation.Value,trendStreak);
            campaign.NextDay();
        }
    }
    public string ToCsv() => SimulationDayRecord.Header+"\n"+string.Join("\n",Records.Select(r=>r.Csv))+"\n";
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        // Cleanup remains inside suppression, including frames between batches.
        campaign.NewGame();clock.AutomaticTick=false;Find<TitleMenuUI>().ReturnTitle();
        map.SeedOverride=originalCitySeed;map.BlocksOverride=originalBlocks;map.UseSampleForValidation=originalSample;
        savingScope?.Dispose();
        cooking.ManualControlEnabled=originalManualCooking;
        clock.AutomaticTick=originalAutomaticTick;spawner.enabled=originalSpawnerEnabled;
        spawner.SeedOverride=originalOrderSeed;dynamics.SeedOverride=originalDynamicsSeed;recruitment.SeedOverride=originalRecruitmentSeed;cooking.SeedOverride=originalCookingSeed;
        canvasGroup.interactable=originalInteractable;canvasGroup.blocksRaycasts=originalRaycasts;
        // Keep the temporary DontSave group reusable: delayed Destroy would invalidate a second run started this frame.
    }
}
