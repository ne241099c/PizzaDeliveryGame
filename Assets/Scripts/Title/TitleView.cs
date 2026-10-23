using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The title screen (設計書_画面デザイン D-5a / 4-1, approved mock Docs/UI_Mock/Title.png).
/// Built as a prefab (Assets/Prefabs/UI/TitleView.prefab) whose parts are referenced here, so the layout and the art can
/// be edited in the editor. This component only shows state: GameMenu and CampaignSaveManager own the actions.</summary>
public sealed class TitleView : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private StickerButton continueButton;
    [SerializeField] private StickerButton newGameButton,tutorialButton,helpButton,quitButton;
    [SerializeField] private Button creditsButton;
    [SerializeField] private TextMeshProUGUI continueSummary,tutorialNote;
    [SerializeField] private RectTransform firstTimeBadge;
    [Header("Text")]
    /// <summary>Logo lines: [0] the small first line, then the large words. Each shadow is a copy drawn behind its line.</summary>
    [SerializeField] private TextMeshProUGUI[] logoLines,logoShadows;
    [SerializeField] private TextMeshProUGUI kana,footer,status,records;
    [Header("Motion (stops when DeliveryFeedback animations are off)")]
    [SerializeField] private Image[] bulbGlows;
    [SerializeField] private Image windowGlow;
    [SerializeField, Min(.1f)] private float twinkleSpeed=1.6f;
    [SerializeField, Range(0,1)] private float glowBase=.55f,glowRange=.35f,windowBase=.24f,windowRange=.08f;

    public StickerButton ContinueButton=>continueButton;
    public StickerButton NewGameButton=>newGameButton;
    public StickerButton TutorialButton=>tutorialButton;
    public StickerButton HelpButton=>helpButton;
    public StickerButton QuitButton=>quitButton;
    public Button CreditsButton=>creditsButton;
    public bool ShowsContinue=>continueButton!=null && continueButton.gameObject.activeSelf;
    public bool ShowsFirstTimeBadge=>firstTimeBadge!=null && firstTimeBadge.gameObject.activeSelf;
    public string Status=>status!=null && status.transform.parent.gameObject.activeSelf?status.text:"";
    public string Summary=>continueSummary!=null?continueSummary.text:"";
    public float WindowGlow=>windowGlow!=null?windowGlow.color.a:0;
    public float BulbGlow(int i)=>bulbGlows!=null && i<bulbGlows.Length && bulbGlows[i]!=null?bulbGlows[i].color.a:0;
    public string LogoText=>logoLines==null?"":string.Join(" ",System.Linq.Enumerable.Select(logoLines,l=>l!=null?l.text:""));

    private void Awake()=>ApplyGameInfo();
    /// <summary>The name comes from GameInfo, so renaming the game needs no prefab edit.</summary>
    public void ApplyGameInfo()
    {
        for(int i=0;logoLines!=null && i<logoLines.Length;i++)
        {
            string line=i==0?GameInfo.LogoSmall:i-1<GameInfo.LogoLarge.Length?GameInfo.LogoLarge[i-1]:"";
            if(logoLines[i]!=null)logoLines[i].text=line;
            if(logoShadows!=null && i<logoShadows.Length && logoShadows[i]!=null)logoShadows[i].text=line;
        }
        if(kana!=null)kana.text=GameInfo.Kana;
        if(footer!=null)footer.text=GameInfo.VersionLabel+" ・ クレジット";
    }
    /// <param name="summary">The usable save in one line; empty hides "続きから".</param>
    /// <param name="firstLaunch">No save file at all: the tutorial gets the "はじめての人へ" tag.</param>
    /// <param name="tutorialReady">False until the tutorial (S2-T) exists: the button is shown disabled with "準備中".</param>
    public void Refresh(string summary,bool firstLaunch,bool tutorialReady,string problem,string recordLine)
    {
        bool hasSave=!string.IsNullOrEmpty(summary);
        if(continueButton!=null)continueButton.gameObject.SetActive(hasSave);
        if(continueSummary!=null)continueSummary.text=summary;
        // Without a save, starting the story is the one thing to press next.
        if(newGameButton!=null)newGameButton.Kind=hasSave?StickerKind.Secondary:StickerKind.Primary;
        if(tutorialButton!=null){tutorialButton.Interactable=tutorialReady;tutorialButton.LabelOffsetY=tutorialReady?0:10;}
        if(tutorialNote!=null)tutorialNote.gameObject.SetActive(!tutorialReady);
        if(firstTimeBadge!=null)firstTimeBadge.gameObject.SetActive(firstLaunch);
        if(status!=null){status.text=problem;status.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(problem));}
        if(records!=null){records.text=recordLine;records.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(recordLine));}
    }
    private void Update()=>Tick(Time.unscaledTime,DeliveryFeedback.Animate);
    /// <summary>Twinkling bulbs and the flickering window at time t; without animation both hold their base brightness.</summary>
    public void Tick(float t,bool animate)
    {
        for(int i=0;bulbGlows!=null && i<bulbGlows.Length;i++)
        {
            if(bulbGlows[i]==null)continue;
            // Each bulb has its own phase and a slightly different speed, so they twinkle out of step.
            float wave=animate?Mathf.Sin(t*twinkleSpeed*(1+.13f*(i%4))+i*1.7f):0;
            SetAlpha(bulbGlows[i],glowBase+glowRange*wave);
        }
        if(windowGlow!=null)SetAlpha(windowGlow,windowBase+(animate?windowRange*(Mathf.PerlinNoise(t*.9f,.37f)*2-1):0));
    }
    private static void SetAlpha(Graphic g,float a){var c=g.color;c.a=Mathf.Clamp01(a);g.color=c;}
}
