using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

/// <summary>Persistent history, separate from an individual campaign. Money uses invariant decimal strings.</summary>
[Serializable]
public sealed class ProfileData
{
    public int version=1;
    public List<string> achievementIds=new();
    public List<string> pizzas=new(),threeStarPizzas=new(),districts=new(),shortcuts=new();
    public int bestDays,bestGrade,achievementPoints,noFailureDays,backstreetUses,missionsCompleted;
    public float bestReputation;
    public string bestCash="0",bestNetAssets="0",bestScore="0";
    public List<string> rankHistory=new(),observedDays=new();
    [NonSerialized] public bool WriteBlocked;
}

public enum AchievementMetric { NoLateDay, MaxBatch, Backstreets, PaidLoan, NoFailureDays, CustomGoal, MissionsCompleted, AllMissionsDay }
public enum AchievementComparison { AtLeast, AtMost, Equal }
[Serializable]
public sealed class AchievementDefinition
{
    public string id,title;
    public AchievementMetric metric;
    public AchievementComparison comparison=AchievementComparison.AtLeast;
    public float threshold;
    public int points;
}

/// <summary>One finalized settlement per run/day. runId must persist in the campaign save.</summary>
public sealed class ProfileObservation
{
    public string runId,rank;
    public int day,grade,delivered,late,maxBatch,backstreets,failedPizzas,missionsCompleted,missionCount;
    public float reputation;
    public decimal cash,loan,score;
    public bool loanPaid,customGoalMet;
    public string[] madePizzas,threeStarPizzas,districts,shortcuts;
}

public static class RewardProfile
{
    public const int FormatVersion=1;
    private static readonly CultureInfo Invariant=CultureInfo.InvariantCulture;
    private static readonly HashSet<string> BlockedPaths=new(StringComparer.Ordinal);
    public static string DefaultPath=>Path.Combine(CampaignSaveFile.Directory,"profile.json");

    /// <summary>Examples are editable data. Gameplay may supply its own serialized definitions.</summary>
    public static AchievementDefinition[] DefaultDefinitions()=>new[]
    {
        // Trophies for what the player achieved (missions and milestones), not for things found on the map.
        Definition("firstMission","はじめてのミッション達成",AchievementMetric.MissionsCompleted,1),
        Definition("missions10","ミッション10回達成",AchievementMetric.MissionsCompleted,10),
        Definition("missions30","ミッション30回達成",AchievementMetric.MissionsCompleted,30),
        Definition("missions60","ミッション60回達成",AchievementMetric.MissionsCompleted,60),
        Definition("perfectDay","ミッション全達成の日",AchievementMetric.AllMissionsDay,1),
        Definition("noLateDay","遅れゼロの日",AchievementMetric.NoLateDay,1),
        Definition("batch3","まとめ名人",AchievementMetric.MaxBatch,3),
        Definition("noBurn10days","焦げなし10日",AchievementMetric.NoFailureDays,10),
        Definition("paidLoan","ローン完済",AchievementMetric.PaidLoan,1),
        Definition("customGoal","自分の目標を達成",AchievementMetric.CustomGoal,1)
    };
    private static AchievementDefinition Definition(string id,string title,AchievementMetric metric,float threshold)
        =>new(){id=id,title=title,metric=metric,threshold=threshold};

    public static ProfileData Load(string path,out string warning)
    {
        warning="";
        string full=Path.GetFullPath(path),backup=full+".bak";
        if(!File.Exists(full) && !File.Exists(backup))return new ProfileData();
        if(TryRead(full,out var data,out var problem)){BlockedPaths.Remove(full);return data;}
        if(TryRead(backup,out data,out _))
        {
            warning="記録の本体を読めないため、バックアップから読み込みました。元のファイルは保存時まで保持します。";
            BlockedPaths.Remove(full);return data;
        }
        BlockedPaths.Add(full);
        warning="記録とバックアップを読めません。元のファイルを保護し、記録の保存を停止しています。（"+problem+"）";
        return new ProfileData{WriteBlocked=true};
    }

    /// <summary>No write occurs during a fixture or a locked game session. Invalid history is never replaced.</summary>
    public static bool Save(string path,ProfileData data,bool allowWrite)
    {
        if(!allowWrite || CampaignSaveManager.SessionSavingLocked || data==null || data.WriteBlocked)return false;
        string full=Path.GetFullPath(path),backup=full+".bak",temp=full+".tmp";
        if(BlockedPaths.Contains(full) || !Validate(data,out _))return false;
        // Even a caller that omitted Load must not overwrite unreadable history.
        bool validPrimary=TryRead(full,out _,out _);
        bool validBackup=TryRead(backup,out _,out _);
        if((File.Exists(full) || File.Exists(backup)) && !validPrimary && !validBackup)
        {BlockedPaths.Add(full);data.WriteBlocked=true;return false;}
        try
        {
            string directory=Path.GetDirectoryName(full);
            if(!string.IsNullOrEmpty(directory))Directory.CreateDirectory(directory);
            using(var stream=new FileStream(temp,FileMode.Create,FileAccess.Write,FileShare.None))
            {
                byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(data,true));
                stream.Write(bytes,0,bytes.Length);stream.Flush(true);
            }
            // Keep the valid recovery copy if the original primary was corrupt.
            if(File.Exists(full))File.Replace(temp,full,validPrimary?backup:null);
            else File.Move(temp,full);
            return true;
        }
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException)
        {return false;}
        finally{try{if(File.Exists(temp))File.Delete(temp);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }

    private static bool TryRead(string path,out ProfileData data,out string reason)
    {
        data=null;reason="ファイルがありません";
        if(!File.Exists(path))return false;
        try
        {
            string json=File.ReadAllText(path);
            // JsonUtility supplies field defaults for omitted values; require a real version marker.
            if(string.IsNullOrWhiteSpace(json) || !json.Contains("\"version\""))throw new FormatException("記録の形式が不明です");
            data=JsonUtility.FromJson<ProfileData>(json);
            if(!Validate(data,out reason)){data=null;return false;}
            return true;
        }
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException)
        {reason=e.Message;data=null;return false;}
    }

    public static bool Validate(ProfileData data,out string reason)
    {
        reason="";
        if(data==null || data.version!=FormatVersion){reason="未対応の記録バージョン";return false;}
        if(data.bestDays<0 || data.bestGrade<0 || data.achievementPoints<0 || data.noFailureDays<0 || data.backstreetUses<0 || data.missionsCompleted<0 ||
            float.IsNaN(data.bestReputation) || float.IsInfinity(data.bestReputation) || data.bestReputation<0)
        {reason="記録の数値が不正";return false;}
        if(!Money(data.bestCash,out _) || !Money(data.bestNetAssets,out _) || !Money(data.bestScore,out _))
        {reason="記録の金額が不正";return false;}
        foreach(var list in new[]{data.achievementIds,data.pizzas,data.threeStarPizzas,data.districts,data.shortcuts,data.observedDays})
            if(list==null || list.Any(string.IsNullOrWhiteSpace) || list.Distinct(StringComparer.Ordinal).Count()!=list.Count)
            {reason="図鑑または称号の重複・欠落";return false;}
        if(data.rankHistory==null || data.rankHistory.Any(rank=>!ValidRank(rank)))
        {reason="ランクの形式が不正";return false;}
        return true;
    }

    public static List<string> Track(ProfileData profile,ProfileObservation observation,IEnumerable<AchievementDefinition> definitions)
    {
        var earned=new List<string>();
        if(profile==null || observation==null || profile.WriteBlocked || !Validate(profile,out _) ||
            string.IsNullOrWhiteSpace(observation.runId) || observation.day<1 || observation.grade<1 ||
            observation.delivered<0 || observation.late<0 || observation.maxBatch<0 || observation.backstreets<0 || observation.failedPizzas<0 ||
            float.IsNaN(observation.reputation) || float.IsInfinity(observation.reputation) || observation.reputation<0)return earned;
        string key=observation.runId+":"+observation.day.ToString(Invariant);
        if(profile.observedDays.Contains(key))return earned;
        bool first=profile.observedDays.Count==0;
        bool followsPreviousDay=profile.observedDays.Contains(observation.runId+":"+(observation.day-1).ToString(Invariant));
        profile.observedDays.Add(key);
        profile.bestDays=Math.Max(profile.bestDays,observation.day);
        profile.bestGrade=Math.Max(profile.bestGrade,observation.grade);
        profile.bestReputation=Math.Max(profile.bestReputation,observation.reputation);
        profile.bestCash=first?observation.cash.ToString(Invariant):Maximum(profile.bestCash,observation.cash);
        profile.bestNetAssets=first?(observation.cash-observation.loan).ToString(Invariant):Maximum(profile.bestNetAssets,observation.cash-observation.loan);
        profile.bestScore=first?observation.score.ToString(Invariant):Maximum(profile.bestScore,observation.score);
        profile.noFailureDays=observation.failedPizzas==0 && (observation.delivered>0 || observation.madePizzas!=null && observation.madePizzas.Length>0)?(followsPreviousDay?AddCounter(profile.noFailureDays,1):1):0;
        profile.backstreetUses=AddCounter(profile.backstreetUses,observation.backstreets);
        profile.missionsCompleted=AddCounter(profile.missionsCompleted,Math.Max(0,observation.missionsCompleted));
        Add(profile.pizzas,observation.madePizzas);Add(profile.threeStarPizzas,observation.threeStarPizzas);
        Add(profile.districts,observation.districts);Add(profile.shortcuts,observation.shortcuts);
        if(!string.IsNullOrEmpty(observation.rank) && ValidRank(observation.rank))profile.rankHistory.Add(observation.rank);
        return EvaluateAchievements(profile,observation,definitions);
    }

    /// <summary>Allows a post-settlement repayment or custom goal notification without recounting the day.</summary>
    public static List<string> EvaluateAchievements(ProfileData profile,ProfileObservation observation,IEnumerable<AchievementDefinition> definitions)
    {
        var earned=new List<string>();
        if(profile==null || observation==null || profile.WriteBlocked || definitions==null || !Validate(profile,out _))return earned;
        foreach(var definition in definitions)
        {
            if(definition==null || string.IsNullOrWhiteSpace(definition.id) || profile.achievementIds.Contains(definition.id) ||
                float.IsNaN(definition.threshold) || float.IsInfinity(definition.threshold))continue;
            float value=definition.metric switch
            {
                AchievementMetric.NoLateDay=>observation.delivered>0 && observation.late==0?1:0,
                AchievementMetric.MaxBatch=>observation.maxBatch,
                AchievementMetric.Backstreets=>profile.backstreetUses,
                AchievementMetric.PaidLoan=>observation.loanPaid?1:0,
                AchievementMetric.NoFailureDays=>profile.noFailureDays,
                AchievementMetric.CustomGoal=>observation.customGoalMet?1:0,
                AchievementMetric.MissionsCompleted=>profile.missionsCompleted,
                AchievementMetric.AllMissionsDay=>observation.missionCount>0 && observation.missionsCompleted>=observation.missionCount?1:0,
                _=>float.NaN
            };
            bool passed=definition.comparison switch
            {
                AchievementComparison.AtLeast=>value>=definition.threshold,
                AchievementComparison.AtMost=>value<=definition.threshold,
                AchievementComparison.Equal=>value==definition.threshold,
                _=>false
            };
            if(!passed)continue;
            profile.achievementIds.Add(definition.id);earned.Add(definition.id);
            profile.achievementPoints=AddCounter(profile.achievementPoints,Math.Max(0,definition.points));
        }
        return earned;
    }

    public static string ReadSummary(ProfileData data,IEnumerable<AchievementDefinition> definitions)
    {
        if(data==null)return "まだ記録がありません";
        var names=new Dictionary<string,string>(StringComparer.Ordinal);
        if(definitions!=null)foreach(var definition in definitions)
            if(definition!=null && !string.IsNullOrEmpty(definition.id))names[definition.id]=definition.title;
        var text=new StringBuilder();
        text.AppendLine($"最高記録：{data.bestDays}日目 / グレード {data.bestGrade} / 評判 {data.bestReputation:F0}");
        text.AppendLine($"所持金 ¥{data.bestCash} / 純資産 ¥{data.bestNetAssets} / スコア {data.bestScore}");
        text.AppendLine("ランク："+string.Join("・",data.rankHistory));
        text.AppendLine($"達成したミッション：{data.missionsCompleted}回");
        text.AppendLine($"トロフィー（{data.achievementIds.Count(names.ContainsKey)}/{names.Count}）");
        foreach(var pair in names)text.AppendLine((data.achievementIds.Contains(pair.Key)?"★ ":"☆ ")+pair.Value);
        text.Append("実績ポイント："+data.achievementPoints);
        return text.ToString();
    }
    private static int AddCounter(int left,int right)=>(int)Math.Min(int.MaxValue,(long)left+right);
    private static bool Money(string value,out decimal amount)=>decimal.TryParse(value,NumberStyles.Number,Invariant,out amount);
    private static string Maximum(string previous,decimal value)=>Math.Max(decimal.Parse(previous,Invariant),value).ToString(Invariant);
    private static bool ValidRank(string rank)=>rank=="S" || rank=="A" || rank=="B" || rank=="C" || rank=="D";
    private static void Add(List<string> target,IEnumerable<string> values)
    {
        if(values==null)return;
        foreach(string value in values)if(!string.IsNullOrWhiteSpace(value) && !target.Contains(value))target.Add(value);
    }
}
