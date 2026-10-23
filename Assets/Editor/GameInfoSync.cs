using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

/// <summary>Copies GameInfo (the single definition of the game's name and version) to the places that cannot read code:
/// the build settings, README.md and the credits text shown in game (from Docs/Credits.md).</summary>
public static class GameInfoSync
{
    public const string CreditsAsset="Assets/Art/Title/Credits.txt";
    private const string ReadmeBegin="<!-- GameInfo:begin (Tools/Game Info/Sync で生成) -->",ReadmeEnd="<!-- GameInfo:end -->";
    private static string Root=>Path.GetDirectoryName(Application.dataPath);

    [MenuItem("Tools/Game Info/Sync")]
    public static void SyncMenu()=>Debug.Log(Sync());

    public static string Sync()
    {
        PlayerSettings.productName=GameInfo.DisplayName;PlayerSettings.bundleVersion=GameInfo.Version;
        AssetDatabase.SaveAssets();
        WriteReadme();WriteCredits();
        return $"GameInfo synced: {GameInfo.DisplayName} {GameInfo.VersionLabel}";
    }
    /// <summary>Empty when every copy matches GameInfo, otherwise what differs.</summary>
    public static string Differences()
    {
        string issues="";
        if(PlayerSettings.productName!=GameInfo.DisplayName)issues+="productName="+PlayerSettings.productName+"; ";
        if(PlayerSettings.bundleVersion!=GameInfo.Version)issues+="bundleVersion="+PlayerSettings.bundleVersion+"; ";
        var readme=Path.Combine(Root,"README.md");
        if(!File.Exists(readme) || File.ReadAllText(readme)!=Readme(File.Exists(readme)?File.ReadAllText(readme):""))issues+="README.md out of date; ";
        var credits=Path.Combine(Root,CreditsAsset);
        if(!File.Exists(credits) || File.ReadAllText(credits)!=CreditsText())issues+="credits text out of date; ";
        return issues;
    }

    private static string Block()=>
        ReadmeBegin+"\n# "+GameInfo.DisplayName+"\n\n"+GameInfo.Kana+"（"+GameInfo.VersionLabel+"、ゲーム名は仮）\n\n"+
        "小さな街のピザ屋の店長になって、ピザを作り、配達員に届け先と道順を指示するゲーム。\n"+ReadmeEnd;
    private static string Readme(string current)
    {
        int a=current.IndexOf(ReadmeBegin),b=current.IndexOf(ReadmeEnd);
        if(a>=0 && b>a)return current.Substring(0,a)+Block()+current.Substring(b+ReadmeEnd.Length);
        return Block()+"\n\n"+
            "## 開く\n\n- Unity 6000.2.10f1 で、このフォルダを開き、`Assets/Scenes/SampleScene.unity` を再生する。\n"+
            "- ゲーム名・バージョンは `Assets/Scripts/GameInfo.cs` の1か所で定義し、Unityのメニュー **Tools > Game Info > Sync** でビルド設定とこのREADMEに反映する。\n\n"+
            "## 資料\n\n- 共同作業の入口：`AGENTS.md`、`Docs/Handoff/Current.md`\n- 素材の出典：`Docs/Credits.md`\n";
    }
    private static void WriteReadme()
    {
        var path=Path.Combine(Root,"README.md");string current=File.Exists(path)?File.ReadAllText(path):"";
        string next=Readme(current);if(next!=current)File.WriteAllText(path,next);
    }
    /// <summary>Docs/Credits.md as plain text for the in-game credits (links and code marks removed).</summary>
    public static string CreditsText()
    {
        var source=Path.Combine(Root,"Docs/Credits.md");if(!File.Exists(source))return "";
        string text=File.ReadAllText(source);
        text=Regex.Replace(text,@"^# .*\n+","",RegexOptions.Multiline);
        text=Regex.Replace(text,@"\[([^\]]+)\]\([^)]+\)","$1");
        text=text.Replace("`","").Replace("**","");
        text=Regex.Replace(text,@"^- ","・",RegexOptions.Multiline);
        text=Regex.Replace(text,@"\n{2,}","\n");
        return text.Trim()+"\n";
    }
    private static void WriteCredits()
    {
        var path=Path.Combine(Root,CreditsAsset);Directory.CreateDirectory(Path.GetDirectoryName(path));string text=CreditsText();
        if(!File.Exists(path) || File.ReadAllText(path)!=text){File.WriteAllText(path,text);AssetDatabase.ImportAsset(CreditsAsset);}
    }
}
