using System;
using System.IO;
using UnityEngine;

/// <summary>A bankrupt game removes its save, so 続きから cannot reopen the game-over screen. Uses a temporary save
/// file only (the session lock is lifted for that file); the player's save is compared before and after.</summary>
public static class GameOverSaveValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Game over save: "+name);count++;}
        string original=save.SavePathOverride;bool automatic=save.AutomaticSavingEnabled,locked=CampaignSaveManager.SessionSavingLocked;
        string actualPath=save.SavePath;byte[] actualBefore=File.Exists(actualPath)?File.ReadAllBytes(actualPath):null;
        string folder=Path.Combine(Path.GetTempPath(),"pizza-game-over-"+Guid.NewGuid().ToString("N"));
        string temp=Path.Combine(folder,"campaign.json");
        try
        {
            save.SavePathOverride=temp;CampaignSaveManager.SessionSavingLocked=false;save.AutomaticSavingEnabled=true;
            clock.AutomaticTick=false;save.StartNewGame();spawner.enabled=false;
            Check(File.Exists(temp),"a new game is saved");
            campaign.StartBusiness();
            economy.ApplyGoalCash(-economy.Cash-100000);
            clock.Advance(clock.BusinessDuration+1000);
            Check(campaign.IsBankrupt,"negative cash after settlement is bankruptcy");
            Check(!File.Exists(temp),"the bankrupt game's save is removed");
            save.SavePreparation();
            Check(!File.Exists(temp),"later checkpoints do not write the bankrupt game back");
        }
        finally
        {
            save.AutomaticSavingEnabled=false;
            save.SavePathOverride=original;
            CampaignSaveManager.SessionSavingLocked=locked;
            save.AutomaticSavingEnabled=automatic;
            if(Directory.Exists(folder))Directory.Delete(folder,true);
            save.StartNewGame();
        }
        byte[] actualAfter=File.Exists(actualPath)?File.ReadAllBytes(actualPath):null;
        Check(actualBefore==null?actualAfter==null:actualAfter!=null && actualBefore.AsSpan().SequenceEqual(actualAfter),"the player's save is unchanged");
        return $"PASS: {count} game over save checks.";
    }
}
