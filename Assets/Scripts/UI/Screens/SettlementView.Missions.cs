using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Before the receipt fills in, the day's missions slide in and each completed one is stamped "達成！"
/// with its reward counting up. A click anywhere (or the skip/next buttons) ends it; with no completed mission it is skipped.</summary>
public sealed partial class SettlementView
{
    private RectTransform missionOverlay,missionPanel,missionHeader;
    private CanvasGroup missionGroup;
    private readonly List<(RectTransform row,Image face,TextMeshProUGUI mark,TextMeshProUGUI state,RectTransform stamp,TextMeshProUGUI reward,decimal amount,float at)> missionRows=new();
    private float missionStart,missionEnd;
    private const float MissionSlide=.4f,MissionRowGap=.22f,MissionStampGap=.65f,MissionHold=1.3f,MissionFade=.3f;
    public bool MissionCelebrationShown=>missionOverlay!=null;
    public int MissionCelebrationRows=>missionRows.Count;

    // Returns how long the receipt waits for the celebration (0 when nothing was completed today).
    private float BuildMissionCelebration(RectTransform root)
    {
        EndMissionCelebration();
        var missions=campaign.Rewards.TodayMissions;
        if(campaign.IsBankrupt || missions==null || !missions.Any(m=>m.completed))return 0;
        float w=root.rect.width,h=root.rect.height;
        missionOverlay=UiKit.Box(root,"MissionCelebration",0,0,w,h);missionOverlay.SetAsLastSibling();
        missionGroup=UiKit.Group(missionOverlay);
        var dim=UiKit.Fill(missionOverlay,new Color(.12f,.08f,.05f,.62f),0,true);
        var skip=missionOverlay.gameObject.AddComponent<Button>();skip.targetGraphic=dim;skip.transition=Selectable.Transition.None;
        skip.onClick.AddListener(SkipMissionCelebration);

        int count=missions.Count,done=missions.Count(m=>m.completed);
        float panelH=230+count*128;
        missionPanel=UiKit.Card(missionOverlay,"Panel",(w-1040)/2,(h-panelH)/2,1040,panelH,T.paper,24,10,6,T.tomato);
        Centered(missionPanel);
        missionHeader=UiKit.Box(missionPanel,"Header",0,0,1040,110);UiKit.Fill(missionHeader,T.tomato,20);
        UiKit.Label(missionHeader,"Title","ミッション達成！",0,0,1040,110,T.titleSize,Color.white,FontRole.Display,TextAlignmentOptions.Center);
        Centered(missionHeader);
        decimal total=missions.Where(m=>m.completed).Sum(m=>CampaignSaveManager.Decimal(m.cashReward));
        UiKit.Label(missionPanel,"Summary",$"{done} / {count} 件達成　報酬 {UiKit.Yen(total)} を受け取りました",40,124,960,48,T.subheadingSize,T.line,FontRole.Black,TextAlignmentOptions.Center);

        missionStart=Time.unscaledTime;missionRows.Clear();
        float stampAt=MissionSlide+count*MissionRowGap+.25f;
        for(int i=0;i<count;i++)
        {
            var m=missions[i];
            var row=UiKit.Card(missionPanel,"Mission_"+m.id,40,184+i*128,960,112,T.paperDeep,16,3,2);
            var mark=UiKit.Label(row,"Mark","☆",16,0,72,112,48,T.inkSub,FontRole.Display,TextAlignmentOptions.Center);
            UiKit.Label(row,"Title",campaign.Rewards.MissionLabel(m),100,12,560,52,T.subheadingSize,T.line,FontRole.Black);
            var state=UiKit.Label(row,"State",m.completed?"":"未達成",100,62,400,38,T.bodySize,T.inkSub,FontRole.Bold);
            decimal amount=m.completed?CampaignSaveManager.Decimal(m.cashReward):0;
            var reward=UiKit.Label(row,"Reward",m.completed?"":UiKit.Yen(CampaignSaveManager.Decimal(m.cashReward)),640,0,290,112,T.headingSize,m.completed?T.basil:T.disabled,FontRole.Display,TextAlignmentOptions.MidlineRight);
            RectTransform stamp=null;
            if(m.completed)
            {
                stamp=UiKit.Card(row,"Stamp",470,14,170,84,new Color(1,1,1,.85f),12,0,5,T.tomato);
                UiKit.Label(stamp,"Text","達成！",0,0,170,84,T.headingSize,T.tomato,FontRole.Display,TextAlignmentOptions.Center);
                Centered(stamp);stamp.gameObject.SetActive(false);
            }
            Centered(row);row.gameObject.SetActive(false);
            missionRows.Add((row,UiKit.FaceOf(row),mark,state,stamp,reward,amount,m.completed?stampAt:-1));
            if(m.completed)stampAt+=MissionStampGap;
        }
        UiKit.Label(missionPanel,"Hint","クリックで次へ",40,panelH-44,960,32,T.captionSize,T.inkSub,FontRole.Black,TextAlignmentOptions.Center);
        missionEnd=missionStart+stampAt+MissionHold;
        AnimateMissionCelebration();
        return missionEnd+MissionFade-missionStart;
    }
    private static void Centered(RectTransform r)
    {
        var size=r.sizeDelta;r.pivot=new Vector2(.5f,.5f);r.anchoredPosition+=new Vector2(size.x/2,-size.y/2);
    }
    private void AnimateMissionCelebration()
    {
        if(missionOverlay==null)return;
        float t=Time.unscaledTime-missionStart;
        float pop=Mathf.Clamp01(t/.35f);
        missionPanel.localScale=Vector3.one*(.85f+.15f*pop+.08f*Mathf.Sin(pop*Mathf.PI));
        missionHeader.localScale=Vector3.one*(1+.06f*Mathf.Sin(t*5)*Mathf.Clamp01(1-(t-2)/2));
        for(int i=0;i<missionRows.Count;i++)
        {
            var r=missionRows[i];
            float s=Mathf.Clamp01((t-MissionSlide-i*MissionRowGap)/.3f);
            r.row.gameObject.SetActive(s>0);
            var p=r.row.anchoredPosition;r.row.anchoredPosition=new Vector2(40+480+(1-s)*(1-s)*600,p.y);
            if(r.at<0)continue;
            float k=t-r.at;
            bool stamped=k>=0;
            if(stamped && !r.stamp.gameObject.activeSelf)GameAudio.Play("stamp");
            r.stamp.gameObject.SetActive(stamped);
            if(!stamped)continue;
            float hit=Mathf.Clamp01(k/.18f);
            r.stamp.localScale=Vector3.one*Mathf.Lerp(2.6f,1,hit*hit);
            r.stamp.localRotation=Quaternion.Euler(0,0,Mathf.Lerp(-30,-12,hit));
            r.row.localScale=Vector3.one*(1+.05f*Mathf.Sin(Mathf.Clamp01((k-.15f)/.25f)*Mathf.PI));
            r.face.color=T.basilLight;r.mark.text="★";r.mark.color=T.cheese;
            r.state.text="達成・報酬を受け取りました";r.state.color=T.basil;
            float c=Mathf.Clamp01((k-.1f)/.5f);
            r.reward.text="+"+UiKit.Yen(r.amount*(decimal)(1-(1-c)*(1-c)));
        }
        float fade=Mathf.Clamp01((Time.unscaledTime-missionEnd)/MissionFade);
        missionGroup.alpha=1-fade;
        if(fade>=1)EndMissionCelebration();
    }
    // Jumps to the end: the receipt starts filling in now.
    private void SkipMissionCelebration()
    {
        if(missionOverlay==null)return;
        EndMissionCelebration();shownAt=Time.unscaledTime;
    }
    private void EndMissionCelebration()
    {
        if(missionOverlay!=null)Destroy(missionOverlay.gameObject);
        missionOverlay=null;missionRows.Clear();
    }
}
