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
        campaign.NewGame();
        if(router.ImplementationStage>=2)
        {
            var hub=UnityEngine.Object.FindFirstObjectByType<HubScreenUI>();var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            UIVisibility.Find(canvas.transform,"ApplicantsButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Check(hub.SelectedTab==1,"applicant button opens applicant tab");
            Check(UIVisibility.Find(canvas.transform,"ApplicantsTab").GetComponent<CanvasGroup>().blocksRaycasts,"applicants receive input");
            Check(!UIVisibility.Find(canvas.transform,"StaffTab").GetComponent<CanvasGroup>().blocksRaycasts,"hidden staff tab does not intercept input");
            UIVisibility.Find(canvas.transform,"ManagementButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(hub.SelectedTab==2,"management button opens management tab");
            UIVisibility.Find(canvas.transform,"StaffButton").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(hub.SelectedTab==0,"staff button returns to team");
        }
        if(router.ImplementationStage>=3)
        {
            var business=UnityEngine.Object.FindFirstObjectByType<BusinessScreenUI>();var list=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();var roster=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
            var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();var planner=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();
            campaign.StartBusiness();business.Refresh();Check(!business.IsPlanningPanelOpen,"no planning panel without selections");
            var order=new Order("Goal1","Screen route",0,100);list.AddOrder(order);list.SelectOnly(order);roster.SelectForDispatch(roster.People[0]);business.Refresh();
            Check(business.IsPlanningPanelOpen,"order and courier open route panel");
            float before=clock.ElapsedSeconds;clock.Advance(1);Check(clock.ElapsedSeconds>before,"planning does not pause game time");
            UIVisibility.Find(canvas.transform,"Deselect").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();business.Refresh();Check(!business.IsPlanningPanelOpen && list.SelectedOrders.Count==0,"deselect closes planning panel");
            list.SelectOnly(order);roster.SelectForDispatch(roster.People[0]);business.Refresh();planner.DispatchWithSuggestion();business.Refresh();Check(!business.IsPlanningPanelOpen,"dispatch closes panel");
            campaign.NewGame();
        }
        if(router.ImplementationStage>=4)
        {
            var ui=UnityEngine.Object.FindFirstObjectByType<SettlementScreenUI>();var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            campaign.StartBusiness();UnityEngine.Object.FindFirstObjectByType<OrderSpawner>().enabled=false;clock.Advance(clock.BusinessDuration+1000);
            Check(ui.Page==0 && !ui.IsShowingAll,"settlement starts with revenue page");
            UIVisibility.Find(canvas.transform,"AdvancePage").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(ui.Page==1,"click advances settlement page");
            UIVisibility.Find(canvas.transform,"ShowAll").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(ui.IsShowingAll && UIVisibility.Find(canvas.transform,"ResultText").GetComponent<TMPro.TextMeshProUGUI>().text.Contains(campaign.FinancialSummary),"show all reuses real ledger summary");
            UIVisibility.Find(canvas.transform,"SkipPages").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(ui.Page==4 && !ui.IsShowingAll,"skip reaches final page");
            UIVisibility.Find(canvas.transform,"ToGameOver").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(router.Current==ScreenId.GameOver,"bankrupt settlement opens game over");
            UIVisibility.Find(canvas.transform,"RetryCampaign").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(router.Current==ScreenId.Hub && campaign.DayNumber==1 && !campaign.IsBankrupt,"retry uses existing new-game reset");
        }
        if(router.ImplementationStage>=5)
        {
            var menu=UnityEngine.Object.FindFirstObjectByType<TitleMenuUI>();var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
            bool automatic=clock.AutomaticTick;clock.AutomaticTick=true;campaign.StartBusiness();
            UIVisibility.Find(canvas.transform,"HeaderMenu").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(menu.MenuOpen && !clock.AutomaticTick,"menu button pauses automatic clock");
            menu.ShowHelp();Check(menu.HelpOpen,"help opens over menu");menu.CloseHelp();Check(menu.MenuOpen && !clock.AutomaticTick,"closing help keeps menu pause");
            menu.RequestTitle();Check(menu.ConfirmationOpen,"title during business requires confirmation");UIVisibility.Find(canvas.transform,"CancelTitle").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(!menu.ConfirmationOpen && router.Current==ScreenId.Business,"cancel preserves current business");
            menu.Resume();Check(!menu.MenuOpen && clock.AutomaticTick,"resume restores original ticking");
            menu.ReturnTitle();Check(router.Current==ScreenId.Title && !clock.AutomaticTick,"confirmed title stops business ticking");
            UIVisibility.Find(canvas.transform,"NewGame").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();Check(router.Current==ScreenId.Hub && clock.Phase==DayPhase.BeforeOpening,"title new game opens preparation");
            Check(router.StartAtTitle,"startup configured for title");clock.AutomaticTick=automatic;
        }
        return $"PASS: {count} screen checks.";
    }
}
