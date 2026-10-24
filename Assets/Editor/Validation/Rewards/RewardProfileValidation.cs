using System;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>Pure profile checks in an isolated temporary directory. Does not touch campaign or real profile files.</summary>
public static class RewardProfileValidation
{
    public static string Run()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Profile file checks run in Edit mode only.");
        string folder=Path.Combine(Path.GetTempPath(),"pizza-profile-"+Guid.NewGuid().ToString("N"));
        string path=Path.Combine(folder,"profile.json");
        Directory.CreateDirectory(folder);
        bool previousLock=CampaignSaveManager.SessionSavingLocked;
        CampaignSaveManager.SessionSavingLocked=false;
        int count=0;
        void Check(bool success,string message){if(!success)throw new Exception("Profile: "+message);count++;}
        try
        {
            var profile=RewardProfile.Load(path,out var warning);
            Check(warning=="" && profile.bestDays==0 && !File.Exists(path),"new profile read has no write side effect");
            var definitions=RewardProfile.DefaultDefinitions();
            var observation=new ProfileObservation{runId="runA",day=1,grade=2,reputation=60,cash=100,loan=200,score=-10,
                delivered=4,late=0,maxBatch=3,backstreets=10,failedPizzas=0,rank="B",missionsCompleted=3,missionCount=3,
                madePizzas=new[]{"Margherita","Margherita","Pepperoni"},threeStarPizzas=new[]{"Margherita"},
                districts=new[]{"district-a"},shortcuts=new[]{"shortcut-a"}};
            var earned=RewardProfile.Track(profile,observation,definitions);
            Check(earned.Count==4 && earned.Contains("batch3") && earned.Contains("noLateDay") && earned.Contains("firstMission") && earned.Contains("perfectDay") && profile.missionsCompleted==3,"achieved missions and milestones award trophies");
            Check(!definitions.Any(d=>d.metric==AchievementMetric.Backstreets),"no trophy for merely finding backstreets");
            Check(profile.bestNetAssets=="-100" && profile.bestScore=="-10","negative first records retain actual values");
            Check(profile.pizzas.Count==2 && profile.threeStarPizzas.Count==1 && profile.districts.Count==1 && profile.shortcuts.Count==1,"encyclopedia unique discovery");
            Check(RewardProfile.Track(profile,observation,definitions).Count==0 && profile.noFailureDays==1 && profile.backstreetUses==10 && profile.rankHistory.Count==1,"same run/day is idempotent");
            observation.loanPaid=true;
            Check(RewardProfile.EvaluateAchievements(profile,observation,definitions).SequenceEqual(new[]{"paidLoan"}),"post-settlement repayment unlock");
            Check(RewardProfile.EvaluateAchievements(profile,observation,definitions).Count==0,"repayment achievement unique");
            for(int day=2;day<=10;day++){observation.day=day;observation.rank=null;RewardProfile.Track(profile,observation,definitions);}
            Check(profile.noFailureDays==10 && profile.achievementIds.Contains("noBurn10days"),"ten consecutive failure-free days");
            observation.day=11;observation.failedPizzas=1;RewardProfile.Track(profile,observation,definitions);
            Check(profile.noFailureDays==0,"failure resets consecutive days");
            observation.runId="runB";observation.day=1;observation.failedPizzas=0;RewardProfile.Track(profile,observation,definitions);
            Check(profile.noFailureDays==1 && profile.bestDays==11 && profile.achievementIds.Contains("noBurn10days"),"new game retains history and starts its own streak");
            var custom=new AchievementDefinition{id="custom-data",metric=AchievementMetric.MaxBatch,comparison=AchievementComparison.Equal,threshold=3,points=7};
            Check(RewardProfile.EvaluateAchievements(profile,observation,new[]{custom}).Count==1 && profile.achievementPoints==7,"data comparison and points");
            Check(!RewardProfile.Save(path,profile,false) && !File.Exists(path),"caller suppression blocks write");
            CampaignSaveManager.SessionSavingLocked=true;
            Check(!RewardProfile.Save(path,profile,true) && !File.Exists(path),"session suppression blocks write");
            CampaignSaveManager.SessionSavingLocked=false;
            Check(RewardProfile.Save(path,profile,true),"first atomic save");
            profile.bestDays=20;Check(RewardProfile.Save(path,profile,true) && File.Exists(path+".bak"),"second save retains backup");
            var loaded=RewardProfile.Load(path,out warning);
            Check(warning=="" && loaded.bestDays==20 && loaded.achievementIds.Count==profile.achievementIds.Count,"round trip history");
            Check(RewardProfile.ReadSummary(loaded,definitions).Contains("まとめ名人"),"Japanese records summary");
            File.WriteAllText(path,"broken");
            loaded=RewardProfile.Load(path,out warning);
            Check(warning!="" && !loaded.WriteBlocked && loaded.bestDays==11 && File.ReadAllText(path)=="broken","backup recovery without replacing primary on load");
            Check(RewardProfile.Save(path,loaded,true),"recovered history can be saved");
            File.WriteAllText(path,"{\"version\":999}");File.WriteAllText(path+".bak","bad backup");
            loaded=RewardProfile.Load(path,out warning);
            Check(loaded.WriteBlocked && warning!="","invalid primary and backup block overwrite");
            Check(!RewardProfile.Save(path,new ProfileData(),true) && File.ReadAllText(path)=="{\"version\":999}" && File.ReadAllText(path+".bak")=="bad backup","fresh object cannot erase damaged history");
            var invalid=new ProfileData();invalid.pizzas.Add("duplicate");invalid.pizzas.Add("duplicate");
            Check(!RewardProfile.Validate(invalid,out _),"duplicate encyclopedia rejected");
            invalid=new ProfileData{bestCash="NaN"};Check(!RewardProfile.Validate(invalid,out _),"invalid money rejected");
            invalid=new ProfileData{bestReputation=float.PositiveInfinity};Check(!RewardProfile.Validate(invalid,out _),"infinite reputation rejected");
            invalid=new ProfileData();invalid.rankHistory.Add("X");Check(!RewardProfile.Validate(invalid,out _),"invalid rank rejected");
            return "PASS "+count+" profile checks (temporary files only)";
        }
        finally
        {
            CampaignSaveManager.SessionSavingLocked=previousLock;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
        }
    }
}
