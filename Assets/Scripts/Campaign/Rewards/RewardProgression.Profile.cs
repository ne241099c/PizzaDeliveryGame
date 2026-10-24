using System;
using System.IO;
using System.Linq;
using UnityEngine;

public sealed partial class RewardProgression
{
    [SerializeField] private AchievementDefinition[] achievementDefinitions=RewardProfile.DefaultDefinitions();
    private ProfileData profile;
    private string loadedProfilePath;
    private string profileWarning="",achievementNotice="";
    private string ProfilePath=>Path.Combine(Path.GetDirectoryName(FindFirstObjectByType<CampaignSaveManager>().SavePath),"profile.json");
    public ProfileData Profile{get
    {
        var path=ProfilePath;
        if(profile==null || loadedProfilePath!=path)
        {
            profile=RewardProfile.Load(path,out profileWarning);
            loadedProfilePath=path;
        }
        return profile;
    }}
    public bool HasNewAchievements=>!string.IsNullOrEmpty(achievementNotice);
    public string ProfileSummary=>achievementNotice+"\n"+profileWarning+"\n"+RewardProfile.ReadSummary(Profile,achievementDefinitions);
    private ProfileObservation Observation(bool result=false)
    {
        var m=State.last??new RewardDayMetrics();
        return new ProfileObservation{runId=State.runId,day=Campaign.DayNumber,grade=State.grade,reputation=FindFirstObjectByType<ReputationManager>().Value,cash=Economy.Cash,loan=Loan,score=Score,delivered=m.delivered,late=m.late+m.cancelled,maxBatch=m.maxBatch,backstreets=m.backstreets,failedPizzas=m.failedPizzas,loanPaid=State.loanCelebrated,customGoalMet=State.goalCompleted,missionsCompleted=State.missions.Count(x=>x.rewarded),missionCount=State.missions.Count,rank=null,madePizzas=m.madePizzas,threeStarPizzas=m.threeStarPizzas,districts=m.visitedDistricts,shortcuts=m.shortcuts};
    }
    private void TrackProfile(bool result=false)
    {
        var p=Profile;bool fresh=!p.observedDays.Contains(State.runId+":"+Campaign.DayNumber);
        var unlocked=RewardProfile.Track(p,Observation(result),achievementDefinitions);
        if(fresh)p.achievementPoints+=State.missions.Where(m=>m.rewarded).Sum(m=>m.points);
        if(result && !State.resultRecorded && !Campaign.IsBankrupt)
        {
            p.rankHistory.Add(RankFor(Score));State.resultRecorded=true;
        }
        Notice(unlocked);WriteProfile();
    }
    private void EvaluateProfileAchievements()
    {
        Notice(RewardProfile.EvaluateAchievements(Profile,Observation(),achievementDefinitions));
        WriteProfile();
    }
    private void Notice(System.Collections.Generic.List<string> ids)
    {
        if(ids.Count>0)achievementNotice="トロフィー獲得："+string.Join("・",ids.Select(id=>achievementDefinitions.FirstOrDefault(d=>d.id==id)?.title??id));
    }
    private void WriteProfile()
    {
        var p=Profile;
        p.bestCash=Money(Math.Max(CampaignSaveManager.Decimal(p.bestCash),Economy.Cash));
        p.bestNetAssets=Money(Math.Max(CampaignSaveManager.Decimal(p.bestNetAssets),NetAssets));
        p.bestScore=Money(Math.Max(CampaignSaveManager.Decimal(p.bestScore),Score));
        var save=FindFirstObjectByType<CampaignSaveManager>();
        RewardProfile.Save(ProfilePath,Profile,save.AutomaticSavingEnabled && !CampaignSaveManager.SessionSavingLocked);
    }
}
