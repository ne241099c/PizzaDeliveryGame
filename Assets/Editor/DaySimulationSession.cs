using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

/// <summary>Autoplay drives the real UI selection/dispatch and GameClock; it contains no economic formulas.</summary>
public sealed class SimulationDayRecord
{
    public const string Header="seed,day,promise_seconds,revenue,materials,fuel,wages,fixed_costs,profit,cash,reputation,delivered,late,cancelled,accidents,bankrupt,stock,spawned,busy_ratio,hired,roster_size,preparation_costs,policy,couriers,daily_growth,growth_count,training,training_costs,experience_growth,potentials";
    public int Seed,Day,Delivered,Late,Cancelled,Accidents,Stock,Spawned,Hired,RosterSize;
    public float BusyRatio;
    public decimal PreparationCosts;
    public string Policy="legacy",Couriers="",DailyGrowth="";
    public int GrowthCount;
    public string Training="",ExperienceGrowth="",Potentials="";
    public decimal TrainingCosts;
    public float Promise,Reputation;
    public decimal Revenue,Materials,Fuel,Wages,FixedCosts,Profit,Cash;
    public bool Bankrupt;
    public string Csv => string.Join(",",new object[]{Seed,Day,Promise,Revenue,Materials,Fuel,Wages,FixedCosts,Profit,Cash,Reputation,Delivered,Late,Cancelled,Accidents,Bankrupt?1:0,Stock,Spawned,BusyRatio,Hired,RosterSize,PreparationCosts,Policy,Couriers,DailyGrowth,GrowthCount,Training,TrainingCosts,ExperienceGrowth,Potentials}
        .Select(Cell));
    private static string Cell(object value)
    {string s=value is IFormattable f?f.ToString(null,CultureInfo.InvariantCulture):value.ToString();return s.IndexOfAny(new[]{',','"','\n'})>=0 ? "\""+s.Replace("\"","\"\"")+"\"" : s;}
}

public sealed class DaySimulationSession : IDisposable
{
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
    private readonly bool train;private string trained;private decimal trainingCost;
    private readonly bool originalAutomaticTick,originalSpawnerEnabled;
    private readonly int? originalOrderSeed,originalDynamicsSeed,originalRecruitmentSeed;
    private readonly CanvasGroup canvasGroup;
    private readonly bool originalInteractable,originalRaycasts;
    private readonly IDisposable savingScope;
    private readonly DaySimulationSettings settings;
    private readonly bool batch,hire,adjust;
    private readonly string policyName;
    private float currentPromise;
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
    public DaySimulationSession(DaySimulationSettings settings,bool? batchOverride=null,bool? hireOverride=null,bool? adjustOverride=null,string name="comparison",int? daysOverride=null,float[] promisesOverride=null,bool? trainOverride=null)
    {
        this.settings=settings;batch=batchOverride??settings.BatchDispatch;hire=hireOverride??settings.HireApplicants;adjust=adjustOverride??settings.AdjustPromise;policyName=name;train=trainOverride??settings.TrainCouriers;
        if(!Application.isPlaying)throw new InvalidOperationException("Enter Play first.");
        days=daysOverride??settings.DaysPerPolicy;promises=promisesOverride??settings.PromiseSeconds;seed=settings.RandomSeed;step=settings.ClockStepSeconds;maximumDaySeconds=settings.MaximumDaySeconds;
        if(days<1 || promises.Length==0 || promises.Any(v=>v<=0 || float.IsNaN(v) || float.IsInfinity(v))
            || step<=0 || float.IsNaN(step) || float.IsInfinity(step) || maximumDaySeconds<=0 || float.IsNaN(maximumDaySeconds) || float.IsInfinity(maximumDaySeconds))
            throw new ArgumentException("Set valid simulation days, promises and clock step in the Inspector.");
        campaign=Find<CampaignManager>();clock=Find<GameClock>();spawner=Find<OrderSpawner>();dynamics=Find<DeliveryDynamics>();recruitment=Find<RecruitmentManager>();
        economy=Find<BusinessEconomy>();flow=Find<DeliveryFlowController>();roster=Find<DeliveryPersonSelector>();orders=Find<OrderListUI>();stats=Find<DayStats>();reputation=Find<ReputationManager>();planner=Find<DeliveryPlannerUI>();progression=Find<CourierProgression>();training=Find<TrainingManager>();
        originalAutomaticTick=clock.AutomaticTick;originalSpawnerEnabled=spawner.enabled;
        originalOrderSeed=spawner.SeedOverride;originalDynamicsSeed=dynamics.SeedOverride;originalRecruitmentSeed=recruitment.SeedOverride;
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
        spawner.SeedOverride=seed;dynamics.SeedOverride=seed;recruitment.SeedOverride=seed;
        campaign.NewGame();currentPromise=promises[policy];spawner.SetPromiseSeconds(currentPromise);
    }
    public void Step()
    {
        if(Finished || disposed)return;
        if(clock.Phase==DayPhase.BeforeOpening)
        {
            dynamics.SeedOverride=unchecked(seed^campaign.DayNumber);dynamics.ResetForDay();
            spawner.SetPromiseSeconds(currentPromise);hired=0;
            if(hire)
            {
                while(roster.People.Count<recruitment.MaximumPeople)
                {
                    var applicant=recruitment.Applicants.Where(a=>economy.Cash>=recruitment.HiringCost(a.Person)+settings.HiringReserve)
                        .OrderByDescending(a=>(a.Person.Driving+a.Person.LocalKnowledge+a.Person.Service+a.Person.Endurance+a.Person.Safety+a.Person.Capacity)/(double)economy.DailyWage(a.Person)).FirstOrDefault();
                    if(applicant==null || !recruitment.Hire(applicant))break;hired++;
                }
            }
            progression.TrackRoster();trained="";trainingCost=0;
            if(train)
            {
                var choice=roster.People.Where(p=>!training.HasAssignment(p)).SelectMany(p=>Enumerable.Range(0,6).Select(i=>new{Person=p,Ability=(CourierAbility)i}))
                    .Where(c=>c.Person.Ability(c.Ability)<c.Person.Potential(c.Ability) && economy.Cash>=training.Cost(c.Person,c.Ability)+settings.TrainingReserve)
                    .OrderBy(c=>c.Person.Ability(c.Ability)).FirstOrDefault();
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
                var selected=orders.Orders.OrderBy(o=>o.Deadline).ThenBy(o=>o.SpawnedAt).Take(batch && person.CanCarry(2)?2:1).ToArray();
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
        var record=new SimulationDayRecord { Seed=seed,Day=campaign.DayNumber,Promise=spawner.PromiseSeconds,
            Revenue=ledger.Revenue,Materials=ledger.Materials,Fuel=ledger.Fuel,Wages=ledger.Wages,FixedCosts=ledger.FixedCosts,
            Profit=ledger.Profit,Cash=economy.Cash,Reputation=reputation.Value,Delivered=stats.DeliveredCount,Late=stats.LateCount,
            Cancelled=stats.CancelledCount,Accidents=stats.AccidentCount,Bankrupt=campaign.IsBankrupt,Stock=stats.ClosingCancelledCount,Spawned=stats.SpawnedCount,
            BusyRatio=stats.BusyRatio(roster.People.Count,clock.BusinessDuration),Hired=hired,RosterSize=roster.People.Count,
            PreparationCosts=ledger.PreparationCosts,Policy=policyName,Couriers=progression.AbilitiesCsv(),DailyGrowth=progression.GrowthCsv(),GrowthCount=progression.GrowthCount,Training=trained,TrainingCosts=trainingCost,ExperienceGrowth=progression.ExperienceGrowthCsv(),Potentials=progression.PotentialsCsv() };
        Records.Add(record);DayCompleted?.Invoke(record);
        if(campaign.IsBankrupt || campaign.DayNumber>=days)
        {
            policy++;
            if(policy>=promises.Length){Finished=true;return;}
            StartPolicy();
        }
        else { if(adjust)currentPromise=settings.AdjustedPromise(spawner.PromiseSeconds,stats.LateCount,reputation.Value);campaign.NextDay(); }
    }
    public string ToCsv() => SimulationDayRecord.Header+"\n"+string.Join("\n",Records.Select(r=>r.Csv))+"\n";
    public void Dispose()
    {
        if(disposed)return;disposed=true;
        savingScope?.Dispose();
        clock.AutomaticTick=originalAutomaticTick;spawner.enabled=originalSpawnerEnabled;
        spawner.SeedOverride=originalOrderSeed;dynamics.SeedOverride=originalDynamicsSeed;recruitment.SeedOverride=originalRecruitmentSeed;
        canvasGroup.interactable=originalInteractable;canvasGroup.blocksRaycasts=originalRaycasts;
        // Keep the temporary DontSave group reusable: delayed Destroy would invalidate a second run started this frame.
    }
}
