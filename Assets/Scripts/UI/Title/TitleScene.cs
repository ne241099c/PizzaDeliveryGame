using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>The Title scene: shows the save state on TitleView, opens help and credits, and starts the Game scene in the
/// chosen mode (GameScenes). It only reads the save file; the Game scene validates and loads it.</summary>
public sealed class TitleScene : MonoBehaviour
{
    [SerializeField] private TitleView view;
    [SerializeField] private TextAsset creditsText;
    /// <summary>Where the help and credits modals are placed (above the title).</summary>
    [SerializeField] private RectTransform overlay;
    private CanvasGroup help,credits,mode,profile;
    private ShopModals.Modal profileModal;
    private TextMeshProUGUI profileText;
    private RectTransform profileContent;
    private ShopModals.Modal helpModal;
    private TextMeshProUGUI helpPageText;
    private int helpPage;
    private static UITheme T=>UiKit.T;
    /// <summary>The tutorial (S2-T) is not implemented yet: the title shows its button disabled with "準備中".</summary>
    public static bool TutorialReady=>false;
    /// <summary>Validations read a temporary file instead of the player's save.</summary>
    public string SavePathOverride {get;set;}
    private string SavePath=>SavePathOverride??CampaignSaveFile.DefaultPath;
    public TitleView View=>view;
    public bool HelpOpen=>help!=null && help.alpha>.5f;
    public bool CreditsOpen=>credits!=null && credits.alpha>.5f;

    private void Start()
    {
        view.ContinueButton.Button.onClick.AddListener(()=>GameScenes.LoadGame(LaunchMode.Continue));
        view.NewGameButton.Button.onClick.AddListener(ShowMode);
        view.HelpButton.Button.onClick.AddListener(ShowHelp);
        view.QuitButton.Button.onClick.AddListener(Quit);
        view.CreditsButton.onClick.AddListener(ShowCredits);
        BuildModals();
        Refresh();
    }
    /// <summary>Reads the save (and best records) and updates the buttons.</summary>
    public void Refresh()
    {
        string path=SavePath;bool exists=File.Exists(path);
        bool usable=CampaignSaveFile.TryReadSummary(path,out var data,out var reason) && exists;
        string problem=GameScenes.TitleProblem;
        if(!string.IsNullOrEmpty(problem))usable=false;
        else if(exists && !usable)problem=CampaignSaveFile.Problem(reason);
        var records=CampaignSaveFile.ReadRecords(path);
        view.Refresh(usable?CampaignSaveFile.Summary(data):"",!exists,TutorialReady,problem,
            records.bestDay>0?CampaignSaveFile.RecordsSummary(records):"");
    }
    private CanvasGroup Panel(string name)
    {
        var panel=UiKit.Stretch(overlay,name);
        panel.gameObject.AddComponent<Image>();
        var group=UiKit.Group(panel);
        UIVisibility.Set(panel.gameObject,false);
        return group;
    }
    private void BuildModals()
    {
        help=Panel("HelpOverlay");
        helpModal=ShopModals.Build(help,overlay,1200,640,"遊び方");
        ShopModals.Body(helpModal,104,400,"",24);
        helpPageText=UiKit.Label(helpModal.Card,"Page","",900,34,260,44,20,T.inkSub,FontRole.Black,TextAlignmentOptions.MidlineRight);
        ShopModals.Button(helpModal,"ModalPrevious","前へ",StickerKind.Secondary,40,532,220,72,()=>TurnHelp(-1));
        ShopModals.Button(helpModal,"ModalNext","次へ ▶",StickerKind.Secondary,280,532,220,72,()=>TurnHelp(1));
        ShopModals.Button(helpModal,"ModalClose","閉じる",StickerKind.Primary,900,524,260,84,CloseHelp);
        BuildRewardModals();
        credits=Panel("CreditsOverlay");
        var creditsModal=ShopModals.Build(credits,overlay,1400,600,"クレジット");
        ShopModals.Body(creditsModal,100,380,creditsText!=null?creditsText.text:"",18);
        ShopModals.Button(creditsModal,"CreditsClose","閉じる",StickerKind.Primary,1100,500,260,76,CloseCredits);
    }
    private void Update()
    {
        if(Keyboard.current==null || !Keyboard.current.escapeKey.wasPressedThisFrame)return;
        if(profile!=null && profile.alpha>.5f)UiKit.Show(profile,false);
        else if(mode!=null && mode.alpha>.5f)UiKit.Show(mode,false);
        else if(CreditsOpen)CloseCredits();else if(HelpOpen)CloseHelp();
    }
    private void BuildRewardModals()
    {
        UiKit.Button(overlay,"RecordsButton",RewardStrings.Records,StickerKind.Secondary,1540,32,348,64,T.bodySize,ShowProfile);
        mode=Panel("GameModeOverlay");
        var choice=ShopModals.Build(mode,overlay,1260,640,RewardStrings.Mode);
        UiKit.Label(choice.Card,"NormalDescription",RewardStrings.NormalHelp,40,120,540,180,T.subheadingSize,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        UiKit.Label(choice.Card,"EndlessDescription",RewardStrings.EndlessHelp,660,120,540,180,T.subheadingSize,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        ShopModals.Button(choice,"NormalMode",RewardStrings.Normal,StickerKind.Primary,40,336,540,92,()=>StartMode(GameMode.Normal));
        ShopModals.Button(choice,"EndlessMode",RewardStrings.Endless,StickerKind.Secondary,660,336,540,92,()=>StartMode(GameMode.Endless));
        ShopModals.Button(choice,"CancelMode",RewardStrings.Cancel,StickerKind.Secondary,940,520,280,76,()=>UiKit.Show(mode,false));
        profile=Panel("RecordsOverlay");
        profileModal=ShopModals.Build(profile,overlay,1420,880,RewardStrings.Records);
        var viewport=UiKit.Box(profileModal.Card,"RecordsViewport",40,108,1340,648);
        UiKit.Fill(viewport,T.paper,0,true);
        viewport.gameObject.AddComponent<RectMask2D>();
        profileContent=UiKit.Box(viewport,"Content",0,0,1300,1200);
        profileText=UiKit.Label(profileContent,"RecordsText","",0,0,1296,1200,T.bodySize,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.viewport=viewport;scroll.content=profileContent;scroll.horizontal=false;scroll.vertical=true;
        ScrollNavigation.Configure(scroll);
        var track=UiKit.Box(viewport,"Scrollbar",1320,0,16,648);
        UiKit.Fill(track,T.paperDeep,8,true);
        var handle=UiKit.Stretch(track,"Handle");
        var bar=track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect=handle;bar.targetGraphic=UiKit.Fill(handle,T.inkSub,8,true);
        bar.direction=Scrollbar.Direction.BottomToTop;
        scroll.verticalScrollbar=bar;scroll.verticalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        ShopModals.Button(profileModal,"CloseRecords",RewardStrings.Close,StickerKind.Primary,1100,788,280,64,()=>UiKit.Show(profile,false));
    }
    public bool ModeOpen=>mode!=null && mode.alpha>.5f;
    public bool ProfileOpen=>profile!=null && profile.alpha>.5f;
    public void ShowMode()
    {
        mode.transform.SetAsLastSibling();
        UiKit.Show(mode,true);
    }
    private static void StartMode(GameMode chosen)
    {
        GameScenes.NewGameMode=chosen;
        GameScenes.LoadGame(LaunchMode.NewGame);
    }
    public void ShowProfile()
    {
        var data=RewardProfile.Load(RewardProfile.DefaultPath,out var warning);
        profileText.text=RewardProfile.ReadSummary(data,RewardProfile.DefaultDefinitions());
        if(!string.IsNullOrEmpty(warning))profileText.text+="\n\n"+warning;
        float height=Mathf.Max(648,profileText.GetPreferredValues(profileText.text,1296,0).y+32);
        profileContent.sizeDelta=new Vector2(1300,height);
        profileText.rectTransform.sizeDelta=new Vector2(1296,height);
        profileText.ForceMeshUpdate();
        profile.transform.SetAsLastSibling();
        UiKit.Show(profile,true);
    }
    public void ShowHelp(){helpPage=0;RefreshHelp();UIVisibility.Set(help.gameObject,true);}
    public void CloseHelp()=>UIVisibility.Set(help.gameObject,false);
    private void TurnHelp(int step){helpPage=(helpPage+HelpPages.Pages.Length+step)%HelpPages.Pages.Length;RefreshHelp();}
    private void RefreshHelp()
    {
        helpModal.Title.text=HelpPages.Title(helpPage);
        helpModal.Body.text=HelpPages.Body(helpPage);
        helpPageText.text=$"{helpPage+1} / {HelpPages.Pages.Length}";
    }
    public void ShowCredits()=>UIVisibility.Set(credits.gameObject,true);
    public void CloseCredits()=>UIVisibility.Set(credits.gameObject,false);
    private static void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
        #else
        Application.Quit();
        #endif
    }
}
