using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed partial class RewardProgression
{
    [SerializeField,Tooltip("現在の自動経路は最速探索なので、これを上回る課題は既定で出題しません。")] private bool offerOptimalRouteChallenge=false;
    [SerializeField] private List<MissionDefinition> missionDefinitions=MissionDefinition.Defaults();
    [SerializeField,Min(0)] private float goalNetAssets=5000,goalReputation=90;
    [SerializeField,Min(1)] private int goalDays=30,goalNoLateDays=3;
    private int maxBatch,backstreets,failedPizzas;
    private float routeSavings;
    private readonly HashSet<PizzaJob> failedJobs=new();
    private readonly HashSet<string> madePizzas=new(),threeStarPizzas=new(),visitedDistricts=new(),shortcuts=new();
    private void OnEnable()
    {
        var f=FindFirstObjectByType<DeliveryFlowController>();
        if(f!=null)
        {
            f.Dispatched+=Dispatched;
            f.Returned+=Returned;
        }
        var c=FindFirstObjectByType<CookingManager>();
        if(c!=null)
        {
            c.PizzaPacked+=Packed;
            c.PizzaDiscarded+=Discarded;
        }
    }
    private void OnDisable()
    {
        var f=FindFirstObjectByType<DeliveryFlowController>();
        if(f!=null)
        {
            f.Dispatched-=Dispatched;
            f.Returned-=Returned;
        }
        var c=FindFirstObjectByType<CookingManager>();
        if(c!=null)
        {
            c.PizzaPacked-=Packed;
            c.PizzaDiscarded-=Discarded;
        }
    }
    private void ResetTelemetry()
    {
        maxBatch=backstreets=failedPizzas=0;
        routeSavings=0;
        failedJobs.Clear();
        madePizzas.Clear();
        threeStarPizzas.Clear();
        visitedDistricts.Clear();
        shortcuts.Clear();
        promotionNotice="";
        achievementNotice="";
    }
    private void Packed(PizzaJob job)
    {
        madePizzas.Add(job.Order.Kind.ToString());
        if(job.Stars==3)threeStarPizzas.Add(job.Order.Kind.ToString());
        if(job.Stars==1 && failedJobs.Add(job))failedPizzas++;
    }
    private void Discarded(PizzaJob job){if((job.Burnt || job.Stage>=PizzaStage.Baked && job.Stars==1) && failedJobs.Add(job))failedPizzas++;}
    private void Dispatched(DeliveryTrip trip)
    {
        maxBatch=Math.Max(maxBatch,trip.Deliveries.Count);
        // At dispatch these timestamps have no accident delays yet; compare to the automatic suggestion then.
        var person=trip.Person.Clone();
        person.IsAvailable=true;
        var suggestion=RoadPathfinder.Suggest(FindFirstObjectByType<MapManager>(),trip.Deliveries.Select(d=>d.Order).ToArray(),person);
        if(suggestion!=null)routeSavings=Mathf.Max(routeSavings,Clock.ToRealSeconds(suggestion.TotalTime)-(trip.ReturnsAt-trip.StartedAt));
    }
    private void Returned(DeliveryTrip trip)
    {
        backstreets+=trip.BackstreetCount;var map=FindFirstObjectByType<MapManager>();
        foreach(var id in trip.EdgeIds)if(map.GetEdge(id).RoadType==RoadType.Backstreet)shortcuts.Add(id);
        foreach(var d in trip.Deliveries)
        {
            var block=map.City?.blocks.FirstOrDefault(b=>b.addresses.Contains(d.Order.DestinationNodeId));
            visitedDistricts.Add(block!=null?block.template.kind.ToString():"いつもの街");
        }
    }
    private RewardDayMetrics CaptureMetrics()
    {
        var s=FindFirstObjectByType<DayStats>();
        return new RewardDayMetrics{oneStar=s.StarCounts[1],twoStar=s.StarCounts[2],accidents=s.AccidentCount,spawned=s.SpawnedCount,closingCancelled=s.ClosingCancelledCount,score=s.TotalScore,twoStarOnTime=s.TwoStarOnTime,threeStarOnTime=s.ThreeStarOnTime,busySeconds=s.BusySeconds,playerMade=FindFirstObjectByType<CookingManager>().PackedByPlayer,staffNames=FindFirstObjectByType<CookingManager>().PackedByStaff.Keys.ToArray(),staffCounts=FindFirstObjectByType<CookingManager>().PackedByStaff.Values.ToArray(),day=Campaign.DayNumber,delivered=s.DeliveredCount,late=s.LateCount,cancelled=s.CancelledCount,threeStar=s.StarCounts[3],failedPizzas=failedPizzas,wasted=Economy.Ledger.WastedPizzas,staffMade=FindFirstObjectByType<CookingManager>().PackedByStaff.Values.Sum(),maxBatch=maxBatch,backstreets=backstreets,routeSavings=routeSavings,revenue=Money(Economy.DailyRevenue),madePizzas=madePizzas.ToArray(),threeStarPizzas=threeStarPizzas.ToArray(),visitedDistricts=visitedDistricts.ToArray(),shortcuts=shortcuts.ToArray()};
    }
    public IReadOnlyList<MissionState> TodayMissions=>State.missions;
    public string MissionLabel(MissionState m){var d=RewardMissions.Lookup(missionDefinitions,m.id);return d!=null?RewardMissions.Title(d,m):m.id;}
    private void AwardMissions()
    {
        decimal amount=0;
        foreach(var m in State.missions)
        {
            var d=RewardMissions.Lookup(missionDefinitions,m.id);
            m.completed=d!=null && RewardMissions.Evaluate(d,m,State.last);
            // Every mission of the day counts; there is nothing to choose or to claim.
            if(m.completed && !m.rewarded)
            {
                m.rewarded=true;
                amount+=CampaignSaveManager.Decimal(m.cashReward);
                State.points+=m.points;
            }
        }
        Economy.ApplyGoalCash(amount);State.missionCash=Money(amount);State.previousMissions=State.missions.Select(m=>m.id).ToArray();
    }
    public string GoalLabel=>State.goalId switch{"assets"=>$"{goalDays}日までに純資産{goalNetAssets:F0}円","reputation"=>$"評判{goalReputation:F0}","streak"=>$"{goalNoLateDays}日連続遅れなし",_=>"自由な目標：未選択"};
    public void SetEndlessGoal(string id)
    {
        if(State.mode!=GameMode.Endless || Clock.Phase!=DayPhase.BeforeOpening || !new[]{"assets","reputation","streak"}.Contains(id))return;
        State.goalId=id;
        State.goalCompleted=false;
        SaveCheckpoint();
    }
    private void CheckCustomGoal()
    {
        if(State.mode!=GameMode.Endless || State.goalCompleted)return;
        State.goalCompleted=State.goalId switch{"assets"=>Campaign.DayNumber<=goalDays && NetAssets>=(decimal)goalNetAssets,"reputation"=>FindFirstObjectByType<ReputationManager>().Value>=goalReputation,"streak"=>State.noLateStreak>=goalNoLateDays,_=>false};
        if(State.goalCompleted)EvaluateProfileAchievements();
    }
}
