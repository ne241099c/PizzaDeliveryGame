using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>Provisional goal/reward UI. Presentation delegates all decisions to RewardProgression.</summary>
public sealed class RewardsView : MonoBehaviour
{
    private CampaignManager campaign;
    private ScreenRouter router;
    private RewardProgression rewards;
    private RectTransform overlay;
    private CanvasGroup panel,result,profile;
    private ShopModals.Modal modal,resultModal;
    private RectTransform body,endlessGoals;
    private TextMeshProUGUI grade,summary,loan,repayment,resultText;
    private StickerButton launcher,repayButton,repayAll,continueButton;
    private Slider repaySlider;
    private readonly List<StickerButton> missionButtons=new();
    private readonly List<StickerButton> repaymentButtons=new();
    private decimal selectedRepayment,availableRepayment;
    private float nextRefresh;
    private string missionSignature;
    private float sectionOffset;
    private readonly Dictionary<RectTransform,Vector2> bodyPositions=new();
    private UITheme T=>UiKit.T;
    public bool IsOpen=>panel!=null && panel.alpha>.5f;
    public bool ResultOpen=>result!=null && result.alpha>.5f;

    private void Start()
    {
        campaign=FindFirstObjectByType<CampaignManager>();
        router=GetComponent<ScreenRouter>();
        if(router==null)router=FindFirstObjectByType<ScreenRouter>();
        if(campaign==null || router==null)return;
        rewards=RewardProgression.Get(campaign);
        var canvas=router.Root(ScreenId.Hub).GetComponentInParent<Canvas>();
        if(canvas==null)return;
        overlay=UiKit.Stretch(canvas.transform,"RewardsOverlay");
        launcher=UiKit.Button(overlay,"GoalsButton",RewardStrings.Goals,StickerKind.Secondary,32,98,560,24,T.captionSize,Show);
        panel=NewPanel("GoalsModal");
        modal=ShopModals.Build(panel,overlay,1440,880,RewardStrings.Goals);
        UiKit.Label(modal.Card,"ProvisionalNotice",RewardStrings.Provisional,760,36,640,40,T.captionSize,T.inkSub,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        body=Scrollable(modal.Card,"GoalsScroll",40,108,1360,650,1500);
        BuildBody();
        ShopModals.Button(modal,"CloseGoals",RewardStrings.Close,StickerKind.Primary,1140,788,260,64,Close);
        result=NewPanel("NormalResult");
        resultModal=ShopModals.Build(result,overlay,1340,860,RewardStrings.Results);
        var resultBody=Scrollable(resultModal.Card,"ResultScroll",40,104,1260,608,800);
        resultText=UiKit.Label(resultBody,"ResultText","",0,0,1216,800,T.subheadingSize,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        continueButton=ShopModals.Button(resultModal,"ContinueEndless",RewardStrings.Continue,StickerKind.Primary,40,748,720,76,ContinueEndless);
        ShopModals.Button(resultModal,"ResultTitle",RewardStrings.Title,StickerKind.Secondary,820,748,480,76,ExitResult);
        router.Changed+=ScreenChanged;
        ScreenChanged(router.Current);
    }
    private CanvasGroup NewPanel(string name)
    {
        var root=UiKit.Stretch(overlay,name);
        root.gameObject.AddComponent<Image>();
        var group=UiKit.Group(root);
        UiKit.Show(group,false);
        return group;
    }
    private static RectTransform Scrollable(Transform parent,string name,float x,float y,float width,float height,float contentHeight)
    {
        var viewport=UiKit.Box(parent,name,x,y,width,height);
        viewport.gameObject.AddComponent<RectMask2D>();
        UiKit.Fill(viewport,UiKit.T.paper,0,true);
        var content=UiKit.Box(viewport,"Content",0,0,width-32,contentHeight);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=viewport;
        scroll.content=content;
        scroll.horizontal=false;
        scroll.vertical=true;
        ScrollNavigation.Configure(scroll);
        var track=UiKit.Box(viewport,"Scrollbar",width-24,0,16,height);
        UiKit.Fill(track,UiKit.T.paperDeep,8,true);
        var handle=UiKit.Stretch(track,"Handle");
        var image=UiKit.Fill(handle,UiKit.T.inkSub,8,true);
        var bar=track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect=handle;
        bar.targetGraphic=image;
        bar.direction=Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar=bar;
        scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        return content;
    }
    private TextMeshProUGUI Text(string name,string text,float y,float height,float size)
        =>UiKit.Label(body,name,text,0,y,1296,height,size,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
    private void BuildBody()
    {
        grade=Text("Grade","",0,56,T.headingSize);
        summary=Text("Progress","",64,160,T.bodySize);
        Text("MissionsHeading",RewardStrings.Missions,240,48,T.headingSize);
        Text("MissionHelp",RewardStrings.MissionHelp,292,66,T.bodySize);
        Text("LoanHeading",RewardStrings.Loan,660,48,T.headingSize);
        loan=Text("LoanState","",716,60,T.subheadingSize);
        Text("RepayHelp",RewardStrings.RepayHelp,780,66,T.bodySize);
        repayment=Text("Repayment","",856,42,T.subheadingSize);
        repaySlider=UiKit.Slider(body,"RepaymentSlider",16,924,1260,24,T.cheese,v=>
        {
            selectedRepayment=Math.Min(availableRepayment,BusinessEconomy.Money((decimal)v));
            repayment.text=RewardStrings.Repayment(selectedRepayment);
        });
        repaySlider.minValue=0;
        repaySlider.wholeNumbers=false;
        repaymentButtons.Add(UiKit.Button(body,"Repay100","100円",StickerKind.Secondary,0,984,200,64,T.subheadingSize,()=>Repay(100)));
        repaymentButtons.Add(UiKit.Button(body,"Repay500","500円",StickerKind.Secondary,216,984,200,64,T.subheadingSize,()=>Repay(500)));
        repayAll=UiKit.Button(body,"RepayAll",RewardStrings.RepayAll,StickerKind.Secondary,432,984,400,64,T.subheadingSize,()=>Repay(availableRepayment));
        repayButton=UiKit.Button(body,"RepaySelected",RewardStrings.Repay,StickerKind.Primary,848,984,448,64,T.subheadingSize,()=>Repay(selectedRepayment));
        endlessGoals=UiKit.Box(body,"EndlessGoals",0,1112,1296,160);
        UiKit.Label(endlessGoals,"FreeGoalHeading",RewardStrings.EndlessGoals,0,0,1296,48,T.headingSize,T.line);
        UiKit.Button(endlessGoals,"AssetsGoal",RewardStrings.AssetsGoal,StickerKind.Secondary,0,64,408,72,T.subheadingSize,()=>Goal("assets"));
        UiKit.Button(endlessGoals,"ReputationGoal",RewardStrings.ReputationGoal,StickerKind.Secondary,432,64,408,72,T.subheadingSize,()=>Goal("reputation"));
        UiKit.Button(endlessGoals,"StreakGoal",RewardStrings.StreakGoal,StickerKind.Secondary,864,64,432,72,T.subheadingSize,()=>Goal("streak"));
        Text("ProfileHeading",RewardStrings.Records,1296,48,T.headingSize);
        UiKit.Button(body,"ProfileDetails",RewardStrings.Records,StickerKind.Secondary,0,1360,600,72,T.subheadingSize,ShowProfile);
        foreach(RectTransform item in body)bodyPositions[item]=item.anchoredPosition;
    }
    private void OnDestroy()
    {
        if(router!=null)router.Changed-=ScreenChanged;
        if(overlay!=null)Destroy(overlay.gameObject);
    }
    private void Update()
    {
        if(rewards==null || launcher==null)return;
        if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(profile!=null && profile.alpha>.5f)UiKit.Show(profile,false);
            else if(IsOpen)Close();
        }
        if(Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.5f;
        launcher.Text=router.Current==ScreenId.Business
            ?RewardStrings.Hud(rewards.Loan,rewards.TodayMissions.Count(m=>m.selected))+(rewards.State.mode==GameMode.Endless?" / "+rewards.GoalLabel:""):RewardStrings.Goals+(rewards.HasNewAchievements?"・称号獲得！":"");
        if(IsOpen)Refresh();
    }
    private void ScreenChanged(ScreenId screen)
    {
        UiKit.Show(launcher,screen!=ScreenId.GameOver);
        UiKit.Place((RectTransform)launcher.transform,screen==ScreenId.Business?32:1490,screen==ScreenId.Business?98:screen==ScreenId.Hub?112:128,screen==ScreenId.Business?560:390,screen==ScreenId.Business?24:screen==ScreenId.Hub?34:52);
        Close();
        UiKit.Show(result,false);
        UiKit.Show(profile,false);
    }
    public void Show()
    {
        if(panel==null)return;
        modal.Title.text=RewardStrings.Goals;
        Refresh();
        panel.transform.SetAsLastSibling();
        UiKit.Show(panel,true);
    }
    public void Close()=>UiKit.Show(panel,false);
    private void Refresh()
    {
        grade.text=RewardStrings.Grade+"："+rewards.GradeName;
        UiKit.Show(endlessGoals,rewards.State.mode==GameMode.Endless);
        summary.text=rewards.NextGradeProgress+"\n"+(router.Current==ScreenId.Settlement?rewards.SettlementSummary:rewards.HudSummary);
        sectionOffset=Mathf.Max(0,summary.GetPreferredValues(summary.text,1296,0).y-160);
        summary.rectTransform.sizeDelta=new Vector2(1296,160+sectionOffset);
        foreach(var pair in bodyPositions)if(pair.Key!=null && pair.Value.y<=-240)pair.Key.anchoredPosition=pair.Value+new Vector2(0,-sectionOffset);
        for(int i=0;i<missionButtons.Count;i++)((RectTransform)missionButtons[i].transform).anchoredPosition=new Vector2(0,-370-i*88-sectionOffset);
        body.sizeDelta=new Vector2(body.sizeDelta.x,1500+sectionOffset);
        loan.text=$"ローン {UiKit.Yen(rewards.Loan)}　純資産 {UiKit.Yen(rewards.NetAssets)}";
        var economy=FindFirstObjectByType<BusinessEconomy>();
        decimal available=economy!=null?Math.Max(0,Math.Min(economy.Cash,rewards.Loan)):0;
        availableRepayment=available;
        repaySlider.maxValue=(float)available;
        selectedRepayment=Math.Min(selectedRepayment,available);
        repaySlider.SetValueWithoutNotify((float)selectedRepayment);
        repaySlider.interactable=rewards.CanRepay && available>0;
        repayment.text=RewardStrings.Repayment(selectedRepayment);
        repayButton.Interactable=repayAll.Interactable=rewards.CanRepay && available>0;
        repaymentButtons[0].Interactable=rewards.CanRepay && available>=100;
        repaymentButtons[1].Interactable=rewards.CanRepay && available>=500;
        string signature=string.Join("|",rewards.TodayMissions.Select(m=>m.id+":"+m.selected+":"+m.completed+":"+m.rewarded));
        if(signature!=missionSignature){missionSignature=signature;BuildMissions();}
        foreach(var button in missionButtons)button.Interactable=router.Current==ScreenId.Hub;
    }
    private void BuildMissions()
    {
        foreach(var button in missionButtons)
        {
            button.transform.SetParent(null,false);
            Destroy(button.gameObject);
        }
        missionButtons.Clear();
        int i=0;
        foreach(var mission in rewards.TodayMissions)
        {
            var item=mission;
            string mark=item.rewarded?"達成・受取済":item.completed?"達成":item.selected?"選択中":"選ぶ";
            string text=$"{mark}：{rewards.MissionLabel(item)}　報酬 {UiKit.Yen(CampaignSaveManager.Decimal(item.cashReward))} / {item.points}pt";
            missionButtons.Add(UiKit.Button(body,"Mission_"+item.id,text,item.selected?StickerKind.Selected:StickerKind.Secondary,
                0,370+i*88+sectionOffset,1296,72,T.bodySize,()=>{rewards.ToggleMission(item.id);Refresh();}));
            i++;
        }
    }
    private void Repay(decimal amount){rewards.Repay(amount);Refresh();}
    private void Goal(string id){rewards.SetEndlessGoal(id);Refresh();}
    public void ShowResult()
    {
        if(result==null || !rewards.HasPendingResult)return;
        Close();
        rewards.RecordResult();
        resultText.text=rewards.ResultSummary;
        resultText.rectTransform.sizeDelta=new Vector2(1216,Math.Max(608,resultText.GetPreferredValues(resultText.text,1216,0).y+32));
        ((RectTransform)resultText.transform.parent).sizeDelta=resultText.rectTransform.sizeDelta;
        resultText.ForceMeshUpdate();
        continueButton.Interactable=!campaign.IsBankrupt;
        result.transform.SetAsLastSibling();
        UiKit.Show(result,true);
    }
    private void ExitResult()
    {
        rewards.RecordResult();
        GameScenes.LoadTitle();
    }
    private void ContinueEndless()
    {
        rewards.ContinueEndless();
        UiKit.Show(result,false);
    }
    private void ShowProfile()
    {
        // The shared title/profile modal can also be opened without changing the campaign scene.
        if(profile!=null)Destroy(profile.gameObject);
        var group=NewPanel("CampaignProfile");
        profile=group;
        var card=ShopModals.Build(group,overlay,1340,860,RewardStrings.Records);
        var content=Scrollable(card.Card,"ProfileScroll",40,104,1260,608,1200);
        var text=UiKit.Label(content,"Profile",rewards.ProfileSummary,0,0,1216,1200,T.bodySize,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        float h=Math.Max(608,text.GetPreferredValues(text.text,1216,0).y+32);
        content.sizeDelta=new Vector2(1228,h);text.rectTransform.sizeDelta=new Vector2(1216,h);text.ForceMeshUpdate();
        ShopModals.Button(card,"CloseProfile",RewardStrings.Close,StickerKind.Primary,1000,748,300,76,()=>{UiKit.Show(group,false);Destroy(group.gameObject);});
        UiKit.Show(group,true);
    }
}
