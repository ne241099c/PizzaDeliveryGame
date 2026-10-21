using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static partial class M6Validation
{
    private static void ValidateSaveGame()
    {
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var training=UnityEngine.Object.FindFirstObjectByType<TrainingManager>();var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var progression=UnityEngine.Object.FindFirstObjectByType<CourierProgression>();
        string original=save.SavePathOverride;bool automatic=save.AutomaticSavingEnabled;
        string actualPath=save.SavePath;byte[] actualBefore=File.Exists(actualPath)?File.ReadAllBytes(actualPath):null;
        string temp=Path.Combine(Path.GetTempPath(),"pizza-m6-save-validation-"+Guid.NewGuid().ToString("N")+".json");
        try
        {
            save.SavePathOverride=temp;save.AutomaticSavingEnabled=true;save.StartNewGame();
            Check(File.Exists(temp),"preparation entry creates save");
            var funding=new SerializedObject(economy);funding.FindProperty("initialCash").floatValue=10000;funding.ApplyModifiedPropertiesWithoutUndo();economy.NewGame();funding.FindProperty("initialCash").floatValue=300;funding.ApplyModifiedPropertiesWithoutUndo();
            spawner.SetPromiseSeconds(45);var person=roster.People[0];
            progression.AwardExperience(person,CourierAbility.Driving,200);person.SetExperience(CourierAbility.Service,123);
            Check(training.Request(person,CourierAbility.LocalKnowledge),"pending training before save");save.SavePreparation();
            string snapshot=File.ReadAllText(temp);Check(save.TryParse(snapshot,out var data,out _),"saved snapshot validates");
            Check(data.training.Count==1 && data.people[0].Status==DeliveryStatus.Training,"training instructions and status saved");
            Check(data.applicants.Count>=2 && data.applicants[0].strengths.Length==2,"daily applicants and traits saved");
            save.StartNewGame();File.WriteAllText(temp,snapshot);save.Startup();
            Check(save.AwaitingChoice && File.ReadAllText(temp)==snapshot,"startup offers continue without overwriting save");
            save.Continue();Check(!save.AwaitingChoice && save.SnapshotJson()==snapshot,"JSON load round trip preserves all campaign data");
            Near(roster.People[0].Driving,.62f,"loaded grown ability");Near(roster.People[0].Experience(CourierAbility.Service),123,"loaded XP remainder");
            Check(roster.People[0].Status==DeliveryStatus.Training && training.Assignments.Count==1,"loaded training blocks dispatch");
            var recruiting=UnityEngine.Object.FindFirstObjectByType<RecruitmentManager>();var loadedApplicant=recruiting.Applicants[0];
            Check(recruiting.Hire(loadedApplicant) && loadedApplicant.Person.CurrentStamina==1,"hiring restored applicant starts with full configured stamina");
            campaign.StartBusiness();string beforeBusiness=File.ReadAllText(temp);clock.Advance(1);save.SavePreparation();
            Check(File.ReadAllText(temp)==beforeBusiness,"business never overwrites preparation save");
            clock.Advance(clock.BusinessDuration+1000);campaign.NextDay();
            Check(save.TryParse(File.ReadAllText(temp),out var next,out _) && next.day==2 && next.training.Count==0,"next preparation autosaves next day and clears training");
            Check(next.totalRevenue==campaign.TotalRevenue.ToString(System.Globalization.CultureInfo.InvariantCulture),"cumulative records saved");
            Check(!save.TryParse("{not-json",out _,out _),"malformed JSON rejected");
            var wrong=save.Capture();wrong.version=999;Check(!save.TryParse(JsonUtility.ToJson(wrong),out _,out var reason) && reason.Contains("version"),"unsupported save version rejected");
            File.WriteAllText(temp,JsonUtility.ToJson(wrong));save.Continue();
            Check(campaign.DayNumber==1 && economy.Cash==300 && save.LastMessage.Contains("version"),"unsupported version starts new game with visible reason");
            File.WriteAllText(temp,"{not-json");save.Continue();Check(campaign.DayNumber==1 && save.LastMessage.Contains("Cannot load"),"corrupt file falls back safely");
            save.StartNewGame();campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1000);
            Check(campaign.IsBankrupt && !File.Exists(temp),"bankruptcy deletes save");
            save.StartNewGame();Check(campaign.DayNumber==1 && economy.Cash==300 && roster.People.Count==2,"start new resets campaign");
        }
        finally
        {
            if(File.Exists(temp))File.Delete(temp);if(File.Exists(temp+".tmp"))File.Delete(temp+".tmp");
            save.AutomaticSavingEnabled=false;save.SavePathOverride=original;campaign.NewGame();save.AutomaticSavingEnabled=automatic;save.ClearNotice();
        }
        byte[] actualAfter=File.Exists(actualPath)?File.ReadAllBytes(actualPath):null;
        Check(actualBefore==null?actualAfter==null:actualAfter!=null && actualBefore.SequenceEqual(actualAfter),"validation leaves normal player save untouched");
    }
}
