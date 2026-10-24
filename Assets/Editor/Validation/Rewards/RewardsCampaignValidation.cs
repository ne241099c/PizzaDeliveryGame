using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class RewardsCampaignValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required");
        CampaignSaveManager.SessionSavingLocked=true;
        T F<T>()where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
        var campaign=F<CampaignManager>();var save=F<CampaignSaveManager>();using var guard=save.SuppressSaving();
        var goals=campaign.Rewards;var economy=F<BusinessEconomy>();var clock=F<GameClock>();var map=F<MapManager>();
        var funding=new SerializedObject(economy);float cash=funding.FindProperty("initialCash").floatValue;
        var config=new SerializedObject(goals);int days=config.FindProperty("normalDays").intValue;var mode=GameScenes.NewGameMode;
        bool spawn=F<OrderSpawner>().enabled;var originalSeed=map.SeedOverride;int n=0;
        void Check(bool b,string m){if(!b)throw new Exception("Rewards campaign: "+m);n++;}
        try
        {
            funding.FindProperty("initialCash").floatValue=50000;funding.ApplyModifiedPropertiesWithoutUndo();
            config.FindProperty("normalDays").intValue=1;config.ApplyModifiedPropertiesWithoutUndo();
            clock.AutomaticTick=false;F<OrderSpawner>().enabled=false;map.SeedOverride=104;
            GameScenes.NewGameMode=GameMode.Normal;campaign.NewGame();
            Check(goals.State.mode==GameMode.Normal && goals.State.normalDays==1,"chosen mode and configurable duration");
            Check(goals.Loan==(decimal)EconomyScale.InitialLoan && goals.InterestFor(1000)==5,"configured loan and daily 0.5 percent interest");
            Check(goals.NetAssets==economy.Cash-goals.Loan,"net assets separate from cash");
            Check(goals.TodayMissions.Count==3 && goals.TodayMissions.Select(m=>m.id).Distinct().Count()==3,"three distinct offers");
            var mission=goals.TodayMissions[0];Check(goals.TodayMissions.All(m=>m.selected),"every mission is active without choosing");
            var json=save.SnapshotJson();Check(save.TryParse(json,out var prep,out _),"preparation round trip validates");
            campaign.RestorePreparation(prep);Check(goals.TodayMissions.All(m=>m.selected) && goals.TodayMissions[0].id==mission.id,"missions persist");
            Check(!goals.HasPendingResult,"no result before final settlement");
            decimal beforeRepay=economy.Cash;
            Check(goals.CanRepay && goals.Repay(100) && goals.Loan==9900 && economy.Cash==beforeRepay-100,"partial repayment in preparation is exact");
            Check(save.TryParse(save.SnapshotJson(),out var repaidPrep,out var repaidReason) && CampaignSaveManager.Decimal(repaidPrep.cash)==economy.Cash && CampaignSaveManager.Decimal(repaidPrep.rewards.loan)==9900,"the preparation save after repaying still loads: "+repaidReason);
            Check(goals.Borrowed==10000 && goals.Repaid==100 && Mathf.Approximately(goals.LoanProgress,.01f) && goals.DaysLeft==1,"loan progress for the 開店準備 gauge");
            campaign.StartBusiness();Check(goals.TodayMissions.All(m=>m.selected),"missions stay active after opening");
            Check(!goals.CanRepay && !goals.Repay(10) && goals.Loan==9900,"no repayment during business");
            F<DayStats>().RecordDelivery(100,0);F<DayStats>().RecordQuality(3,true);economy.SendMessage("CreditArrival",1000);
            clock.Advance(clock.BusinessDuration+1);
            Check(clock.Phase==DayPhase.Ended && economy.Ledger.Settled,"existing settlement finishes first");
            Check(goals.HasPendingResult && !campaign.IsBankrupt,"normal duration produces result");
            Check(goals.Loan==9949.5m,"interest on the remaining loan applied once at settlement");
            decimal loan=goals.Loan,after=economy.Cash;goals.SettleDay();Check(goals.Loan==loan && economy.Cash==after,"settlement idempotent");
            int day=campaign.DayNumber;campaign.NextDay();Check(campaign.DayNumber==day,"pending result blocks next day");
            Check(!goals.Repay(100) && goals.Loan==loan && economy.Cash==after,"no repayment after closing (preparation only)");
            Check(save.TryParse(save.SnapshotJson(),out var checkpoint,out var reason),"settled checkpoint validates: "+reason);
            decimal settled=economy.Cash;campaign.RestorePreparation(checkpoint);
            Check(clock.Phase==DayPhase.Ended && goals.Loan==loan && economy.Cash==settled,"restore settlement cash and loan");
            Check(goals.HasPendingResult && economy.Ledger.Revenue==1000,"restore final result and ledger");
            Check(F<DayStats>().StarCounts[3]==1 && F<DayStats>().ThreeStarOnTime==1,"restored settlement preserves quality totals");
            goals.RecordResult();int ranks=goals.Profile.rankHistory.Count;goals.RecordResult();Check(goals.Profile.rankHistory.Count==ranks && ranks>0,"result history not duplicated");
            goals.ContinueEndless();Check(goals.State.mode==GameMode.Endless && campaign.DayNumber==2 && clock.Phase==DayPhase.BeforeOpening,"continue normal into endless without reset");
            Check(goals.Loan==loan,"continuation retains finances");
            decimal beforePayoff=economy.Cash;
            Check(!goals.Repay(-1) && !goals.Repay(loan+1),"invalid repayment rejected");
            Check(goals.Repay(goals.Loan) && goals.Loan==0 && economy.Cash==beforePayoff-loan+goals.PayoffBonus && goals.LoanProgress==1,"payoff in preparation and one-time bonus");
            Check(!goals.Repay(1),"paid loan cannot pay again");
            Check(CampaignSaveManager.Decimal(goals.Profile.bestScore)>=goals.Score,"payoff bonus updates best score");
            Check(goals.Profile.achievementIds.Contains("paidLoan"),"payoff awards persistent achievement");
            settled=economy.Cash;
            goals.SetEndlessGoal("streak");Check(goals.State.goalId=="streak","endless target selection");
            var grade=new RewardGradeDefinition{reputation=40,revenue=1000,onTimeRate=.8f};
            Check(goals.Qualifies(grade,40,1000,0,.8f,0),"promotion thresholds inclusive");
            Check(!goals.Qualifies(grade,39,1000,0,.8f,0) && !goals.Qualifies(grade,40,999,0,.8f,0) && !goals.Qualifies(grade,40,1000,0,.79f,0),"promotion rejects each unmet threshold");
            Check(goals.RankFor(37500)=="S" && goals.RankFor(25000)=="A" && goals.RankFor(15000)=="B" && goals.RankFor(5000)=="C" && goals.RankFor(4999)=="D","rank boundaries");
            Check(goals.CalculateScore(100,1000,50,1,1)==2700,"score separate from operating score");
            var old=save.Capture();old.rewards=null;string oldJson=JsonUtility.ToJson(old);oldJson="{"+oldJson.Substring(oldJson.IndexOf("\"facilities\":",StringComparison.Ordinal));Check(save.TryParse(oldJson,out var legacyParsed,out _) && legacyParsed.rewards==null,"old optional reward field migrates");campaign.RestorePreparation(old);Check(goals.State.mode==GameMode.Endless && goals.Loan==0 && economy.Cash==settled,"legacy save keeps funds and no surprise debt");
            GameScenes.NewGameMode=GameMode.Endless;campaign.NewGame();campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1);
            Check(!goals.HasPendingResult,"endless has no normal ending");
            campaign.NextDay();Check(campaign.DayNumber==2,"endless can advance");
            goals.State.grade=3;campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1);campaign.NextDay();
            Check(map.City==null || map.City.blocks.Count>=12,"grade unlock expands actual town");
            Check(F<FacilityManager>().IsUnlocked(PizzaKind.Seafood),"grade unlocks actual pizza kind");
            Check(F<FacilityManager>().UnlockedMenuCount==3 && F<FacilityManager>().Rejection(FacilityType.Menu).Length>0,"grade menu is shown without duplicate purchases");
            Check(save.TryParse(save.SnapshotJson(),out _,out reason),"expanded town save validates: "+reason);
            var invalid=save.Capture();invalid.rewards.loan="-1";Check(!save.TryParse(JsonUtility.ToJson(invalid),out _,out _),"invalid loan rejected");
            campaign.NewGame();economy.ApplyGoalCash(-economy.Cash+1);campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1);
            Check(campaign.IsBankrupt && economy.Cash<0,"negative settled cash bankrupts without grace");
            int bankruptDay=campaign.DayNumber;campaign.NextDay();Check(campaign.DayNumber==bankruptDay,"bankrupt cannot advance");
            return $"PASS: {n} reward campaign checks.";
        }
        finally
        {
            funding.FindProperty("initialCash").floatValue=cash;funding.ApplyModifiedPropertiesWithoutUndo();config.FindProperty("normalDays").intValue=days;config.ApplyModifiedPropertiesWithoutUndo();GameScenes.NewGameMode=mode;map.SeedOverride=originalSeed;
            campaign.NewGame();clock.AutomaticTick=false;F<OrderSpawner>().enabled=spawn;
        }
    }
}
