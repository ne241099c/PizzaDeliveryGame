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
        var slider=all.First(t=>t.name=="RepaymentSlider").GetComponent<Slider>();
        Check(!slider.wholeNumbers && slider.minValue==0,"repayment supports decimal values and zero");
        var rewards=RewardProgression.Get(UnityEngine.Object.FindFirstObjectByType<CampaignManager>());
        Check(all.Count(t=>t.name.StartsWith("Mission_"))==rewards.TodayMissions.Count,"all offered mission buttons present");
        Check(all.Any(t=>t.name=="Repay100") && all.Any(t=>t.name=="Repay500") && all.Any(t=>t.name=="RepayAll"),"repayment presets present");
        Check(all.Any(t=>t.name=="ContinueEndless") && all.Any(t=>t.name=="ResultTitle"),"result offers continue or title");
        view.Close();
        Check(!view.IsOpen && panel.alpha==0 && !panel.blocksRaycasts,"close releases raycasts");
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
