using System;
using UnityEditor;
using UnityEngine;

/// <summary>The Game scene right after it starts (new game or continue): the generated town is in place and the state it
/// would save can be loaded again. Play Mode only; reads the snapshot in memory, never writes the save.</summary>
public static class LaunchValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Launch (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Launch: "+name);count++;}
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();var map=UnityEngine.Object.FindFirstObjectByType<MapManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        Check(save!=null && map!=null && map.IsReady,"Game scene started with its map");
        Check(clock.Phase==DayPhase.BeforeOpening,"the campaign starts in preparation");
        Check(map.CityState==null || map.City!=null && map.CityState.blocks==map.City.blocks.Count && map.CityState.blocks>0,"the generated town is loaded (not the sample map)");
        Check(save.TryParse(save.SnapshotJson(),out _,out var reason),"what would be saved loads again: "+reason);
        return $"PASS: {count} launch checks.";
    }
}
