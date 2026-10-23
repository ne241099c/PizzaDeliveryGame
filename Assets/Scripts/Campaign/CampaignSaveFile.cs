using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>Where the save and best records live, and a light read for the title scene. The full validation (map,
/// facilities, people) needs the game's managers and runs in the Game scene (CampaignSaveManager.TryParse).</summary>
public static class CampaignSaveFile
{
    public const string FileName="campaign.json";
    public const int FormatVersion=3;
    /// <summary>The data folder under the fixed GameInfo.SaveFolder rather than productName, so renaming the game keeps the saves.
    /// On desktop that is the sibling of persistentDataPath; elsewhere the platform folder itself.</summary>
    public static string Directory
    {
        get
        {
            string data=Application.persistentDataPath;
            #if UNITY_EDITOR || UNITY_STANDALONE
            string company=Path.GetDirectoryName(data);
            if(!string.IsNullOrEmpty(company))return Path.Combine(company,GameInfo.SaveFolder);
            #endif
            return data;
        }
    }
    public static string DefaultPath=>Path.Combine(Directory,FileName);
    public static string RecordsPath(string savePath)=>Path.Combine(Path.GetDirectoryName(savePath),"records.json");
    public static decimal Decimal(string value)=>decimal.Parse(value,NumberStyles.Number,CultureInfo.InvariantCulture);

    /// <summary>Reads the file and checks what the title needs: a known version, a day, cash and at least one courier.</summary>
    public static bool TryReadSummary(string path,out CampaignSaveData data,out string reason)
    {
        data=null;reason="";
        try
        {
            data=JsonUtility.FromJson<CampaignSaveData>(File.ReadAllText(path));
            if(data==null || data.version<1 || data.version>FormatVersion)throw new FormatException("Unsupported save version.");
            if(data.day<1 || data.people==null || data.people.Count<1)throw new FormatException("Missing campaign candidate.");
            _=Decimal(data.cash);
            return true;
        }
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException || e is FormatException || e is OverflowException)
        {data=null;reason=e.Message;return false;}
    }
    /// <summary>"3日目 ・ ¥1,240 ・ 評判 72" for the continue button.</summary>
    public static string Summary(CampaignSaveData data)=>$"{data.day}日目 ・ {UiKit.Yen(Decimal(data.cash))} ・ 評判 {data.reputation:F0}";
    public static string Problem(string reason)=>"セーブを読めませんでした（"+reason+"）。元のセーブは変更していません。「最初から」を選ぶと新しく始めます。";

    public static BestRecords ReadRecords(string savePath)
    {
        string path=RecordsPath(savePath);
        try{var records=File.Exists(path)?JsonUtility.FromJson<BestRecords>(File.ReadAllText(path)):null;return NormalizeRecords(records);}
        catch(Exception e)when(e is IOException || e is UnauthorizedAccessException || e is ArgumentException){return new BestRecords();}
    }
    private static BestRecords NormalizeRecords(BestRecords records)
    {
        records??=new BestRecords();
        records.bestDay=Math.Max(0,records.bestDay);
        records.bestDelivered=Math.Max(0,records.bestDelivered);
        records.bestDailyRevenue=NonnegativeMoney(records.bestDailyRevenue);
        records.bestTotalRevenue=NonnegativeMoney(records.bestTotalRevenue);
        return records;
    }
    private static string NonnegativeMoney(string value)=>decimal.TryParse(value,NumberStyles.Number,CultureInfo.InvariantCulture,out var amount) && amount>=0
        ?amount.ToString(CultureInfo.InvariantCulture):"0";
    public static string RecordsSummary(BestRecords r)
    {
        r=NormalizeRecords(r);
        if(r.bestDay==0)return "最高記録：まだありません";
        return $"最高記録：{r.bestDay}日目まで ・ 1日の売上 {UiKit.Yen(Decimal(r.bestDailyRevenue))} ・ 通算売上 {UiKit.Yen(Decimal(r.bestTotalRevenue))} ・ 配達 {r.bestDelivered}件";
    }
}
