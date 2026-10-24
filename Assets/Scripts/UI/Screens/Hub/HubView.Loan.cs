using System;
using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>The loan and today's missions on the 開店準備 screen (2026-10-24): the top-bar loan gauge and the
/// 「ローン・ミッション」 tab. The loan is repaid here only, during preparation (RewardProgression.CanRepay).</summary>
public sealed partial class HubView
{
    public const int LoanTab=4;
    [Header("Loan")]
    [SerializeField] private StickerButton loanButton;
    [SerializeField] private TextMeshProUGUI loanAmount,loanCaption;
    [SerializeField] private UiBar loanBar;
    private RewardProgression Rewards=>campaign.Rewards;

    private void ShowLoanGauge()
    {
        if(loanButton==null)return;
        var r=Rewards;
        loanAmount.text=r.Loan>0?UiKit.Yen(r.Loan):"完済！";
        loanBar.Set(r.LoanProgress,-1,r.Loan>0?T.basil:T.cheese);
        loanCaption.text=r.Loan>0?$"返済 {r.LoanProgress*100:0}%　利息 {UiKit.Yen(r.InterestFor(r.Loan))}/日"+(r.DaysLeft>0?$"　あと{r.DaysLeft}日":""):"ローン完済";
    }
    private string LoanSignature()
    {
        var r=Rewards;
        return r.Loan+"|"+r.CanRepay+"|"+string.Join("/",r.TodayMissions.Select(m=>m.id+m.completed+m.rewarded));
    }
    /// <summary>Today's fixed spending: wages, fixed costs and maintenance (materials and fuel come on top).</summary>
    private decimal TodayCosts=>economy.TeamWages+economy.FixedCost(campaign.DayNumber);
    private decimal KeepCostsAmount=>BusinessEconomy.Money(Math.Max(0,Math.Min(Rewards.Loan,economy.Cash-TodayCosts)));

    private void BuildLoan()
    {
        var r=Rewards;
        ShowLoanGauge();
        // ---- loan ----
        var card=UiKit.Card(content,"LoanCard",0,0,780,610,T.paper);
        UiKit.Label(card,"Heading",RewardStrings.LoanState,32,22,400,48,T.headingSize,T.line,FontRole.Display);
        UiKit.Label(card,"RemainingTitle","残り",32,84,100,40,T.subheadingSize,T.inkSub,FontRole.Black);
        UiKit.Label(card,"Remaining",r.Loan>0?UiKit.Yen(r.Loan):"完済！",120,64,620,84,72,r.Loan>0?T.line:T.basil,FontRole.Display);
        var bar=UiKit.Bar(card,"Progress",32,166,716,36,T.basil);bar.Set(r.LoanProgress);
        UiKit.Label(card,"ProgressText",$"返済済み {UiKit.Yen(r.Repaid)} / 借りた額 {UiKit.Yen(r.Borrowed)}（{r.LoanProgress*100:0}%）",32,212,716,36,T.subheadingSize,T.line,FontRole.Black);
        UiKit.Label(card,"Interest",$"利息：毎日の精算で残りの0.5%（今日 {UiKit.Yen(r.InterestFor(r.Loan))}）・完済ボーナス {UiKit.Yen(r.PayoffBonus)}",32,252,716,32,T.bodySize,T.inkSub,FontRole.Bold);
        UiKit.Label(card,"Deadline",r.DaysLeft>0?$"{r.State.normalDays}日目の精算までに完済しよう　あと{r.DaysLeft}日":r.State.mode==GameMode.Normal?"最終日です":"エンドレス：返済は自由です",32,288,716,32,T.bodySize,r.DaysLeft>0 && r.DaysLeft<=3?T.berry:T.line,FontRole.Black);
        decimal oneThousand=Math.Min(1000,Math.Min(r.Loan,economy.Cash)),keep=KeepCostsAmount,all=Math.Max(0,Math.Min(r.Loan,economy.Cash));
        Repayment(card,"Repay1000",$"{UiKit.Yen(oneThousand)} 返す",32,196,oneThousand);
        Repayment(card,"RepayKeep","費用を残して返す",244,264,keep);
        Repayment(card,"RepayAll","返せるだけ返す",524,224,all);
        UiKit.Label(card,"Costs",$"所持金 {UiKit.Yen(economy.Cash)}　今日の費用の見込み {UiKit.Yen(TodayCosts)}（日給・固定費・維持費）",32,440,716,32,T.bodySize,T.line,FontRole.Bold);
        UiKit.Label(card,"KeepHelp",$"「費用を残して返す」は {UiKit.Yen(keep)}。材料費・燃料費の分も少し残しておくと安心です。",32,476,716,56,T.captionSize,T.inkSub,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        UiKit.Label(card,"When",r.CanRepay || r.Loan<=0?"返済は開店準備の間だけできます。":"いまは返済できません（開店準備の間だけ）。",32,546,716,32,T.captionSize,T.inkSub,FontRole.Black);
        // ---- missions ----
        var missions=UiKit.Card(content,"MissionCard",808,0,780,610,T.paper);
        UiKit.Label(missions,"Heading",RewardStrings.Missions,32,22,500,48,T.headingSize,T.line,FontRole.Display);
        UiKit.Label(missions,"Help","全部が対象です。達成すると、精算のときに報酬を自動で受け取ります。",32,78,716,32,T.bodySize,T.inkSub,FontRole.Bold);
        int i=0;
        foreach(var m in r.TodayMissions)
        {
            var row=UiKit.Card(missions,"Mission_"+m.id,32,126+i*124,716,108,m.completed?T.basilLight:T.paperDeep,16,2,2);
            UiKit.Label(row,"Mark",m.completed?"★":"☆",20,0,60,108,44,m.completed?T.basil:T.inkSub,FontRole.Display,TextAlignmentOptions.Center);
            UiKit.Label(row,"Title",r.MissionLabel(m),88,14,460,48,T.subheadingSize,T.line,FontRole.Black);
            UiKit.Label(row,"State",m.rewarded?"達成・受け取り済み":m.completed?"達成":"挑戦中",88,58,460,36,T.bodySize,m.completed?T.basil:T.inkSub,FontRole.Bold);
            UiKit.Label(row,"Reward",UiKit.Yen(CampaignSaveManager.Decimal(m.cashReward)),540,0,156,108,T.headingSize,T.line,FontRole.Display,TextAlignmentOptions.MidlineRight);
            i++;
        }
        UiKit.Label(missions,"Total",$"これまでに達成したミッション：{r.Profile.missionsCompleted}回",32,512,716,36,T.bodySize,T.line,FontRole.Black);
        UiKit.Label(missions,"Grade",$"店のグレード：{r.GradeName}",32,552,716,32,T.captionSize,T.inkSub,FontRole.Black);
    }
    private void Repayment(RectTransform card,string name,string label,float x,float width,decimal amount)
    {
        var button=UiKit.Button(card,name,label,name=="RepayAll"?StickerKind.Primary:StickerKind.Secondary,x,354,width,68,T.subheadingSize,()=>{bool owed=Rewards.Loan>0;if(Rewards.Repay(amount)){RefreshNow();if(owed && Rewards.Loan==0)Story.OfferEnding();}});
        button.Interactable=Rewards.CanRepay && amount>0;
    }
}
