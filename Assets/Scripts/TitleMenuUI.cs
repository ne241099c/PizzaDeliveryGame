using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[DefaultExecutionOrder(250)]
public sealed class TitleMenuUI : MonoBehaviour
{
    [SerializeField] private ScreenRouter router;
    [SerializeField] private GameClock clock;
    [SerializeField] private CampaignSaveManager save;
    [SerializeField] private CanvasGroup menu,help,confirmation;
    [SerializeField] private GameObject choice;
    [SerializeField] private TextMeshProUGUI helpText,titleStatus;
    [SerializeField] private Button resume,menuHelp,menuTitle,confirmYes,confirmNo,helpNext,helpPrevious,helpClose,titleHelp,quit,headerMenu,continueGame,newGame;
    private bool paused,originalAutomaticTick;
    private int helpPage;
    public bool MenuOpen=>menu.alpha>.5f;
    public bool HelpOpen=>help.alpha>.5f;
    public bool ConfirmationOpen=>confirmation.alpha>.5f;
    private static readonly string[] Pages={
        "営業の流れ\n開店準備でスタッフ・設備・お届け目安を決めて「営業開始」。営業は3分です。左上のスイッチ（または 1 / 2、Q / E）で厨房と配達指示を切り替えます。閉店後、全員が帰ったら精算です。",
        "厨房\n内勤が自動で調理します。自分で作るときは、伝票 → 生地 → ソース → チーズ → 指定の具 → 窯の順。緑の帯で窯から出すと高品質です。焼けたピザは自動で箱に入り、保温棚に並びます。",
        "配達指示\n① 焼き上がった伝票を、下の配達員のレーンへドラッグします（地図のピンからも可）。同じレーンに入れると、まとめて運びます。\n② 赤いルートを、通りたい道までドラッグして直し、「出発」。届ける順番は、レーンの伝票を入れ替えます。",
        "目安・遅れ・売上\n目安を過ぎても受付猶予中は出発でき、減点で配達できます。遅れるとチップ・品質ボーナスは付きません。出発しないまま猶予を過ぎるか閉店すると、キャンセルです。目安を短くすると注文が増えます。",
        "セーブ\n開店準備のたびに自動でセーブします。営業の途中でやめたときは、「続きから」でその日の開店準備から再開します。最高記録は、ゲームをやり直しても残ります。"};
    private ShopModals.Modal menuModal,helpModal,confirmModal;private TextMeshProUGUI menuNote,helpPageText;
    private static UITheme T=>UiKit.T;
    private void BuildModals()
    {
        var shop=FindFirstObjectByType<ShopUiRoot>(FindObjectsInactive.Include);var root=shop!=null?shop.transform:null;
        menuModal=ShopModals.Build(menu,root,560,500,"メニュー");
        menuNote=ShopModals.Body(menuModal,92,64,"",18);menuNote.color=T.inkSub;
        ShopModals.Button(menuModal,"ModalResume","再開する",StickerKind.Primary,60,170,440,84,Resume);
        ShopModals.Button(menuModal,"ModalHelp","遊び方",StickerKind.Secondary,60,276,440,72,ShowHelp);
        ShopModals.Button(menuModal,"ModalTitle","タイトルへ",StickerKind.Secondary,60,370,440,72,RequestTitle);
        helpModal=ShopModals.Build(help,root,1200,640,"遊び方");
        ShopModals.Body(helpModal,104,400,"",24);
        helpPageText=UiKit.Label(helpModal.Card,"Page","",900,34,260,44,20,T.inkSub,FontRole.Black,TextAlignmentOptions.MidlineRight);
        ShopModals.Button(helpModal,"ModalPrevious","前へ",StickerKind.Secondary,40,532,220,72,()=>{helpPage=(helpPage+Pages.Length-1)%Pages.Length;RefreshHelp();});
        ShopModals.Button(helpModal,"ModalNext","次へ ▶",StickerKind.Secondary,280,532,220,72,()=>{helpPage=(helpPage+1)%Pages.Length;RefreshHelp();});
        ShopModals.Button(helpModal,"ModalClose","閉じる",StickerKind.Primary,900,524,260,84,CloseHelp);
        confirmModal=ShopModals.Build(confirmation,root,760,420,"タイトルに戻りますか？");
        ShopModals.Body(confirmModal,104,140,"今日の営業はここで終わり、結果は残りません。\n「続きから」で、今日の開店準備から再開できます。",22);
        ShopModals.Button(confirmModal,"ModalConfirm","タイトルへ",StickerKind.Danger,40,300,320,84,ReturnTitle);
        ShopModals.Button(confirmModal,"ModalCancel","営業に戻る",StickerKind.Primary,400,300,320,84,()=>{UIVisibility.Set(confirmation.gameObject,false);UIVisibility.Set(menu.gameObject,true);});
    }
    private void Start()
    {
        resume.onClick.AddListener(Resume);menuHelp.onClick.AddListener(ShowHelp);menuTitle.onClick.AddListener(RequestTitle);
        confirmYes.onClick.AddListener(ReturnTitle);confirmNo.onClick.AddListener(()=>UIVisibility.Set(confirmation.gameObject,false));
        helpNext.onClick.AddListener(()=>{helpPage=(helpPage+1)%Pages.Length;RefreshHelp();});helpPrevious.onClick.AddListener(()=>{helpPage=(helpPage+Pages.Length-1)%Pages.Length;RefreshHelp();});helpClose.onClick.AddListener(CloseHelp);
        titleHelp.onClick.AddListener(ShowHelp);headerMenu.onClick.AddListener(ShowMenu);quit.onClick.AddListener(Quit);
        continueGame.onClick.AddListener(ResumeClock);newGame.onClick.AddListener(ResumeClock);
        BuildModals();router.Changed+=ScreenChanged;HidePanels();ScreenChanged(router.Current);
    }
    private void OnDestroy()=>router.Changed-=ScreenChanged;
    private void Update()
    {
        if(Keyboard.current==null || !Keyboard.current.escapeKey.wasPressedThisFrame)return;
        if(HelpOpen)CloseHelp();else if(ConfirmationOpen){UIVisibility.Set(confirmation.gameObject,false);UIVisibility.Set(menu.gameObject,true);}else if(MenuOpen)Resume();else ShowMenu();
    }
    private void ScreenChanged(ScreenId id)
    {
        if(id==ScreenId.Title){UIVisibility.Set(choice,true);continueGame.interactable=save.HasUsableSave;titleStatus.text=save.TitleDescription+"\n"+save.RecordsSummary;}
    }
    private void PauseClock(){if(paused)return;paused=true;originalAutomaticTick=clock.AutomaticTick;clock.AutomaticTick=false;}
    private void ResumeClock(){if(!paused)return;paused=false;clock.AutomaticTick=originalAutomaticTick;}
    private void HidePanels(){UIVisibility.Set(menu.gameObject,false);UIVisibility.Set(help.gameObject,false);UIVisibility.Set(confirmation.gameObject,false);}
    public void ShowMenu()
    {
        PauseClock();UIVisibility.Set(menu.gameObject,true);
        if(menuNote!=null)menuNote.text=clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed?"営業は止まっています。セーブは毎日の開店準備で自動。途中でやめると、今日の開店準備から再開します。":"開店準備の内容は自動でセーブされています。";
    }
    public void Resume(){HidePanels();if(router.Current!=ScreenId.Title)ResumeClock();}
    public void ShowHelp(){if(clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed)PauseClock();UIVisibility.Set(help.gameObject,true);helpPage=0;RefreshHelp();}
    private void RefreshHelp()
    {
        helpText.text=$"{helpPage+1}/{Pages.Length}\n\n"+Pages[helpPage];if(helpModal==null)return;
        var parts=Pages[helpPage].Split(new[]{'\n'},2);helpModal.Title.text=parts[0];helpModal.Body.text=parts.Length>1?parts[1]:"";helpPageText.text=$"{helpPage+1} / {Pages.Length}";
    }
    public void CloseHelp(){UIVisibility.Set(help.gameObject,false);if(!MenuOpen && router.Current!=ScreenId.Title)ResumeClock();}
    public void RequestTitle()
    {
        if(clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed){PauseClock();UIVisibility.Set(menu.gameObject,false);UIVisibility.Set(confirmation.gameObject,true);}
        else ReturnTitle();
    }
    public void ReturnTitle(){HidePanels();if(clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed)PauseClock();router.Show(ScreenId.Title);}
    private void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying=false;
        #else
        Application.Quit();
        #endif
    }
}
