using System;
using UnityEngine;

/// <summary>Old numeric sample tests use their authored graph. City regressions exercise generated towns separately.</summary>
public sealed class SampleMapValidationScope:IDisposable
{
    private readonly MapManager map;private readonly CampaignSaveManager save;private readonly IDisposable protection;private readonly bool original;
    public SampleMapValidationScope()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required for sample validation.");
        save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();protection=save.SuppressSaving();
        map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        original=map.UseSampleForValidation;
        map.UseSampleForValidation=true;
        map.RestoreCity(null);
    }
    public void Dispose()
    {
        try
        {
            map.UseSampleForValidation=original;
            if(!original)
            {
                save.StartNewGame();
                UnityEngine.Object.FindFirstObjectByType<GameClock>().AutomaticTick=false;
                CampaignSaveManager.SessionSavingLocked=true;
            }
        }
        finally{protection.Dispose();}
    }
}
