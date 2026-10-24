using System;
using System.Linq;
using UnityEngine;

/// <summary>The opening, the choice after the loan is repaid and both ways into the ending (chosen in preparation, and the
/// normal mode's last day with the loan repaid), each followed by the result. Nothing is saved. The pictures are checked on
/// screen (Tools/Review/capture_story.py).</summary>
public static class StoryValidation
{
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        var save=UnityEngine.Object.FindFirstObjectByType<CampaignSaveManager>();using var scope=save.SuppressSaving();
        var campaign=UnityEngine.Object.FindFirstObjectByType<CampaignManager>();
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var economy=UnityEngine.Object.FindFirstObjectByType<BusinessEconomy>();
        var rewardsView=UnityEngine.Object.FindFirstObjectByType<RewardsView>();
        var settlement=UnityEngine.Object.FindFirstObjectByType<SettlementView>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var r=campaign.Rewards;
        int count=0;void Check(bool ok,string name){if(!ok)throw new Exception("Story: "+name);count++;}
        StickerButton Button(string name)=>UnityEngine.Object.FindObjectsByType<StickerButton>(FindObjectsInactive.Include,FindObjectsSortMode.None).FirstOrDefault(b=>b.name==name && b.gameObject.scene.IsValid());
        void PayOff(){economy.TransferInPreparation(r.Loan+1000);Check(r.Repay(r.Loan) && r.Loan==0,"the whole loan is repaid in preparation");}
        void CloseResult(){Button("ContinueEndless").Button.onClick.Invoke();Check(!rewardsView.ResultOpen,"エンドレスで続ける closes the result");}
        bool spawning=spawner.enabled;spawner.enabled=false;
        try
        {
            // ---- opening ----
            GameScenes.NewGameMode=GameMode.Normal;campaign.NewGame();
            var opening=Story.PlayOpening();
            Check(opening!=null && opening.SceneCount==4 && opening.Index==0 && Story.Current==opening,"the opening plays four scenes from the first");
            Check(opening.Stage.childCount>0,"the first scene draws on the stage");
            opening.Advance();Check(opening.Index==0 && !opening.Finished,"a click first shows the whole caption");
            opening.Finish();Check(opening.Finished && Story.Current==null,"スキップ ends the opening");
            Check(clock.Phase==DayPhase.BeforeOpening && campaign.DayNumber==1,"after the opening the first day is in preparation");

            // ---- repaid early: keep playing, or the ending ----
            PayOff();
            Story.OfferEnding();
            Check(Story.Offering && Button("KeepPlaying")!=null && Button("GoToEnding")!=null,"repaying everything offers 続ける / エンディングへ");
            Check(Button("KeepPlaying").Text.Contains("20日目"),"normal mode: keep playing until day 20");
            Button("GoToEnding").Button.onClick.Invoke();
            Check(r.HasPendingResult && r.EndedEarly,"choosing the ending opens the result in preparation");
            var ending=Story.Current;
            Check(ending!=null && ending.name=="Ending" && ending.SceneCount==4,"the ending plays four scenes");
            ending.Finish();
            Check(Story.Current==null && rewardsView.ResultOpen,"after the ending the result (今回の成績) opens");
            CloseResult();
            Check(r.State.mode==GameMode.Endless && !r.HasPendingResult && clock.Phase==DayPhase.BeforeOpening && campaign.DayNumber==1,"エンドレスで続ける after an early ending stays in today's preparation");

            // ---- repaid, kept playing: the last day's settlement leads to the ending ----
            campaign.NewGame();PayOff();
            r.State.normalDays=campaign.DayNumber;
            campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1000);
            Check(clock.Phase==DayPhase.Ended && r.EndingDue,"the normal mode's last day with the loan repaid is due for the ending");
            settlement.RevealAll();settlement.NextButton.Button.onClick.Invoke();
            Check(Story.Current!=null && Story.Current.name=="Ending","エンディングへ on the settlement plays the ending");
            Story.Current.Finish();
            Check(rewardsView.ResultOpen,"the result follows the ending");
            CloseResult();

            // ---- not repaid by the last day: the result only ----
            campaign.NewGame();r.State.normalDays=campaign.DayNumber;
            campaign.StartBusiness();clock.Advance(clock.BusinessDuration+1000);
            if(!campaign.IsBankrupt)
            {
                Check(r.HasPendingResult && !r.EndingDue,"with the loan left the last day goes to the result, not the ending");
                settlement.RevealAll();settlement.NextButton.Button.onClick.Invoke();
                Check(Story.Current==null && rewardsView.ResultOpen,"the result opens without the ending");
                CloseResult();
            }
        }
        finally
        {
            Story.Current?.Finish();spawner.enabled=spawning;
            foreach(var leftover in UnityEngine.Object.FindObjectsByType<StoryPlayer>(FindObjectsSortMode.None))UnityEngine.Object.DestroyImmediate(leftover.gameObject);
            var choice=GameObject.Find("PayoffChoice");if(choice!=null)UnityEngine.Object.DestroyImmediate(choice);
        }
        return $"PASS: {count} story checks.";
    }
}
