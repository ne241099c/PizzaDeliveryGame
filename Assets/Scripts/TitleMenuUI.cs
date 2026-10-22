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
        "営業の流れ\n開店準備でスタッフ・設備・お届け目安を決め、営業を始めます。厨房と配達指示は下のボタン、1 / 2、Q / Eで切り替えます。閉店後は全員の帰還を待って精算します。",
        "厨房と自動梱包\n内勤は自動で調理します。自分で作るときは伝票→生地→ソース→チーズ→指定の具→窯の順です。緑の帯で窯をクリックして取り出すと高品質。取り出した後の梱包は自動です。",
        "配達指示\n① 左上で配達員の名前を選ぶ ② 注文一覧の「積む」を押す ③ 「届け先へ」か地図のピンを行く順に押す ④ 時間を比べて「出発」。最後の届け先から店へ戻る経路が付きます。道を直接クリックして自分で組むこともできます。「1本戻す」「やり直す」で修正でき、おすすめ経路は任意です。",
        "目安・遅れ・売上\n注文ごとに残り時間を表示します。目安を過ぎても受付猶予中は出発でき、配達は減点で成功になります。遅れるとチップ・品質ボーナスは付きません。未出発のまま猶予を過ぎるとキャンセル、閉店でも未出発分はキャンセルです。短い目安は注文が増え、長い目安は減ります。既存注文の目安は変更されません。"};
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
