using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>The scene validations as Test Runner tests. Each test enters Play Mode in the Title scene, locks saving, loads
/// the Game scene when needed, runs one validation and exits. Simulations (minutes long) are not included.
/// Automatic saving is switched off as soon as Play Mode starts and stays off, so the player's save and records are kept.</summary>
public sealed class PlayModeValidationTests
{
    private const string GameScene="Assets/Scenes/Game.unity",TitleScene="Assets/Scenes/Title.unity";

    /// <summary>Enters Play Mode in the Title scene, locks saving for the session, loads the scene under test (in the given
    /// mode), runs the validation and exits. The lock is in place before the Game scene starts, so its startup can never write
    /// the player's save. One flat coroutine: the Test Runner only handles EnterPlayMode yielded from the test itself.</summary>
    /// <param name="legacyAmounts">Older validations check exact amounts from before the money change of 2026-10-24
    /// (EconomyScale): they run with those base amounts. Reward validations use the real amounts.</param>
    private static IEnumerator Run(Func<string> validation,bool newGameFirst=false,string scene=GameScene,LaunchMode mode=LaunchMode.Direct,bool legacyAmounts=true)
    {
        if(!Application.isPlaying)
        {
            // Play Mode runs the scene as it is in memory (like pressing Play), so unsaved edits to that scene are fine.
            var active=EditorSceneManager.GetActiveScene();
            if(active.path!=TitleScene)
            {
                if(active.isDirty)Assert.Inconclusive("Save or discard "+active.path+" first (tests open "+TitleScene+").");
                EditorSceneManager.OpenScene(TitleScene);
            }
            yield return new EnterPlayMode();
        }
        CampaignSaveManager.SessionSavingLocked=true;
        if(scene==GameScene)
        {
            GameScenes.LoadGame(mode);
            for(int i=0;i<300 && !GameReady();i++)yield return null;
        }
        yield return null; // let Start() run once
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();if(save!=null)save.AutomaticSavingEnabled=false;
        var amounts=scene==GameScene && legacyAmounts?EconomyScale.Legacy():null;
        string result=null;
        try
        {
            if(newGameFirst)
            {
                // As in run_suite.py, where earlier validations leave a started campaign (and its generated town) behind.
                save.StartNewGame();
                yield return null;yield return null;
            }
            try{result=validation();}
            finally{CleanUp();}
        }
        finally{amounts?.Dispose();}
        yield return new ExitPlayMode();
        StringAssert.StartsWith("PASS",result);
    }
    private static bool GameReady()
    {
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name==GameScenes.Game && clock!=null && map!=null && map.IsReady && clock.Phase==DayPhase.BeforeOpening;
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
    [UnityTest] public IEnumerator ShopUx()=>Run(ShopUxValidation.Run);
    [UnityTest] public IEnumerator SavePreservation()=>Run(M6Validation.RunSaveOnly);
    [UnityTest] public IEnumerator KitchenRecovery()=>Run(KitchenRecoveryValidation.Run);
    [UnityTest] public IEnumerator KitchenQueue()=>Run(KitchenQueueValidation.Run);
    [UnityTest] public IEnumerator KitchenTraining()=>Run(KitchenTrainingValidation.Run);
    [UnityTest] public IEnumerator GameOverSave()=>Run(GameOverSaveValidation.Run);
    [UnityTest] public IEnumerator RewardCampaign()=>Run(RewardsCampaignValidation.Run,legacyAmounts:false);
    [UnityTest] public IEnumerator RewardScreens()=>Run(RewardsUiValidation.RunGame,legacyAmounts:false);
    [UnityTest] public IEnumerator PizzaMenu()=>Run(PizzaMenuValidation.Run);
    [UnityTest] public IEnumerator CityGeneration()=>Run(CityGenerationValidation.RunBasic);
    [UnityTest] public IEnumerator CityNetwork()=>Run(CityGenerationValidation.RunNetwork);
    [UnityTest] public IEnumerator CityCampaign()=>Run(CityCampaignValidation.Run);
    [UnityTest] public IEnumerator CityOrders()=>Run(CityOrderValidation.Run);
    [UnityTest] public IEnumerator PrefabScreens()=>Run(ScreenPrefabValidation.Run);
    /// <summary>The Game scene as the title starts it: continue (the player's save, or a new game without one), then 最初から.</summary>
    [UnityTest] public IEnumerator LaunchContinue()=>Run(LaunchValidation.Run);
    [UnityTest] public IEnumerator LaunchNewGame()=>Run(LaunchValidation.Run,false,GameScene,LaunchMode.NewGame);
}
