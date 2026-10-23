using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Title scene (D-5a): the name from GameInfo, buttons by save state, help and credits, separate art layers and
/// the motion switch. Play Mode in the Title scene only. Reads temporary save files, never the player's save.</summary>
public static class TitleValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Title (Play Mode, Title scene)")]
    public static void Menu()=>Debug.Log(Run());
    private static string TempDirectory=>Path.Combine(Application.temporaryCachePath,"title-validation");
    public static string Run()
    {
        if(!Application.isPlaying)return "FAIL: Play Mode required";
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Title: "+name);count++;}
        var title=UnityEngine.Object.FindFirstObjectByType<TitleScene>();Check(title!=null,"TitleScene exists (open the Title scene)");
        var view=title.View;Check(view!=null,"title view assigned");
        string original=title.SavePathOverride;
        try
        {
            // One definition of the name.
            Check(GameInfoSync.Differences()=="","build settings, README and credits match GameInfo: "+GameInfoSync.Differences());
            Check(GameInfo.LogoSmall+" "+string.Join(" ",GameInfo.LogoLarge)==GameInfo.DisplayName,"logo lines spell the display name");
            Check(view.LogoText==GameInfo.DisplayName,"logo shows GameInfo: "+view.LogoText);
            Check(Path.GetFileName(CampaignSaveFile.Directory)==GameInfo.SaveFolder,"save folder does not follow the display name: "+CampaignSaveFile.Directory);
            // Play Mode copies lose their prefab link, so check the asset: every serialized reference is set.
            var prefab=AssetDatabase.LoadAssetAtPath<TitleView>(TitleViewBuilder.PrefabPath);Check(prefab!=null,"prefab "+TitleViewBuilder.PrefabPath);
            var missing=MissingReferences(prefab);Check(missing=="","prefab references set: "+missing);

            // First launch: no file.
            Check(Stage("first")=="","first launch layout: "+Stage("first"));
            Check(!view.ShowsContinue && view.ShowsFirstTimeBadge,"first launch hides continue and tags the tutorial");
            Check(view.NewGameButton.Kind==StickerKind.Primary,"without a save, new game is the main button");
            Check(!view.TutorialButton.Interactable && !TitleScene.TutorialReady,"tutorial shown disabled until S2-T");
            Check(view.Status=="","no problem note on first launch");
            // A usable save (the summary line comes from the file).
            Check(Stage("save")=="","save layout: "+Stage("save"));
            Check(view.ShowsContinue && !view.ShowsFirstTimeBadge,"a save shows continue and no first-time tag");
            Check(view.Summary=="3日目 ・ ¥1,240 ・ 評判 72","continue summary: "+view.Summary);
            Check(view.NewGameButton.Kind==StickerKind.Secondary && view.ContinueButton.Kind==StickerKind.Primary,"continue is the main button");
            // An unreadable save.
            Check(Stage("broken")=="","unreadable save layout: "+Stage("broken"));
            Check(!view.ShowsContinue && !view.ShowsFirstTimeBadge && view.Status.Contains("読めませんでした"),"unreadable save explains itself: "+view.Status);

            // Help and credits open over the title and close again.
            view.HelpButton.Button.onClick.Invoke();Check(title.HelpOpen,"help opens");title.CloseHelp();Check(!title.HelpOpen,"help closes");
            view.CreditsButton.onClick.Invoke();Check(title.CreditsOpen,"credits open from the footer");title.CloseCredits();Check(!title.CreditsOpen,"credits close");
            Check(new SerializedObject(title).FindProperty("creditsText").objectReferenceValue is TextAsset t && t.text.Contains("Mochiy Pop One"),"credits text comes from Docs/Credits.md");

            // Art: separate layers and decoration never takes clicks.
            var scene=view.transform.Find("Scene");
            foreach(string layer in new[]{"Sky","Town","Road","Shop","Lights"})Check(scene!=null && scene.Find(layer)!=null,"separate layer "+layer);
            Check(scene.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"background passes clicks through");
            // Motion follows the shared switch.
            view.Tick(1,false);float w1=view.WindowGlow,b1=view.BulbGlow(0);view.Tick(2.3f,false);
            Check(Mathf.Approximately(w1,view.WindowGlow) && Mathf.Approximately(b1,view.BulbGlow(0)),"motion off holds still");
            view.Tick(1,true);float b2=view.BulbGlow(0);view.Tick(2.3f,true);Check(!Mathf.Approximately(b2,view.BulbGlow(0)),"motion on twinkles");
        }
        finally{title.SavePathOverride=original;title.CloseHelp();title.CloseCredits();title.Refresh();}
        return $"PASS: {count} title checks.";
    }
    /// <summary>A minimal save that the title can summarise (3日目, ¥1,240, 評判 72).</summary>
    private const string SampleSave="{\"version\":3,\"day\":3,\"cash\":\"1240\",\"openingCash\":\"1240\",\"preparationCosts\":\"0\",\"reputation\":72,\"people\":[{\"Name\":\"Aoi\"}]}";
    /// <summary>For validation and screenshots: shows the title as "save", "first" (no save) or "broken" from a temporary
    /// file, optionally with the credits open. Returns the layout issues of what is shown (empty = none). Call Restore afterwards.</summary>
    public static string Stage(string kind,bool credits=false)
    {
        var title=UnityEngine.Object.FindFirstObjectByType<TitleScene>();
        Directory.CreateDirectory(TempDirectory);string path=Path.Combine(TempDirectory,kind+".json");
        if(File.Exists(path))File.Delete(path);
        if(kind=="save")File.WriteAllText(path,SampleSave);else if(kind=="broken")File.WriteAllText(path,"{");
        title.SavePathOverride=path;title.Refresh();if(credits)title.ShowCredits();else title.CloseCredits();
        Canvas.ForceUpdateCanvases();return VisualValidation.LayoutReport();
    }
    public static void Restore()
    {
        var title=UnityEngine.Object.FindFirstObjectByType<TitleScene>();if(title==null)return;
        title.SavePathOverride=null;title.CloseCredits();title.Refresh();
    }
    /// <summary>Names of the object-reference fields (and array elements) left empty on a component.</summary>
    public static string MissingReferences(Component component)
    {
        var so=new SerializedObject(component);var p=so.GetIterator();string missing="";
        for(bool enter=true;p.NextVisible(enter);enter=false)
        {
            if(p.propertyType==SerializedPropertyType.ObjectReference && p.name!="m_Script" && p.objectReferenceValue==null)missing+=p.propertyPath+" ";
            if(p.isArray && p.propertyType==SerializedPropertyType.Generic)
                for(int i=0;i<p.arraySize;i++){var e=p.GetArrayElementAtIndex(i);if(e.propertyType==SerializedPropertyType.ObjectReference && e.objectReferenceValue==null)missing+=e.propertyPath+" ";}
        }
        return missing.Trim();
    }
}
