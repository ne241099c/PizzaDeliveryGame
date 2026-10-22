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
        "ONE DAY AT THE SHOP\nDuring preparation, hire or train your team, set the delivery promise, then start business. After closing, all couriers return before settlement.",
        "DELIVERY PROMISE\nA shorter promise attracts more orders. A longer promise reduces demand. Existing order deadlines do not change when the promise changes.",
        "CHOOSE YOUR ROADS\nSelect up to two orders and an available courier. Click connected roads through the destinations and back to the shop. Compare against individual trips. The suggested route is optional.",
        "MONEY AND REPUTATION\nOn-time deliveries earn tips and reputation. Late deliveries earn less. Materials, fuel, wages and fixed costs reduce cash. Rest couriers to recover stamina."};
    private void Start()
    {
        resume.onClick.AddListener(Resume);menuHelp.onClick.AddListener(ShowHelp);menuTitle.onClick.AddListener(RequestTitle);
        confirmYes.onClick.AddListener(ReturnTitle);confirmNo.onClick.AddListener(()=>UIVisibility.Set(confirmation.gameObject,false));
        helpNext.onClick.AddListener(()=>{helpPage=(helpPage+1)%Pages.Length;RefreshHelp();});helpPrevious.onClick.AddListener(()=>{helpPage=(helpPage+Pages.Length-1)%Pages.Length;RefreshHelp();});helpClose.onClick.AddListener(CloseHelp);
        titleHelp.onClick.AddListener(ShowHelp);headerMenu.onClick.AddListener(ShowMenu);quit.onClick.AddListener(Quit);
        continueGame.onClick.AddListener(ResumeClock);newGame.onClick.AddListener(ResumeClock);
        router.Changed+=ScreenChanged;HidePanels();ScreenChanged(router.Current);
    }
    private void OnDestroy()=>router.Changed-=ScreenChanged;
    private void Update()
    {
        if(Keyboard.current==null || !Keyboard.current.escapeKey.wasPressedThisFrame)return;
        if(HelpOpen)CloseHelp();else if(ConfirmationOpen)UIVisibility.Set(confirmation.gameObject,false);else if(MenuOpen)Resume();else ShowMenu();
    }
    private void ScreenChanged(ScreenId id)
    {
        if(id==ScreenId.Title){UIVisibility.Set(choice,true);continueGame.interactable=save.HasUsableSave;titleStatus.text=save.TitleDescription;}
    }
    private void PauseClock(){if(paused)return;paused=true;originalAutomaticTick=clock.AutomaticTick;clock.AutomaticTick=false;}
    private void ResumeClock(){if(!paused)return;paused=false;clock.AutomaticTick=originalAutomaticTick;}
    private void HidePanels(){UIVisibility.Set(menu.gameObject,false);UIVisibility.Set(help.gameObject,false);UIVisibility.Set(confirmation.gameObject,false);}
    public void ShowMenu(){PauseClock();UIVisibility.Set(menu.gameObject,true);}
    public void Resume(){HidePanels();if(router.Current!=ScreenId.Title)ResumeClock();}
    public void ShowHelp(){if(clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed)PauseClock();UIVisibility.Set(help.gameObject,true);helpPage=0;RefreshHelp();}
    private void RefreshHelp()=>helpText.text=$"{helpPage+1}/{Pages.Length}\n\n"+Pages[helpPage];
    public void CloseHelp(){UIVisibility.Set(help.gameObject,false);if(!MenuOpen && router.Current!=ScreenId.Title)ResumeClock();}
    public void RequestTitle()
    {
        if(clock.Phase==DayPhase.Open || clock.Phase==DayPhase.Closed){PauseClock();UIVisibility.Set(confirmation.gameObject,true);}
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
