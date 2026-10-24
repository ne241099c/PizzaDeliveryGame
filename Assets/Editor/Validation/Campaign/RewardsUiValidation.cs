using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Focused presentation checks; does not spend funds or write real campaign/profile saves.</summary>
public static class RewardsUiValidation
{
    public static string RunGame()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();
        using var guard=save.SuppressSaving();
        var view=UnityEngine.Object.FindFirstObjectByType<RewardsView>();
        int n=0;
        void Check(bool valid,string label){if(!valid)throw new Exception("Rewards UI: "+label);n++;}
        Check(view!=null,"reward view installed");
        view.Show();
        Check(view.IsOpen,"goal modal opens");
        var canvas=view.GetComponent<ScreenRouter>().Root(ScreenId.Hub).GetComponentInParent<Canvas>();
        var all=canvas.GetComponentsInChildren<Transform>(true);
        var panel=all.First(t=>t.name=="GoalsModal").GetComponent<CanvasGroup>();
        Check(panel.alpha==1 && panel.blocksRaycasts && panel.interactable,"goal modal uses CanvasGroup visibility");
        var scroll=all.First(t=>t.name=="GoalsScroll").GetComponent<ScrollRect>();
        Check(scroll.vertical && !scroll.horizontal && scroll.content.rect.height>scroll.viewport.rect.height,"goals fit in vertical scroller");
        Check(scroll.verticalScrollbar!=null,"visible scrolling handle provided");
        var rewards=RewardProgression.Get(UnityEngine.Object.FindFirstObjectByType<CampaignManager>());
        Check(all.Count(t=>t.name.StartsWith("Mission_"))==rewards.TodayMissions.Count,"every mission of the day is listed");
        Check(!all.Any(t=>t.name=="RepaymentSlider") && !all.Any(t=>t.name=="GoalsButton"),"no repayment in the menu modal or after settlement");
        Check(all.Any(t=>t.name=="ContinueEndless") && all.Any(t=>t.name=="ResultTitle"),"result offers continue or title");
        view.Close();
        Check(!view.IsOpen && panel.alpha==0 && !panel.blocksRaycasts,"close releases raycasts");
        // 開店準備: loan gauge in the top bar, the ローン・ミッション tab with repayment and missions.
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var hub=UnityEngine.Object.FindFirstObjectByType<HubView>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;view.GetComponent<ScreenRouter>().Show(ScreenId.Hub,true);
            var gauge=all.First(t=>t.name=="LoanGauge").GetComponent<StickerButton>();
            gauge.Button.onClick.Invoke();
            Check(hub.SelectedTab==HubView.LoanTab,"the loan gauge opens the ローン・ミッション tab");
            economy.ApplyGoalCash(3000);hub.RefreshNow();
            var tab=hub.Content.GetComponentsInChildren<Transform>(true);
            Check(tab.Count(t=>t.name.StartsWith("Mission_"))==rewards.TodayMissions.Count,"missions on the preparation screen");
            Check(tab.Any(t=>t.name=="RepayKeep") && tab.Any(t=>t.name=="RepayAll") && tab.Any(t=>t.name=="Repay1000"),"repayment buttons on the preparation screen");
            decimal loan=rewards.Loan,cash=economy.Cash;
            tab.First(t=>t.name=="Repay1000").GetComponent<StickerButton>().Button.onClick.Invoke();
            Check(rewards.Loan==loan-1000 && economy.Cash==cash-1000,"repay 1000 from the preparation tab");
            Check(gauge.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true).Any(t=>t.name=="Caption" && t.text.Contains("返済 10%")),"top bar shows the repayment progress");
            campaign.StartBusiness();hub.RefreshNow();
            var open=hub.Content.GetComponentsInChildren<StickerButton>(true).FirstOrDefault(b=>b.name=="RepayAll");
            Check(open==null || !open.Interactable,"no repayment after opening");
        }
        finally{campaign.NewGame();clock.AutomaticTick=false;hub.ShowTab(0);}
        return $"PASS {n} reward UI checks";
    }
    public static string RunTitle()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var title=UnityEngine.Object.FindFirstObjectByType<TitleScene>();
        int n=0;
        void Check(bool valid,string label){if(!valid)throw new Exception("Rewards title: "+label);n++;}
        Check(title!=null,"title installed");
        title.View.NewGameButton.Button.onClick.Invoke();
        Check(title.ModeOpen,"new game asks for mode");
        var all=title.View.GetComponentInParent<Canvas>().GetComponentsInChildren<Transform>(true);
        Check(all.Any(t=>t.name=="NormalMode") && all.Any(t=>t.name=="EndlessMode"),"both modes offered");
        all.First(t=>t.name=="CancelMode").GetComponent<Button>().onClick.Invoke();
        Check(!title.ModeOpen,"mode choice can be cancelled");
        title.ShowProfile();
        Check(title.ProfileOpen,"records open without campaign");
        var scroll=all.First(t=>t.name=="RecordsViewport").GetComponent<ScrollRect>();
        Check(scroll.vertical && scroll.verticalScrollbar!=null,"records can scroll");
        all.First(t=>t.name=="CloseRecords").GetComponent<Button>().onClick.Invoke();
        Check(!title.ProfileOpen,"records close");
        return $"PASS {n} reward title checks";
    }
}
