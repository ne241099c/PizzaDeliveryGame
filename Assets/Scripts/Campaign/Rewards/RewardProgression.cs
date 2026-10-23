using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

public enum GameMode { Normal, Endless }
[Serializable] public sealed class RewardSaveData
{
    public int version=1,normalDays=20,grade=1,totalDelivered,totalOnTime,totalThreeStars,points,noLateStreak;
    public GameMode mode;
    public string runId,loan="0",interest="0",missionCash="0",goalId="",settlementText="";
    public bool resultSeen,resultPending,resultRecorded,ended,loanCelebrated,goalCompleted;
    public List<RewardDayMetrics> recent=new();
    public List<MissionState> missions=new();
    public string[] previousMissions=Array.Empty<string>();
    public string[] ledger;
    public RewardDayMetrics last;
}
[Serializable] public sealed class RewardGradeDefinition
{
    public string name; public float reputation,revenue,netAssets,onTimeRate,threeStarRate;
    public int districts=5,menuKinds=1,equipmentLevel=10;
}
/// <summary>Additive campaign goals. Does not replace the delivery score or operating ledger.</summary>
public sealed partial class RewardProgression : MonoBehaviour
{
    [SerializeField,Min(1)] private int normalDays=20;
    [SerializeField] private float revenueWeight=.1f,reputationWeight=10,threeStarWeight=1000,onTimeWeight=1000;
    [SerializeField] private float[] rankThresholds={15000,10000,6000,2000};
    public RewardSaveData State {get;private set;}=new();
    public static RewardProgression Get(CampaignManager campaign)
    {var p=campaign.GetComponent<RewardProgression>();return p!=null?p:campaign.gameObject.AddComponent<RewardProgression>();}
    private CampaignManager Campaign=>GetComponent<CampaignManager>();
    private BusinessEconomy Economy=>FindFirstObjectByType<BusinessEconomy>();
    private GameClock Clock=>FindFirstObjectByType<GameClock>();
    public bool HasPendingResult=>State.resultPending && !State.resultSeen;
    public decimal NetAssets=>Economy.Cash-Loan;
    public decimal Score=>CalculateScore(NetAssets,Campaign.TotalRevenue,FindFirstObjectByType<ReputationManager>().Value,State.totalDelivered>0?(float)State.totalThreeStars/State.totalDelivered:0,State.totalDelivered>0?(float)State.totalOnTime/State.totalDelivered:0);
    public decimal CalculateScore(decimal assets,decimal revenue,float reputation,float stars,float onTime)=>BusinessEconomy.Money(assets+revenue*(decimal)revenueWeight+(decimal)reputation*(decimal)reputationWeight+(decimal)Mathf.Clamp01(stars)*(decimal)threeStarWeight+(decimal)Mathf.Clamp01(onTime)*(decimal)onTimeWeight);
    public string RankFor(decimal score)
    {
        string[] names={"S","A","B","C"};
        for(int i=0;i<Math.Min(4,rankThresholds.Length);i++)if(score>=(decimal)rankThresholds[i])return names[i];
        return "D";
    }
    public string ResultSummary=>$"{(Campaign.IsBankrupt?"破産":Loan==0?"ローン完済！ クリア":"返済できなかった…")}\n{Campaign.DayNumber}日目 / {GradeName}\n所持金 {UiKit.Yen(Economy.Cash)}　ローン {UiKit.Yen(Loan)}\n純資産 {UiKit.Yen(NetAssets)} / 累計売上 {UiKit.Yen(Campaign.TotalRevenue)}\n{(Campaign.IsBankrupt?"ランクなし":$"スコア {Score:F0} / ランク {RankFor(Score)}")}\n{ProfileSummary}";
    public void NewGame()
    {
        State=new RewardSaveData{mode=GameScenes.NewGameMode,normalDays=normalDays,runId=Guid.NewGuid().ToString("N"),loan=Money((decimal)initialLoan)};
        ResetTelemetry();
    }
    public void Restore(RewardSaveData data,int day)
    {
        State=data??new RewardSaveData{mode=GameMode.Endless,normalDays=normalDays,runId=Guid.NewGuid().ToString("N"),grade=Mathf.Clamp(day,1,4),loan="0",loanCelebrated=true};
        ResetTelemetry();
    }
    public void PrepareDay()
    {
        State.ended=false;State.interest=State.missionCash="0";State.settlementText="";
        State.missions=RewardMissions.Generate(FindFirstObjectByType<MapManager>().CityState?.seed??0,Campaign.DayNumber,State.grade,missionDefinitions.Where(d=>offerOptimalRouteChallenge || d.kind!=MissionKind.RouteSavings),State.previousMissions);
        ResetTelemetry();
    }
    public void SettleDay()
    {
        if(State.ended)return;
        State.ended=true;State.last=CaptureMetrics();State.recent.Add(State.last);while(State.recent.Count>recentDays)State.recent.RemoveAt(0);
        State.totalDelivered+=State.last.delivered;
        State.totalOnTime+=State.last.delivered-State.last.late;
        State.totalThreeStars+=State.last.threeStar;
        State.noLateStreak=State.last.delivered>0 && State.last.late==0 && State.last.cancelled==0?State.noLateStreak+1:0;
        decimal interest=InterestFor(Loan);State.loan=Money(Loan+interest);State.interest=Money(interest);Economy.ApplyGoalCash(-interest);
        AwardMissions();Promote();
        State.resultPending=State.mode==GameMode.Normal && Campaign.DayNumber>=State.normalDays && !State.resultSeen;
        if(Economy.Cash<0)Campaign.DeclareBankruptcy();
        CheckCustomGoal();TrackProfile();
        State.settlementText=$"{Campaign.DayNumber}日目 / 売上 {UiKit.Yen(Economy.DailyRevenue)} / 営業損益 {UiKit.Signed(Economy.Ledger.Profit)}\n利息 −{UiKit.Yen(interest)} / ミッション +{UiKit.Yen(CampaignSaveManager.Decimal(State.missionCash))}\n";
        State.ledger=CaptureLedger();
    }
    public void ContinueEndless()
    {
        if(!HasPendingResult || Campaign.IsBankrupt)return;
        RecordResult();State.resultSeen=true;State.resultPending=false;State.mode=GameMode.Endless;
        SaveCheckpoint();Campaign.NextDay();
    }
    public void RecordResult(){if(HasPendingResult)TrackProfile(true);SaveCheckpoint();}
    public void SaveCheckpoint()=>FindFirstObjectByType<CampaignSaveManager>().SavePreparation();
    public string HudSummary=>$"{GradeName} / ローン {UiKit.Yen(Loan)} / 目標 {State.missions.Count(m=>m.selected)}件"+(State.mode==GameMode.Normal?$" / {Campaign.DayNumber}/{State.normalDays}日":" / "+GoalLabel);
    public string SettlementSummary=>State.settlementText+$"ローン {UiKit.Yen(Loan)} / 純資産 {UiKit.Yen(NetAssets)}\n{promotionNotice}\n"+string.Join("\n",State.missions.Where(m=>m.selected).Select(m=>$"{(m.completed?"達成":"未達成")} {MissionLabel(m)}"))+(Economy.Cash<Economy.FixedCost(Campaign.DayNumber+1)+Economy.TeamWages?"\n警告：あしたの固定費・日給に所持金が足りません。":"");
    public static string Money(decimal n)=>n.ToString(CultureInfo.InvariantCulture);
    public static bool Valid(RewardSaveData d)
    {
        if(d==null)return true;
        try
        {
            return d.version==1 && Enum.IsDefined(typeof(GameMode),d.mode) && d.normalDays>0 && d.grade>=1 && d.grade<=4 && d.totalDelivered>=0 && d.totalOnTime>=0 && d.totalOnTime<=d.totalDelivered && d.totalThreeStars>=0 && d.totalThreeStars<=d.totalDelivered && d.points>=0 && !string.IsNullOrWhiteSpace(d.runId) && CampaignSaveManager.Decimal(d.loan)>=0 && CampaignSaveManager.Decimal(d.interest)>=0 && CampaignSaveManager.Decimal(d.missionCash)>=0 && d.missions!=null && d.missions.Count<=3 && d.missions.All(m=>m!=null && !string.IsNullOrWhiteSpace(m.id) && m.target>=0 && m.points>=0 && CampaignSaveManager.Decimal(m.cashReward)>=0) && d.missions.Select(m=>m.id).Distinct().Count()==d.missions.Count && d.recent!=null && d.recent.All(m=>m!=null && m.delivered>=0 && m.late>=0 && m.late<=m.delivered) && (!d.ended || d.last!=null && d.ledger!=null && d.ledger.Length==13 && d.ledger.All(v=>decimal.TryParse(v,NumberStyles.Number,CultureInfo.InvariantCulture,out _)));
        }
        catch(Exception e)when(e is FormatException || e is ArgumentException || e is OverflowException){return false;}
    }
    private string[] CaptureLedger()
    {
        var l=Economy.Ledger;
        return new[]{l.OpeningCash,l.PreparationCosts,l.FacilityPurchases,l.EquipmentMaintenance,l.QualityBonus,l.Revenue,l.Materials,l.Distance,l.Fuel,l.Wages,l.FixedCosts,l.Repairs,l.Waste}.Select(Money).ToArray();
    }
}
