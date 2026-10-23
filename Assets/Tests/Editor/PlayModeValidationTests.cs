using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The scene validations as Test Runner tests. Each test opens its scene (Game, or Title for the title checks),
/// enters Play Mode, runs one validation and exits. Simulations (minutes long) are not included.
/// Automatic saving is switched off as soon as Play Mode starts and stays off, so the player's save and records are kept.</summary>
public sealed class PlayModeValidationTests
{
    private const string GameScene="Assets/Scenes/Game.unity",TitleScene="Assets/Scenes/Title.unity";

    private static IEnumerator Run(Func<string> validation,bool newGameFirst=false,string scene=GameScene)
    {
        if(!Application.isPlaying)
        {
            // Play Mode runs the scene as it is in memory (like pressing Play), so unsaved edits to that scene are fine.
            var active=EditorSceneManager.GetActiveScene();
            if(active.path!=scene)
            {
                if(active.isDirty)Assert.Inconclusive("Save or discard "+active.path+" first (tests open "+scene+").");
                EditorSceneManager.OpenScene(scene);
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
        // Saving is already off; leaving Play Mode discards the test's game.
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();if(save!=null)save.AutomaticSavingEnabled=false;
    }

    [UnityTest] public IEnumerator Title()=>Run(TitleValidation.Run,false,TitleScene);
    [UnityTest] public IEnumerator CoreAndCampaign()=>Run(M6Validation.Run); // M3/M4, M5, reset and M6
    [UnityTest] public IEnumerator Screens()=>Run(ScreenValidation.Run);
    [UnityTest] public IEnumerator Visual()=>Run(VisualValidation.Run,true);
    [UnityTest] public IEnumerator Business()=>Run(M7Validation.Run);
    [UnityTest] public IEnumerator Shop()=>Run(ShopValidation.Run);
    [UnityTest] public IEnumerator PrefabScreens()=>Run(ScreenPrefabValidation.Run);
}
