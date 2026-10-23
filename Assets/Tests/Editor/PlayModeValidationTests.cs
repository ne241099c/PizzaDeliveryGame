using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The existing scene validations (Tools/Review/run_suite.py) as Test Runner tests. Each test opens SampleScene,
/// enters Play Mode, runs one validation and exits. Simulations (minutes long) are not included.
/// Automatic saving is switched off as soon as Play Mode starts and stays off, so the player's save and records are kept.</summary>
public sealed class PlayModeValidationTests
{
    private const string Scene="Assets/Scenes/SampleScene.unity";

    private static IEnumerator Run(Func<string> validation,bool newGameFirst=false)
    {
        if(!Application.isPlaying)
        {
            // Play Mode runs the scene as it is in memory (like pressing Play), so unsaved edits to SampleScene are fine.
            // Hidden legacy layouts (ContentSizeFitter) re-mark it dirty after every domain reload.
            var active=EditorSceneManager.GetActiveScene();
            if(active.path!=Scene)
            {
                if(active.isDirty)Assert.Inconclusive("Save or discard "+active.path+" first (tests open "+Scene+").");
                EditorSceneManager.OpenScene(Scene);
            }
            yield return new EnterPlayMode();
        }
        // Saving stays off for the whole test, frames included (scopes inside the validations restore to off).
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();if(save!=null)save.AutomaticSavingEnabled=false;
        yield return null; // let Start() run once
        if(newGameFirst)
        {
            // As in run_suite.py, where earlier validations leave a started campaign (and its generated town) behind.
            save.StartNewGame();
            yield return null;yield return null;
        }
        string result=null;
        try{result=validation();}
        finally{CleanUp();}
        yield return new ExitPlayMode();
        StringAssert.StartsWith("PASS",result);
    }
    private static void CleanUp()
    {
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();if(save==null)return;
        using(save.SuppressSaving()){save.StartNewGame();var menu=UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>();if(menu!=null)menu.ReturnTitle();}
    }

    [UnityTest] public IEnumerator Title()=>Run(TitleValidation.Run);
    [UnityTest] public IEnumerator CoreAndCampaign()=>Run(M6Validation.Run); // M3/M4, M5, reset and M6
    [UnityTest] public IEnumerator Screens()=>Run(ScreenValidation.Run);
    [UnityTest] public IEnumerator Visual()=>Run(VisualValidation.Run,true);
    [UnityTest] public IEnumerator Business()=>Run(M7Validation.Run);
    [UnityTest] public IEnumerator Shop()=>Run(ShopValidation.Run);
    [UnityTest] public IEnumerator PrefabScreens()=>Run(ScreenPrefabValidation.Run);
}
