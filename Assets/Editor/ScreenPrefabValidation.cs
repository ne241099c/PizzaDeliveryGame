using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>The prefab-built shop screens, through the buttons the player actually sees (the older ScreenValidation
/// presses the hidden legacy panels). Play Mode only; saving is suppressed.</summary>
public static class ScreenPrefabValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Screen Prefabs (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    public static string Run()
    {
        if(!Application.isPlaying)return "FAIL: Play Mode required";
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Screen prefabs: "+name);count++;}
        var save=Find<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var router=Find<ScreenRouter>();var campaign=Find<CampaignManager>();var roster=Find<DeliveryPersonSelector>();var crew=Find<KitchenCrew>();

        // 開店準備 (4-2): tabs, the promise slider and the start button.
        var hub=Find<HubView>();Check(hub!=null,"hub view exists");
        var hubMissing=TitleValidation.MissingReferences(AssetDatabase.LoadAssetAtPath<HubView>(ScreenPrefabs.Folder+"HubView.prefab")).Split(' ').Where(m=>m.Length>0 && !m.StartsWith("tabBadges.Array.data[2]") && !m.StartsWith("tabBadges.Array.data[3]")).ToArray();
        Check(hubMissing.Length==0,"hub prefab references set: "+string.Join(" ",hubMissing));
        save.StartNewGame();Canvas.ForceUpdateCanvases();
        Check(router.Current==ScreenId.Hub && router.Root(ScreenId.Hub)==hub.GetComponent<CanvasGroup>(),"new game shows the prefab hub");
        var hubButtons=hub.GetComponentsInChildren<StickerButton>(true);
        hubButtons.First(b=>b.name=="Tab2").Button.onClick.Invoke();Check(hub.SelectedTab==2,"facility tab opens");
        hubButtons.First(b=>b.name=="Tab0").Button.onClick.Invoke();Check(hub.SelectedTab==0,"staff tab opens");
        var spawner=Find<OrderSpawner>();float promise=spawner.PromiseSeconds;var slider=hub.GetComponentsInChildren<UnityEngine.UI.Slider>(true).First(x=>x.name=="PromiseSlider");
        UiKit.SyncPromise(slider,spawner,Find<GameClock>()); // the view syncs its range every frame; this call is in the same frame
        slider.value=Mathf.Clamp(promise+10,slider.minValue,slider.maxValue);Check(Mathf.Approximately(spawner.PromiseSeconds,slider.value),"promise slider sets the promise");spawner.SetPromiseSeconds(promise);
        Check(VisualValidation.LayoutReport()=="","hub layout: "+VisualValidation.LayoutReport());
        var startClock=Find<GameClock>();bool startTick=startClock.AutomaticTick;startClock.AutomaticTick=false;
        hubButtons.First(b=>b.name=="StartBusinessV2").Button.onClick.Invoke();Check(startClock.Phase==DayPhase.Open && router.Current==ScreenId.Business,"営業開始 opens the shop");
        startClock.AutomaticTick=startTick;

        // 今日のまとめ (4-3): a day with no deliveries, then the buttons on the receipt screen.
        var settlement=Find<SettlementView>();Check(settlement!=null,"settlement view exists");
        Check(TitleValidation.MissingReferences(AssetDatabase.LoadAssetAtPath<SettlementView>(ScreenPrefabs.Folder+"SettlementView.prefab"))=="","settlement prefab references set");
        var clock=Find<GameClock>();bool tick=clock.AutomaticTick;clock.AutomaticTick=false;
        try
        {
            save.StartNewGame();campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1000);
            Check(router.Current==ScreenId.Settlement && router.Root(ScreenId.Settlement)==settlement.GetComponent<CanvasGroup>(),"day end shows the prefab settlement");
            settlement.SkipButton.Button.onClick.Invoke();Check(settlement.Page==4,"スキップ reveals every line");
            var shown=settlement.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>t.text).ToArray();
            Check(shown.Any(t=>t.StartsWith(campaign.DayNumber+"日目")) && settlement.transform.Find("Receipt")!=null,"title and receipt for the day");
            Check(VisualValidation.LayoutReport()=="","settlement layout: "+VisualValidation.LayoutReport());
            bool bankrupt=campaign.IsBankrupt;settlement.NextButton.Button.onClick.Invoke();
            Check(bankrupt?router.Current==ScreenId.GameOver:router.Current==ScreenId.Hub && campaign.DayNumber==2,"次へ goes to "+(bankrupt?"game over":"day 2"));
        }
        finally{clock.AutomaticTick=tick;}

        // 閉店 (4-4)
        var over=Find<GameOverView>();Check(over!=null,"game over view exists");
        Check(TitleValidation.MissingReferences(AssetDatabase.LoadAssetAtPath<GameOverView>(ScreenPrefabs.Folder+"GameOverView.prefab"))=="","game over prefab references set");
        save.StartNewGame();router.Show(ScreenId.GameOver,true);over.Refresh();Canvas.ForceUpdateCanvases();
        Check(router.Root(ScreenId.GameOver)==over.GetComponent<CanvasGroup>(),"router shows the prefab instance");
        var texts=over.GetComponentsInChildren<TMPro.TMP_Text>(true).Select(t=>t.text).ToArray();
        Check(texts.Any(t=>t.Contains(campaign.DayNumber+"日目まで")) && texts.Contains(UiKit.Yen(campaign.TotalRevenue)),"summary shows the campaign numbers");
        int members=Mathf.Min(6,roster.People.Count)+Mathf.Min(2,crew.People.Count);
        Check(over.transform.GetComponentsInChildren<Transform>(true).Count(t=>t.name.StartsWith("Avatar_"))==members,"staff row shows "+members+" people");
        Check(VisualValidation.LayoutReport()=="","game over layout: "+VisualValidation.LayoutReport());
        over.ToTitle.Button.onClick.Invoke();Check(router.Current==ScreenId.Title,"タイトルへ returns to the title");
        router.Show(ScreenId.GameOver,true);over.Retry.Button.onClick.Invoke();
        Check(router.Current==ScreenId.Hub && campaign.DayNumber==1 && !campaign.IsBankrupt,"もう一度 starts a new campaign");
        return $"PASS: {count} screen prefab checks.";
    }
}
