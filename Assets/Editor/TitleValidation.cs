using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Title screen (D-5a): the name from GameInfo, buttons by save state, credits, separate art layers and the motion
/// switch. Play Mode only. Uses a temporary save path, so the player's save and records are not read or written.</summary>
public static class TitleValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Title (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        if(!Application.isPlaying)return "FAIL: Play Mode required";
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Title: "+name);count++;}
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();var router=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();
        var menu=UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>();var view=menu!=null?menu.TitleView:null;
        Check(save!=null && router!=null && menu!=null && view!=null,"title objects exist");
        using var scope=save.SuppressSaving();
        string original=save.SavePathOverride,dir=Path.Combine(Application.temporaryCachePath,"title-validation");Directory.CreateDirectory(dir);
        try
        {
            // One definition of the name.
            Check(GameInfoSync.Differences()=="","build settings, README and credits match GameInfo: "+GameInfoSync.Differences());
            Check(GameInfo.LogoSmall+" "+string.Join(" ",GameInfo.LogoLarge)==GameInfo.DisplayName,"logo lines spell the display name");
            Check(view.LogoText==GameInfo.DisplayName,"logo shows GameInfo: "+view.LogoText);
            Check(Path.GetFileName(CampaignSaveManager.SaveDirectory)==GameInfo.SaveFolder,"save folder does not follow the display name: "+CampaignSaveManager.SaveDirectory);
            // Play Mode copies lose their prefab link, so check the asset: every serialized reference is set.
            var prefab=AssetDatabase.LoadAssetAtPath<TitleView>(TitleViewBuilder.PrefabPath);Check(prefab!=null,"prefab "+TitleViewBuilder.PrefabPath);
            var missing=MissingReferences(prefab);Check(missing=="","prefab references set: "+missing);

            void Show(string path){save.SavePathOverride=path;router.Show(ScreenId.Title,true);Canvas.ForceUpdateCanvases();}
            // First launch: no file.
            Show(Path.Combine(dir,"none.json"));if(File.Exists(save.SavePath))File.Delete(save.SavePath);Show(save.SavePath);
            Check(!view.ShowsContinue && view.ShowsFirstTimeBadge,"first launch hides continue and tags the tutorial");
            Check(view.NewGameButton.Kind==StickerKind.Primary,"without a save, new game is the main button");
            Check(!view.TutorialButton.Interactable && !TitleMenuUI.TutorialReady,"tutorial shown disabled until S2-T");
            Check(view.Status=="","no problem note on first launch");
            Check(VisualValidation.LayoutReport()=="","first launch layout: "+VisualValidation.LayoutReport());
            // A usable save.
            string valid=Path.Combine(dir,"valid.json");File.WriteAllText(valid,save.SnapshotJson());Show(valid);
            Check(view.ShowsContinue && !view.ShowsFirstTimeBadge,"a save shows continue and no first-time tag");
            Check(view.Summary==save.ContinueSummary && view.Summary.Contains("日目 ・ ¥") && view.Summary.Contains("評判"),"continue summary: "+view.Summary);
            Check(view.NewGameButton.Kind==StickerKind.Secondary && view.ContinueButton.Kind==StickerKind.Primary,"continue is the main button");
            Check(VisualValidation.LayoutReport()=="","save layout: "+VisualValidation.LayoutReport());
            // An unreadable save.
            string broken=Path.Combine(dir,"broken.json");File.WriteAllText(broken,"{");Show(broken);
            Check(!view.ShowsContinue && !view.ShowsFirstTimeBadge && view.Status.Contains("読めませんでした"),"unreadable save explains itself: "+view.Status);
            Check(VisualValidation.LayoutReport()=="","unreadable save layout: "+VisualValidation.LayoutReport());

            // Buttons go to the existing actions.
            view.HelpButton.Button.onClick.Invoke();Check(menu.HelpOpen,"help opens");menu.CloseHelp();Check(!menu.HelpOpen,"help closes");
            view.CreditsButton.onClick.Invoke();Check(menu.CreditsOpen,"credits open from the footer");menu.CloseCredits();Check(!menu.CreditsOpen,"credits close");
            Check(menu.GetComponent<TitleMenuUI>()!=null && new SerializedObject(menu).FindProperty("creditsText").objectReferenceValue is TextAsset t && t.text.Contains("Mochiy Pop One"),"credits text comes from Docs/Credits.md");

            // Art: separate layers, decoration never takes clicks, and the old theme pass leaves the title alone.
            var scene=view.transform.Find("Scene");
            foreach(string layer in new[]{"Sky","Town","Road","Shop","Lights"})Check(scene!=null && scene.Find(layer)!=null,"separate layer "+layer);
            Check(scene.GetComponentsInChildren<Graphic>(true).All(g=>!g.raycastTarget),"background passes clicks through");
            Check(view.GetComponentsInChildren<ThemedText>(true).Length==0 && view.GetComponentsInChildren<ThemedButton>(true).Length==0,"legacy theme pass skips the title");
            // Motion follows the shared switch.
            view.Tick(1,false);float w1=view.WindowGlow,b1=view.BulbGlow(0);view.Tick(2.3f,false);
            Check(Mathf.Approximately(w1,view.WindowGlow) && Mathf.Approximately(b1,view.BulbGlow(0)),"motion off holds still");
            view.Tick(1,true);float b2=view.BulbGlow(0);view.Tick(2.3f,true);Check(!Mathf.Approximately(b2,view.BulbGlow(0)),"motion on twinkles");
        }
        finally{save.SavePathOverride=original;router.Show(ScreenId.Title,true);}
        return $"PASS: {count} title checks.";
    }
    /// <summary>For screenshots: shows the title as "save", "first" (no save) or "broken" from a temporary save file, optionally
    /// with the credits open. Returns the layout issues of what is shown (empty = none). Call Restore afterwards.</summary>
    public static string Stage(string kind,bool credits=false)
    {
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();var router=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();var menu=UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>();
        string dir=Path.Combine(Application.temporaryCachePath,"title-validation");Directory.CreateDirectory(dir);string path=Path.Combine(dir,kind+".json");
        if(File.Exists(path))File.Delete(path);
        if(kind=="save")File.WriteAllText(path,save.SnapshotJson());else if(kind=="broken")File.WriteAllText(path,"{");
        save.SavePathOverride=path;router.Show(ScreenId.Title,true);if(credits)menu.ShowCredits();else menu.CloseCredits();
        Canvas.ForceUpdateCanvases();return VisualValidation.LayoutReport();
    }
    public static void Restore()
    {
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();var menu=UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>();
        save.SavePathOverride=null;menu.CloseCredits();UnityEngine.Object.FindFirstObjectByType<ScreenRouter>().Show(ScreenId.Title,true);
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
