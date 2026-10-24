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
    private TextMeshProUGUI grade,summary,loan,resultText;
    private StickerButton continueButton;
    private readonly List<TextMeshProUGUI> missionRows=new();
    private Action onClose;
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
        // Opened from the ≡ menu during business; in preparation the 開店準備 tab shows the loan and missions instead.
        panel=NewPanel("GoalsModal");
        modal=ShopModals.Build(panel,overlay,1440,880,RewardStrings.Goals);
        UiKit.Label(modal.Card,"ProvisionalNotice",RewardStrings.Provisional,760,36,640,40,T.captionSize,T.inkSub,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        body=Scrollable(modal.Card,"GoalsScroll",40,108,1360,650,1260);
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
        Text("LoanHeading",RewardStrings.LoanState,660,48,T.headingSize);
        loan=Text("LoanState","",716,60,T.subheadingSize);
        Text("RepayHelp",RewardStrings.RepayWhere,780,66,T.bodySize);
        endlessGoals=UiKit.Box(body,"EndlessGoals",0,872,1296,160);
        UiKit.Label(endlessGoals,"FreeGoalHeading",RewardStrings.EndlessGoals,0,0,1296,48,T.headingSize,T.line);
        UiKit.Button(endlessGoals,"AssetsGoal",RewardStrings.AssetsGoal,StickerKind.Secondary,0,64,408,72,T.subheadingSize,()=>Goal("assets"));
        UiKit.Button(endlessGoals,"ReputationGoal",RewardStrings.ReputationGoal,StickerKind.Secondary,432,64,408,72,T.subheadingSize,()=>Goal("reputation"));
        UiKit.Button(endlessGoals,"StreakGoal",RewardStrings.StreakGoal,StickerKind.Secondary,864,64,432,72,T.subheadingSize,()=>Goal("streak"));
        Text("ProfileHeading",RewardStrings.Records,1056,48,T.headingSize);
        UiKit.Button(body,"ProfileDetails",RewardStrings.Records,StickerKind.Secondary,0,1120,600,72,T.subheadingSize,ShowProfile);
        foreach(RectTransform item in body)bodyPositions[item]=item.anchoredPosition;
    }
    private void OnDestroy()
    {
        if(router!=null)router.Changed-=ScreenChanged;
        if(overlay!=null)Destroy(overlay.gameObject);
    }
    private void Update()
    {
        if(rewards==null || panel==null)return;
        if(Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(profile!=null && profile.alpha>.5f)UiKit.Show(profile,false);
            else if(IsOpen)Close();
        }
        if(Time.unscaledTime<nextRefresh)return;
        nextRefresh=Time.unscaledTime+.5f;
        if(IsOpen)Refresh();
    }
    private void ScreenChanged(ScreenId screen)
    {
        Close();
        UiKit.Show(result,false);
        UiKit.Show(profile,false);
    }
    public void Show(Action closed=null)
    {
        if(panel==null)return;
        onClose=closed;
        modal.Title.text=RewardStrings.Goals;
        Refresh();
        panel.transform.SetAsLastSibling();
        UiKit.Show(panel,true);
    }
    public void Close()
    {
        bool wasOpen=IsOpen;UiKit.Show(panel,false);
        var callback=onClose;onClose=null;if(wasOpen)callback?.Invoke();
    }
    private void Refresh()
    {
        grade.text=RewardStrings.Grade+"："+rewards.GradeName;
        UiKit.Show(endlessGoals,rewards.State.mode==GameMode.Endless);
        summary.text=rewards.NextGradeProgress+"\n"+(router.Current==ScreenId.Settlement?rewards.SettlementSummary:rewards.HudSummary);
        sectionOffset=Mathf.Max(0,summary.GetPreferredValues(summary.text,1296,0).y-160);
        summary.rectTransform.sizeDelta=new Vector2(1296,160+sectionOffset);
        foreach(var pair in bodyPositions)if(pair.Key!=null && pair.Value.y<=-240)pair.Key.anchoredPosition=pair.Value+new Vector2(0,-sectionOffset);
        for(int i=0;i<missionRows.Count;i++)missionRows[i].rectTransform.anchoredPosition=new Vector2(0,-370-i*88-sectionOffset);
        body.sizeDelta=new Vector2(body.sizeDelta.x,1260+sectionOffset);
        loan.text=RewardStrings.LoanLine(rewards);
        string signature=string.Join("|",rewards.TodayMissions.Select(m=>m.id+":"+m.completed+":"+m.rewarded));
        if(signature!=missionSignature){missionSignature=signature;BuildMissions();}
    }
    private void BuildMissions()
    {
        foreach(var row in missionRows)
        {
            row.transform.SetParent(null,false);
            Destroy(row.gameObject);
        }
        missionRows.Clear();
        int i=0;
        // Every mission is active; achieved ones are paid automatically at settlement.
        foreach(var item in rewards.TodayMissions)
        {
            string mark=item.rewarded?"★ 達成・受け取り済み":item.completed?"★ 達成":"☆ 挑戦中";
            string text=$"{mark}　{rewards.MissionLabel(item)}　報酬 {UiKit.Yen(CampaignSaveManager.Decimal(item.cashReward))}";
            var row=UiKit.Label(body,"Mission_"+item.id,text,0,370+i*88+sectionOffset,1296,72,T.bodySize,item.rewarded?T.basil:T.line,FontRole.Black);
            missionRows.Add(row);
            i++;
        }
    }
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
        // After the ending chosen in preparation the run goes on as endless when continued from the title.
        if(rewards.EndedEarly)rewards.ContinueEndless();else rewards.RecordResult();
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
