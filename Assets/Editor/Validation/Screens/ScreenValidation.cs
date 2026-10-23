using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>Screen routing in the Game scene: one screen takes input at a time, campaign events switch screens, and the
/// in-game menu pauses the clock. Play Mode only; saving is suppressed.</summary>
public static class ScreenValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate Screens (Play Mode)")]
    public static void Menu()=>Debug.Log(Run());
    private static T Find<T>() where T:UnityEngine.Object=>UnityEngine.Object.FindFirstObjectByType<T>();
    public static string Run()
    {
        var router=Find<ScreenRouter>();var campaign=Find<CampaignManager>();var clock=Find<GameClock>();var save=Find<CampaignSaveManager>();
        using var scope=save.SuppressSaving();int count=0;
        void Check(bool ok,string name){if(!ok)throw new Exception("Screens: "+name);count++;}
        foreach(ScreenId id in Enum.GetValues(typeof(ScreenId)))
        {
            Check(router.Root(id)!=null && router.Root(id).gameObject.activeInHierarchy,"active screen root "+id);
            router.Show(id,true);
            foreach(ScreenId other in Enum.GetValues(typeof(ScreenId)))
                Check(router.Root(other).blocksRaycasts==(other==id),"only target screen receives clicks "+id+" / "+other);
        }
        var scenes=EditorBuildSettings.scenes.Where(s=>s.enabled).Select(s=>System.IO.Path.GetFileNameWithoutExtension(s.path)).ToArray();
        Check(scenes.Length>=2 && scenes[0]==GameScenes.Title && scenes.Contains(GameScenes.Game),"build starts at the Title scene and includes the Game scene");

        save.StartNewGame();Check(router.Current==ScreenId.Hub,"preparation event opens hub");
        campaign.StartBusiness();Check(router.Current==ScreenId.Business,"business event opens the shop");
        var spawner=Find<OrderSpawner>();bool spawning=spawner.enabled;spawner.enabled=false;
        try
        {
            // Planning a route does not stop time; dispatching clears the selection.
            var list=Find<OrderBook>();var roster=Find<CourierRoster>();var planner=Find<DeliveryPlanner>();
            var order=M7Validation.ReadyOrder("Goal1","Screen route",clock.ElapsedSeconds,100);list.AddOrder(order);
            Check(list.SelectOnly(order) && roster.SelectForDispatch(roster.People[0]),"a ready order and an idle courier can be selected");
            float before=clock.ElapsedSeconds;clock.Advance(1);Check(clock.ElapsedSeconds>before,"planning does not pause game time");
            planner.DispatchWithSuggestion();
            Check(list.SelectedOrders.Count==0 && !roster.People[0].IsAvailable,"dispatch sends the courier and clears the selection");
            clock.Advance(clock.BusinessDuration+1000);Check(router.Current==ScreenId.Settlement,"last return and settlement event");
        }
        finally{spawner.enabled=spawning;}

        // In-game menu: opens from the hub's menu button, pauses an automatic clock, asks before leaving a business day.
        save.StartNewGame();
        var menu=Find<GameMenu>();bool automatic=clock.AutomaticTick;clock.AutomaticTick=true;
        try
        {
            campaign.StartBusiness();menu.ShowMenu();Check(menu.MenuOpen && !clock.AutomaticTick,"menu pauses the automatic clock");
            menu.ShowHelp();
            Check(menu.HelpOpen,"help opens over menu");
            menu.CloseHelp();
            Check(menu.MenuOpen && !clock.AutomaticTick,"closing help keeps the pause");
            menu.RequestTitle();Check(menu.ConfirmationOpen,"leaving during business asks first");
            var buttons=menu.GetComponentsInChildren<StickerButton>(true).Concat(UnityEngine.Object.FindObjectsByType<StickerButton>(FindObjectsInactive.Include,FindObjectsSortMode.None)).ToArray();
            Check(buttons.Any(b=>b.name=="ModalConfirm"),"the confirmation offers タイトルへ");
            buttons.First(b=>b.name=="ModalCancel").Button.onClick.Invoke();
            Check(!menu.ConfirmationOpen && menu.MenuOpen && router.Current==ScreenId.Business,"営業に戻る keeps the business day");
            menu.Resume();Check(!menu.MenuOpen && clock.AutomaticTick,"resume restores ticking");
        }
        finally{menu.Resume();clock.AutomaticTick=automatic;save.StartNewGame();}
        return $"PASS: {count} screen checks.";
    }
}
