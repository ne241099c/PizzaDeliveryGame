using UnityEngine;

/// <summary>When the opening and the ending play. The opening: a new game started from the title. The ending: chosen when
/// the loan is fully repaid (or kept for later), and at the end of the normal mode's last day with the loan repaid. After
/// the ending the usual result (今回の成績) follows, with エンドレスで続ける / タイトルへ.</summary>
public static class Story
{
    public static StoryPlayer Current {get;private set;}
    public static StoryPlayer PlayOpening()
    {
        var rewards=Object.FindFirstObjectByType<CampaignManager>().Rewards;
        Current=StoryPlayer.Play("Opening",StoryScenes.Opening(rewards.State.mode,rewards.Loan,rewards.State.normalDays),()=>Current=null);
        return Current;
    }
    public static StoryPlayer PlayEnding(System.Action then)
    {
        var campaign=Object.FindFirstObjectByType<CampaignManager>();var r=campaign.Rewards;
        Current=StoryPlayer.Play("Ending",StoryScenes.Ending(r.Borrowed,campaign.DayNumber,r.State.totalDelivered,r.GradeName,r.State.grade,campaign.TotalRevenue),()=>{Current=null;then?.Invoke();});
        return Current;
    }
    /// <summary>Asked once, right after the repayment that brings the loan to ¥0.</summary>
    public static void OfferEnding()
    {
        var campaign=Object.FindFirstObjectByType<CampaignManager>();
        var overlay=Object.FindFirstObjectByType<ScreenRouter>().Root(ScreenId.Hub).GetComponentInParent<Canvas>().rootCanvas.transform;
        var go=new GameObject("PayoffChoice",typeof(RectTransform),typeof(CanvasGroup),typeof(UnityEngine.UI.Image));go.layer=5;
        var panel=go.GetComponent<CanvasGroup>();
        var modal=ShopModals.Build(panel,overlay,1180,560,"ローン完済！");
        bool normal=campaign.Rewards.State.mode==GameMode.Normal;
        ShopModals.Body(modal,110,220,(normal?$"おめでとうございます。借りたお金をすべて返しました（完済ボーナス {UiKit.Yen(campaign.Rewards.PayoffBonus)}）。\n\n{campaign.Rewards.State.normalDays}日目まで経営を続けるか、ここでエンディングを見るか選べます。続けた場合も、{campaign.Rewards.State.normalDays}日目の精算のあとにエンディングが流れます。":$"おめでとうございます。借りたお金をすべて返しました（完済ボーナス {UiKit.Yen(campaign.Rewards.PayoffBonus)}）。\n\nこのまま経営を続けるか、ここでエンディングを見るか選べます。"),26);
        ShopModals.Button(modal,"KeepPlaying",normal?$"{campaign.Rewards.State.normalDays}日目まで続ける":"このまま続ける",StickerKind.Secondary,40,440,520,88,()=>Object.Destroy(go));
        ShopModals.Button(modal,"GoToEnding","エンディングへ ▶",StickerKind.Primary,620,440,520,88,()=>
        {
            Object.Destroy(go);campaign.Rewards.ChooseEnding();
            PlayEnding(()=>Object.FindFirstObjectByType<RewardsView>().ShowResult());
        });
    }
    public static bool Offering=>GameObject.Find("PayoffChoice")!=null;
}
