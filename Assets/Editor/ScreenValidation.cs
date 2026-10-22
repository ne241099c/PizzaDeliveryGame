using System;
using UnityEngine;
using UnityEditor;

public static class ScreenValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Screens (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    public static string Run()
    {
        var router=UnityEngine.Object.FindFirstObjectByType<ScreenRouter>();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        using var scope=save.SuppressSaving();int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception("Screens: "+name);count++;}
        foreach(ScreenId id in Enum.GetValues(typeof(ScreenId)))
        {
            Check(router.Root(id)!=null && router.Root(id).gameObject.activeInHierarchy,"active screen root "+id);
            router.Show(id,true);
            foreach(ScreenId other in Enum.GetValues(typeof(ScreenId)))
                Check(router.Root(other).blocksRaycasts==(other==id),"only target screen receives clicks "+id+" / "+other);
        }
        campaign.NewGame();Check(router.Current==ScreenId.Hub,"preparation event opens hub");
        campaign.StartBusiness();Check(router.Current==ScreenId.Business,"business event opens map");
        UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;
        clock.Advance(clock.BusinessDuration+1000);Check(router.Current==ScreenId.Settlement,"last return and settlement event");
        campaign.NewGame();return $"PASS: {count} screen checks.";
    }
}
