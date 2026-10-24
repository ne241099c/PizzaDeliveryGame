using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A tutorial chapter (S2-T, chosen on the title; never forced on day 1). Runs a new game that is never saved
/// (CampaignSaveManager turns saving off), opens the shop with scripted orders, and guides the player step by step:
/// a coach card with the instruction and a pulsing frame around the part to use. Steps finish when the player does the
/// action (or presses 次へ on explanation steps); the clock stops on explanation steps. Chapters: TutorialDirector.Chapters.cs.</summary>
public sealed partial class TutorialDirector : MonoBehaviour
{
    public const int ChapterCount=3;
    public static readonly string[] ChapterTitles={"作る","配達する","内勤と作り直し"};
    public int Chapter {get;set;}=1;
    public int StepIndex {get;private set;}
    public int StepCount=>steps.Count;
    public bool Finished {get;private set;}
    public string CurrentText=>StepIndex<steps.Count?steps[StepIndex].Text:"";
    public static TutorialDirector Active {get;private set;}

    private sealed class Step
    {
        public string Text;
        public Func<IEnumerable<RectTransform>> Targets;
        public Func<bool> Done;
        public Action Enter;
        /// <summary>An explanation: the clock stops and the player presses 次へ.</summary>
        public bool Manual;
        /// <summary>Runs every frame while the step is shown (scripted kitchen actions).</summary>
        public Action Tick;
    }
    private readonly List<Step> steps=new();
    private CampaignManager campaign;
    private GameClock clock;
    private BusinessView business;
    private ScreenRouter router;
    private GameMenu menu;
    private RectTransform overlay,coach,frames;
    private TextMeshProUGUI coachTitle,coachText;
    private StickerButton nextButton,quitButton,secondButton;
    private readonly List<RectTransform> frameList=new();
    private bool started;
    private float nextLookup;
    private RectTransform[] targets;
    private static UITheme T=>UiKit.T;

    private void Awake()=>Active=this;
    private void OnDestroy()
    {
        if(Active==this)Active=null;
        if(overlay!=null)Destroy(overlay.gameObject);
        if(clock!=null)clock.EndTutorialBusiness();
        var cooking=FindFirstObjectByType<CookingManager>();if(onDiscard!=null && cooking!=null)cooking.PizzaDiscarded-=onDiscard;
    }
    private void Start()=>Begin();
    /// <summary>Sets the chapter up (once; Start calls it, validations may call it earlier).</summary>
    public void Begin()
    {
        if(started)return;
        campaign=FindFirstObjectByType<CampaignManager>();clock=FindFirstObjectByType<GameClock>();
        business=FindFirstObjectByType<BusinessView>(FindObjectsInactive.Include);router=FindFirstObjectByType<ScreenRouter>();
        menu=FindFirstObjectByType<GameMenu>();
        if(campaign==null || clock==null || business==null || router==null){Debug.LogWarning("Tutorial: the Game scene is missing its managers.");return;}
        BuildOverlay();
        clock.ExtendBusinessForTutorial(3600);
        Chapter=Mathf.Clamp(Chapter,1,ChapterCount);
        switch(Chapter){case 1:ChapterCooking();break;case 2:ChapterDelivery();break;default:ChapterRecovery();break;}
        started=true;Enter(0);
    }

    // ---- steps ----
    private void Add(string text,Func<bool> done,Func<IEnumerable<RectTransform>> targets=null,Action enter=null)
        =>steps.Add(new Step{Text=text,Done=done,Targets=targets,Enter=enter});
    private void Explain(string text,Func<IEnumerable<RectTransform>> targets=null,Action enter=null)
        =>steps.Add(new Step{Text=text,Manual=true,Targets=targets,Enter=enter});
    private void Enter(int index)
    {
        StepIndex=index;
        if(index>=steps.Count){Finish();return;}
        var step=steps[index];step.Enter?.Invoke();nextLookup=0;
        clock.AutomaticTick=!step.Manual;
        coachTitle.text=$"チュートリアル {Chapter}. {ChapterTitles[Chapter-1]}　{index+1}/{steps.Count}";
        coachText.text=step.Text;
        UiKit.Show(nextButton,step.Manual);nextButton.Text="次へ ▶";
        UiKit.Show(secondButton,false);
    }
    /// <summary>Moves on from an explanation step (the 次へ button; validations call it too).</summary>
    public void Next()
    {
        if(Finished){NextChapter();return;}
        if(started && StepIndex<steps.Count && steps[StepIndex].Manual)Enter(StepIndex+1);
    }
    private void Update()=>Poll();
    /// <summary>Checks the current step and updates the frames (every frame; validations call it directly).</summary>
    public void Poll()
    {
        if(!started || overlay==null)return;
        bool covered=menu!=null && (menu.MenuOpen || menu.HelpOpen || menu.ConfirmationOpen);
        overlay.gameObject.SetActive(!covered);
        if(!Finished && StepIndex<steps.Count)
        {
            var step=steps[StepIndex];step.Tick?.Invoke();
            if(!step.Manual && step.Done!=null && step.Done())Enter(StepIndex+1);
        }
        if(covered)return;
        // Looking the parts up is not free: refresh the frames' targets five times a second.
        if(Time.unscaledTime>=nextLookup){nextLookup=Time.unscaledTime+.2f;targets=Finished || StepIndex>=steps.Count?null:steps[StepIndex].Targets?.Invoke()?.ToArray();}
        Highlight(targets);
    }
    private void Finish()
    {
        Finished=true;clock.AutomaticTick=false;
        coachTitle.text=$"チュートリアル {Chapter}. {ChapterTitles[Chapter-1]}　完了！";
        coachText.text=Chapter<ChapterCount?$"よくできました！ 次は「{Chapter+1}. {ChapterTitles[Chapter]}」です。":"これでチュートリアルはおしまいです。タイトルの「最初から」で本番の営業を始めましょう。";
        UiKit.Show(nextButton,Chapter<ChapterCount);nextButton.Text="次の章へ ▶";
        UiKit.Show(secondButton,true);UiKit.Show(quitButton,false);
        targets=null;Highlight(null);
    }
    private void NextChapter(){if(Chapter<ChapterCount)GameScenes.LoadTutorial(Chapter+1);}

    // ---- overlay ----
    private void BuildOverlay()
    {
        var canvas=router.Root(ScreenId.Business).GetComponentInParent<Canvas>();
        overlay=UiKit.Stretch(canvas.rootCanvas.transform,"TutorialOverlay");
        frames=UiKit.Stretch(overlay,"Frames");
        coach=UiKit.Card(overlay,"TutorialCoach",0,0,660,236,T.paper,18,6,3,T.tomato);
        coachTitle=UiKit.Label(coach,"Title","",28,16,604,32,T.bodySize,T.tomato,FontRole.Black);
        coachText=UiKit.Label(coach,"Text","",28,52,604,112,T.subheadingSize,T.line,FontRole.Black,TextAlignmentOptions.TopLeft,true);
        nextButton=UiKit.Button(coach,"TutorialNext","次へ ▶",StickerKind.Primary,404,166,228,56,T.subheadingSize,Next);
        secondButton=UiKit.Button(coach,"TutorialTitle","タイトルへ",StickerKind.Secondary,176,166,212,56,T.bodySize,()=>GameScenes.LoadTitle());
        quitButton=UiKit.Button(coach,"TutorialQuit","やめる",StickerKind.Secondary,28,166,132,56,T.bodySize,()=>GameScenes.LoadTitle());
        overlay.SetAsLastSibling();
    }
    private void Highlight(IEnumerable<RectTransform> targets)
    {
        var rects=new List<Rect>();
        if(targets!=null)foreach(var t in targets)if(t!=null && t.gameObject.activeInHierarchy && Local(t,out var r))rects.Add(r);
        while(frameList.Count<rects.Count)frameList.Add(NewFrame());
        float pulse=.55f+.45f*Mathf.Sin(Time.unscaledTime*6);
        for(int i=0;i<frameList.Count;i++)
        {
            bool shown=i<rects.Count;frameList[i].gameObject.SetActive(shown);if(!shown)continue;
            var r=rects[i];UiKit.Place(frameList[i],r.x-10,r.y-10,r.width+20,r.height+20);
            foreach(var g in frameList[i].GetComponentsInChildren<Image>())g.color=new Color(T.tomato.r,T.tomato.g,T.tomato.b,pulse);
        }
        PlaceCoach(rects);
    }
    private RectTransform NewFrame()
    {
        var f=UiKit.Box(frames,"Frame",0,0,10,10);
        foreach(var (name,min,max) in new[]{("Top",new Vector2(0,1),new Vector2(1,1)),("Bottom",new Vector2(0,0),new Vector2(1,0)),("Left",new Vector2(0,0),new Vector2(0,1)),("Right",new Vector2(1,0),new Vector2(1,1))})
        {
            var side=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<RectTransform>();side.SetParent(f,false);
            side.anchorMin=min;side.anchorMax=max;side.pivot=new Vector2(.5f,.5f);
            bool horizontal=name=="Top" || name=="Bottom";side.sizeDelta=horizontal?new Vector2(0,8):new Vector2(8,0);side.anchoredPosition=Vector2.zero;
            side.GetComponent<Image>().raycastTarget=false;
        }
        return f;
    }
    /// <summary>How many parts the current step points at (validations check the frames find their targets).</summary>
    public int TargetCount=>targets?.Count(t=>t!=null)??0;
    public RectTransform Coach=>coach;
    /// <summary>The target's rectangle in overlay coordinates (x, y from the top left), or false when off screen.</summary>
    private bool Local(RectTransform target,out Rect rect)
    {
        var corners=new Vector3[4];target.GetWorldCorners(corners);
        var canvas=overlay.GetComponentInParent<Canvas>();var cam=canvas.renderMode==RenderMode.ScreenSpaceOverlay?null:canvas.worldCamera;
        Vector2 min=new(float.MaxValue,float.MaxValue),max=new(float.MinValue,float.MinValue);
        foreach(var c in corners)
        {
            var screen=RectTransformUtility.WorldToScreenPoint(cam,c);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(overlay,screen,cam,out var p);
            min=Vector2.Min(min,p);max=Vector2.Max(max,p);
        }
        var o=overlay.rect;
        rect=new Rect(min.x-o.xMin,o.yMax-max.y,max.x-min.x,max.y-min.y);
        return rect.width>1 && rect.height>1 && rect.xMax>0 && rect.yMax>0 && rect.x<o.width && rect.y<o.height;
    }
    private void PlaceCoach(List<Rect> avoid)
    {
        float w=overlay.rect.width,h=overlay.rect.height,cw=coach.sizeDelta.x,ch=coach.sizeDelta.y;
        var spots=new[]{new Vector2(w-cw-32,h-ch-32),new Vector2(32,h-ch-32),new Vector2(w-cw-32,132),new Vector2(32,132),new Vector2((w-cw)/2,(h-ch)/2)};
        Vector2 best=spots[0];float bestOverlap=float.MaxValue;
        foreach(var s in spots)
        {
            var r=new Rect(s.x,s.y,cw,ch);float overlap=avoid.Sum(a=>Overlap(a,r));
            if(overlap<bestOverlap-1){bestOverlap=overlap;best=s;}
        }
        UiKit.Place(coach,best.x,best.y,cw,ch);
    }
    private static float Overlap(Rect a,Rect b)
    {
        float x=Mathf.Max(0,Mathf.Min(a.xMax,b.xMax)-Mathf.Max(a.xMin,b.xMin)),y=Mathf.Max(0,Mathf.Min(a.yMax,b.yMax)-Mathf.Max(a.yMin,b.yMin));
        return x*y;
    }
    /// <summary>A named part of the shop screen (the first match under the business view), or null.</summary>
    private RectTransform Part(string name)=>business.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(t=>t.name==name && t.gameObject.activeInHierarchy);
    private IEnumerable<RectTransform> Parts(params string[] names)=>names.Select(Part).Where(t=>t!=null);
}
