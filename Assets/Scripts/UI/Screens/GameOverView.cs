using System.Linq;
using TMPro;
using UnityEngine;

/// <summary>閉店 (設計書_画面デザイン 4-4): a calm summary and two choices. The layout is the prefab
/// Assets/Prefabs/UI/GameOverView.prefab (built by ScreenPrefabs); this fills in the numbers and the staff row.</summary>
public sealed class GameOverView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI days,records;
    [SerializeField] private TextMeshProUGUI[] statValues;
    [SerializeField] private RectTransform team;
    [SerializeField] private StickerButton retry,toTitle;
    private ScreenRouter router;
    private CampaignManager campaign;
    private CourierRoster roster;
    private KitchenCrew crew;
    private CampaignSaveManager save;
    private UITheme T=>UiKit.T;private ScreenId last=(ScreenId)(-1);
    public StickerButton Retry=>retry;
    public StickerButton ToTitle=>toTitle;
    private void Awake()
    {
        router=FindFirstObjectByType<ScreenRouter>();
        campaign=FindFirstObjectByType<CampaignManager>();
        roster=FindFirstObjectByType<CourierRoster>();
        crew=FindFirstObjectByType<KitchenCrew>();
        save=FindFirstObjectByType<CampaignSaveManager>();
    }
    private void Start()
    {
        retry.Button.onClick.AddListener(()=>save.StartNewGame());
        toTitle.Button.onClick.AddListener(GameScenes.LoadTitle);
    }
    private void Update()
    {
        if(UiRendering.Suspended)return;
        if(router.Current==last)return;last=router.Current;if(last==ScreenId.GameOver)Refresh();
    }
    public void Refresh()
    {
        days.text=$"{campaign.DayNumber}日目まで、お店を続けました。";
        string[] values={UiKit.Yen(campaign.TotalRevenue),UiKit.Yen(campaign.BestDailyRevenue),campaign.TotalDelivered+"件"};
        for(int i=0;i<statValues.Length && i<values.Length;i++)statValues[i].text=values[i];
        records.text=save!=null?save.RecordsSummary:"";
        // The staff row changes with the roster, so it is rebuilt each time the screen opens.
        foreach(Transform child in team)Destroy(child.gameObject);
        float x=0;
        foreach(var p in roster.People.Take(6)){Member(p.Name,T.CourierColor(p.Name),x);x+=128;}
        foreach(var c in crew.People.Take(2)){Member(c.Name,T.info,x);x+=128;}
    }
    private void Member(string name,Color color,float x)
    {
        UiKit.Avatar(team,"Avatar_"+name,x+20,0,76,color,name.Substring(0,1));
        UiKit.Label(team,"Name_"+name,name,x,82,116,30,18,T.line,FontRole.Black,TextAlignmentOptions.Center);
    }
}
