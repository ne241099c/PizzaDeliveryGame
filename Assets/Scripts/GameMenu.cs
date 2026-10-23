using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>The in-game menu (Esc): resume, how to play, and back to the title (with a confirmation during business).
/// The clock stops while it is open. Each modal is a CanvasGroup panel under the in-game UI root (ShopUiRoot).</summary>
[DefaultExecutionOrder(250)]
public sealed class GameMenu : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private CanvasGroup menu,help,confirmation;
    private bool paused,originalAutomaticTick;
    private int helpPage;
    private ShopModals.Modal helpModal;
    private TextMeshProUGUI menuNote,helpPageText;
    private static UITheme T=>UiKit.T;
    public bool MenuOpen=>menu.alpha>.5f;
    public bool HelpOpen=>help.alpha>.5f;
    public bool ConfirmationOpen=>confirmation.alpha>.5f;

    private void Start(){BuildModals();HidePanels();}
    private void BuildModals()
    {
        var shop=FindFirstObjectByType<ShopUiRoot>(FindObjectsInactive.Include);var root=shop!=null?shop.transform:null;
        var menuModal=ShopModals.Build(menu,root,560,500,"メニュー");
        menuNote=ShopModals.Body(menuModal,92,64,"",18);menuNote.color=T.inkSub;
        ShopModals.Button(menuModal,"ModalResume","再開する",StickerKind.Primary,60,170,440,84,Resume);
        ShopModals.Button(menuModal,"ModalHelp","遊び方",StickerKind.Secondary,60,276,440,72,ShowHelp);
        ShopModals.Button(menuModal,"ModalTitle","タイトルへ",StickerKind.Secondary,60,370,440,72,RequestTitle);
        helpModal=ShopModals.Build(help,root,1200,640,"遊び方");
        ShopModals.Body(helpModal,104,400,"",24);
        helpPageText=UiKit.Label(helpModal.Card,"Page","",900,34,260,44,20,T.inkSub,FontRole.Black,TextAlignmentOptions.MidlineRight);
        ShopModals.Button(helpModal,"ModalPrevious","前へ",StickerKind.Secondary,40,532,220,72,()=>TurnHelp(-1));
        ShopModals.Button(helpModal,"ModalNext","次へ ▶",StickerKind.Secondary,280,532,220,72,()=>TurnHelp(1));
        ShopModals.Button(helpModal,"ModalClose","閉じる",StickerKind.Primary,900,524,260,84,CloseHelp);
        var confirmModal=ShopModals.Build(confirmation,root,760,420,"タイトルに戻りますか？");
        ShopModals.Body(confirmModal,104,140,"今日の営業はここで終わり、結果は残りません。\n「続きから」で、今日の開店準備から再開できます。",22);
        ShopModals.Button(confirmModal,"ModalConfirm","タイトルへ",StickerKind.Danger,40,300,320,84,ReturnTitle);
        ShopModals.Button(confirmModal,"ModalCancel","営業に戻る",StickerKind.Primary,400,300,320,84,CancelTitle);
    }
    private void Update()
    {
        if(Keyboard.current==null || !Keyboard.current.escapeKey.wasPressedThisFrame)return;
        if(ShopHand.Instance!=null && (ShopHand.Instance.Held!=null || ShopHand.Instance.CancelledThisFrame)){ShopHand.Instance.Cancel();return;}
        if(HelpOpen)CloseHelp();
        else if(ConfirmationOpen)CancelTitle();
        else if(MenuOpen)Resume();
        else ShowMenu();
    }
    private bool InBusiness=>clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed;
    private void PauseClock(){if(paused)return;paused=true;originalAutomaticTick=clock.AutomaticTick;clock.AutomaticTick=false;}
    private void ResumeClock(){if(!paused)return;paused=false;clock.AutomaticTick=originalAutomaticTick;}
    private void HidePanels(){UIVisibility.Set(menu.gameObject,false);UIVisibility.Set(help.gameObject,false);UIVisibility.Set(confirmation.gameObject,false);}
    public void ShowMenu()
    {
        PauseClock();UIVisibility.Set(menu.gameObject,true);
        if(menuNote!=null)menuNote.text=InBusiness?"営業は止まっています。セーブは毎日の開店準備で自動。途中でやめると、今日の開店準備から再開します。":"開店準備の内容は自動でセーブされています。";
    }
    public void Resume(){HidePanels();ResumeClock();}
    public void ShowHelp(){if(InBusiness)PauseClock();UIVisibility.Set(help.gameObject,true);helpPage=0;RefreshHelp();}
    private void TurnHelp(int step){helpPage=(helpPage+HelpPages.Pages.Length+step)%HelpPages.Pages.Length;RefreshHelp();}
    private void RefreshHelp()
    {
        if(helpModal==null)return;
        helpModal.Title.text=HelpPages.Title(helpPage);helpModal.Body.text=HelpPages.Body(helpPage);helpPageText.text=$"{helpPage+1} / {HelpPages.Pages.Length}";
    }
    public void CloseHelp(){UIVisibility.Set(help.gameObject,false);if(!MenuOpen)ResumeClock();}
    /// <summary>During business the day's progress would be lost, so it asks first.</summary>
    public void RequestTitle()
    {
        if(InBusiness){PauseClock();UIVisibility.Set(menu.gameObject,false);UIVisibility.Set(confirmation.gameObject,true);}
        else ReturnTitle();
    }
    public void CancelTitle(){UIVisibility.Set(confirmation.gameObject,false);UIVisibility.Set(menu.gameObject,true);}
    public void ReturnTitle(){HidePanels();PauseClock();GameScenes.LoadTitle();}
}
