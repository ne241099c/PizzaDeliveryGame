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
    private CanvasGroup help,credits;
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
        view.NewGameButton.Button.onClick.AddListener(()=>GameScenes.LoadGame(LaunchMode.NewGame));
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
        var records=CampaignSaveFile.ReadRecords(path);
        view.Refresh(usable?CampaignSaveFile.Summary(data):"",!exists,TutorialReady,exists && !usable?CampaignSaveFile.Problem(reason):"",
            records.bestDay>0?CampaignSaveFile.RecordsSummary(records):"");
    }
    private CanvasGroup Panel(string name)
    {
        var panel=UiKit.Stretch(overlay,name);panel.gameObject.AddComponent<Image>();var group=UiKit.Group(panel);UIVisibility.Set(panel.gameObject,false);return group;
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
        credits=Panel("CreditsOverlay");
        var creditsModal=ShopModals.Build(credits,overlay,1400,600,"クレジット");
        ShopModals.Body(creditsModal,100,380,creditsText!=null?creditsText.text:"",18);
        ShopModals.Button(creditsModal,"CreditsClose","閉じる",StickerKind.Primary,1100,500,260,76,CloseCredits);
    }
    private void Update()
    {
        if(Keyboard.current==null || !Keyboard.current.escapeKey.wasPressedThisFrame)return;
        if(CreditsOpen)CloseCredits();else if(HelpOpen)CloseHelp();
    }
    public void ShowHelp(){helpPage=0;RefreshHelp();UIVisibility.Set(help.gameObject,true);}
    public void CloseHelp()=>UIVisibility.Set(help.gameObject,false);
    private void TurnHelp(int step){helpPage=(helpPage+HelpPages.Pages.Length+step)%HelpPages.Pages.Length;RefreshHelp();}
    private void RefreshHelp(){helpModal.Title.text=HelpPages.Title(helpPage);helpModal.Body.text=HelpPages.Body(helpPage);helpPageText.text=$"{helpPage+1} / {HelpPages.Pages.Length}";}
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
