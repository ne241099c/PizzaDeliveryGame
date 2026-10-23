using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>今日のまとめ (UI_Mock/Settlement.png): a receipt that fills in line by line with counting numbers,
/// then growth, reputation and pizza quality. Values are read from the settled DayLedger and day statistics.
/// The frame (sky, title, progress dots, buttons) is the prefab Assets/Prefabs/UI/SettlementView.prefab (ScreenPrefabs);
/// the receipt and the cards on the right change every day and are built here.</summary>
public sealed class SettlementView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI title,cityTomorrow;
    [SerializeField] private RectTransform titleBadge;
    [SerializeField] private Image[] legendDots;
    [SerializeField] private StickerButton nextButton,skipButton;
    private ScreenRouter router;private GameClock clock;private CampaignManager campaign;private BusinessEconomy economy;private DayStats stats;private DeliveryPersonSelector roster;
    private KitchenCrew crew;private DeliveryFlowController flow;private ScoreManager score;private ReputationManager reputation;private CourierProgression progression;private CookingManager cooking;
    private UITheme T=>UiKit.T;
    private RectTransform receipt,right;
    private readonly List<DeliveryTrip> trips=new();private float openingReputation;
    private readonly List<(RectTransform row,int page,decimal target,TextMeshProUGUI value,string format)> rows=new();
    private float shownAt;private int revealed;private bool built,pending;
    public int Page=>revealed;
    public StickerButton NextButton=>nextButton;
    public StickerButton SkipButton=>skipButton;
    private const float StepSeconds=.45f;

    private void Awake()
    {
        router=FindFirstObjectByType<ScreenRouter>();clock=FindFirstObjectByType<GameClock>();campaign=FindFirstObjectByType<CampaignManager>();economy=FindFirstObjectByType<BusinessEconomy>();
        stats=FindFirstObjectByType<DayStats>();roster=FindFirstObjectByType<DeliveryPersonSelector>();crew=FindFirstObjectByType<KitchenCrew>();flow=FindFirstObjectByType<DeliveryFlowController>();
        score=FindFirstObjectByType<ScoreManager>();reputation=FindFirstObjectByType<ReputationManager>();progression=FindFirstObjectByType<CourierProgression>();cooking=FindFirstObjectByType<CookingManager>();
    }
    private void Start(){skipButton.Button.onClick.AddListener(RevealAll);nextButton.Button.onClick.AddListener(Next);}
    private void OnEnable(){if(clock==null)return;clock.OnDayStarted+=Opening;clock.OnDayEnded+=Closed;flow.Dispatched+=Trip;}
    private void OnDisable(){if(clock==null)return;clock.OnDayStarted-=Opening;clock.OnDayEnded-=Closed;flow.Dispatched-=Trip;}
    private void Opening(){trips.Clear();openingReputation=reputation.Value;built=false;}
    private void Trip(DeliveryTrip trip)=>trips.Add(trip);
    // Built on the next frame so the campaign has settled the ledger first.
    private void Closed(){pending=true;}

    private void Next()
    {
        if(revealed<4){RevealAll();return;}
        if(campaign.IsBankrupt){router.Show(ScreenId.GameOver);return;}
        campaign.NextDay();
    }
    public void RevealAll()
    {
        if(clock.Phase!=DayPhase.Ended || !economy.Ledger.Settled)return;
        if(pending || !built){pending=false;BuildContent();}
        revealed=4;shownAt=Time.unscaledTime-101;
    }

    private void BuildContent()
    {
        if(receipt!=null)Destroy(receipt.gameObject);if(right!=null)Destroy(right.gameObject);rows.Clear();built=true;revealed=0;shownAt=Time.unscaledTime;
        var root=(RectTransform)transform;var l=economy.Ledger;
        title.text=$"{campaign.DayNumber}日目 おつかれさま!";titleBadge.anchoredPosition=new Vector2(120+title.GetPreferredValues().x+24,-76);
        if(campaign.IsBankrupt){title.text=$"{campaign.DayNumber}日目 閉店です…";UiKit.SetBadge(titleBadge,"所持金がマイナス",T.berry,Color.white);}else UiKit.SetBadge(titleBadge,ShopStrings.Ended,T.paper,T.line);
        var save=FindFirstObjectByType<CampaignSaveManager>();
        if(!campaign.IsBankrupt && save!=null && save.NewRecords.Count>0)UiKit.SetBadge(titleBadge,"新記録！ "+string.Join("・",save.NewRecords),T.cheese,T.line);
        receipt=UiKit.Card(root,"Receipt",120,204,800,706,T.paper,16,8,3);
        int tips=trips.Sum(t=>t.Deliveries.Where(d=>d.Resolved).Sum(d=>score.TipForArrival(d.Order,d.ArrivesAt,t.ServiceAtDispatch)));
        decimal quality=l.QualityBonus,basePoints=l.Revenue-tips-quality;
        float y=26;
        Row(receipt,ref y,0,"売上",$"配達 {stats.DeliveredCount}件(期限内 {stats.DeliveredCount-stats.LateCount} / 遅れ {stats.LateCount})",l.Revenue,false,true);
        Sub(receipt,ref y,0,"基本の得点",basePoints);Sub(receipt,ref y,0,"チップ",tips);
        Sub(receipt,ref y,0,"品質ボーナス",quality,$"★3 ×{stats.ThreeStarOnTime} / ★2 ×{stats.TwoStarOnTime}");
        decimal per=economy.MaterialCost(1);
        Row(receipt,ref y,1,"材料費",per>0?$"{per:F0} × {l.Materials/per:F0}件":"",-l.Materials);
        Row(receipt,ref y,1,"燃料費","",-l.Fuel);
        string names=string.Join(" / ",roster.People.Select(p=>$"{p.Name} {economy.DailyWage(p):F0}").Concat(crew.People.Select(c=>$"{c.Name} {economy.DailyWage(c):F0}")).Take(3))+(roster.People.Count+crew.Count>3?" ほか":"");
        Row(receipt,ref y,1,"日給",names,-l.Wages);
        Row(receipt,ref y,1,"固定費","",-(l.FixedCosts-l.EquipmentMaintenance));
        Row(receipt,ref y,1,"設備の維持費","",-l.EquipmentMaintenance);
        Row(receipt,ref y,1,"事故の修理費",stats.AccidentCount>0?$"{stats.AccidentCount}回":"",-l.Repairs);
        if(l.WastedPizzas>0)Row(receipt,ref y,1,"廃棄",$"{l.WastedPizzas}枚",-l.Waste);
        var line=UiKit.Box(receipt,"Total",36,y+4,728,3);UiKit.Fill(line,T.line,0);rows.Add((line,2,0,null,null));
        var profitLabel=UiKit.Label(receipt,"ProfitName","今日の損益",44,y+40,300,60,30,T.line,FontRole.Black);
        var profit=UiKit.Label(receipt,"Profit","",300,y+16,420,96,T.titleSize,l.Profit>=0?T.basil:T.berry,FontRole.Display,TextAlignmentOptions.MidlineRight);
        rows.Add((profitLabel.rectTransform,2,0,null,null));rows.Add((profit.rectTransform,2,l.Profit,profit,"signed"));
        var badge=UiKit.Badge(receipt,"ProfitBadge",l.Profit>=0?"▲ 黒字":"▼ 赤字",650,y+96,l.Profit>=0?T.basil:T.berry,Color.white,40,20);rows.Add((badge,2,0,null,null));
        var cash=UiKit.Label(receipt,"Cash",$"所持金  <color={UiKit.Html(T.inkSub)}>{UiKit.Yen(economy.OpeningCash)}</color>  →  {UiKit.Yen(economy.Cash)}",44,y+136,720,60,32,T.line,FontRole.Display);
        rows.Add((cash.rectTransform,2,0,null,null));
        // zigzag bottom
        for(int i=0;i<26;i++){var z=UiKit.Box(receipt,"Zig"+i,10+i*30,696,22,22);z.localRotation=Quaternion.Euler(0,0,45);UiKit.Fill(z,T.paper,0);}
        right=UiKit.Box(root,"Right",980,204,820,640);
        BuildGrowth();BuildQuality();BuildAccidents();
        cityTomorrow.text=FindFirstObjectByType<MapManager>().TomorrowCityInfo(campaign.DayNumber);
        foreach(var r in rows)r.row.gameObject.SetActive(false);
    }
    private void Row(RectTransform card,ref float y,int page,string name,string detail,decimal amount,bool small=false,bool emphasis=false)
    {
        var row=UiKit.Box(card,"Row_"+name,36,y,728,emphasis?56:40);
        var n=UiKit.Label(row,"Name",name,8,0,220,emphasis?50:40,emphasis?T.headingSize:T.subheadingSize,T.line,FontRole.Display);float w=n.GetPreferredValues().x;
        UiKit.Label(row,"Detail",detail,16+w,4,Mathf.Max(1,424-w),emphasis?44:32,T.captionSize,T.inkSub,FontRole.Black);
        var v=UiKit.Label(row,"Value","",440,0,288,emphasis?50:40,emphasis?T.headingSize:T.subheadingSize,amount<0?T.berry:T.line,FontRole.Display,TextAlignmentOptions.MidlineRight);
        if(emphasis){var u=UiKit.Box(row,"Under",0,54,728,3);UiKit.Fill(u,T.line,0);}else UiKit.DashedLine(row,0,40,728,T.disabled,6,5,2);
        rows.Add((row,page,amount,v,"yen"));y+=emphasis?64:44;
    }
    private void Sub(RectTransform card,ref float y,int page,string name,decimal amount,string detail="")
    {
        var row=UiKit.Box(card,"Sub_"+name,60,y,704,32);UiKit.Label(row,"Name",name,8,0,200,32,T.bodySize,T.line,FontRole.Black);
        UiKit.Label(row,"Detail",detail,150,0,300,32,T.captionSize,T.inkSub,FontRole.Black);
        var v=UiKit.Label(row,"Value","",404,0,300,32,T.bodySize,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight);rows.Add((row,page,amount,v,"yen"));y+=32;
    }
    private void BuildGrowth()
    {
        var card=UiKit.Card(right,"Growth",0,0,820,320,T.paper,16,8,3);rows.Add((card,3,0,null,null));
        UiKit.Label(card,"Title","成長したこと",34,22,400,56,36,T.line,FontRole.Display);
        var records=progression.Records.ToList();UiKit.Badge(card,"Count",$"{records.Count}件",300,32,T.cheese,T.line,36,18);
        if(records.Count==0){UiKit.Label(card,"Empty","今日は成長なし。たくさん走ると育ちます。",34,120,740,40,22,T.inkSub,FontRole.Black);return;}
        for(int i=0;i<records.Count && i<3;i++)
        {
            var r=records[i];float y=96+i*72;
            UiKit.Avatar(card,"Avatar"+i,34,y,60,T.CourierColor(r.Person.Name),r.Person.Name.Substring(0,1));
            UiKit.Label(card,"Name"+i,$"{r.Person.Name} ・ {ShopStrings.Ability(r.Ability)}",110,y+8,260,44,24,T.line,FontRole.Black);
            var bar=UiKit.Bar(card,"Bar"+i,380,y+18,270,24,T.barFill);bar.Set(r.Ability==CourierAbility.Capacity?r.After/2:r.After);
            UiKit.Label(card,"Change"+i,$"{UiKit.Ability(r.Before)} → {UiKit.Ability(r.After)} <color={UiKit.Html(T.basil)}>▲</color>",660,y+8,150,44,24,T.line,FontRole.Display,TextAlignmentOptions.MidlineRight);
        }
    }
    private void BuildQuality()
    {
        var card=UiKit.Card(right,"Quality",0,340,820,232,T.paper,16,8,3);rows.Add((card,3,0,null,null));
        UiKit.Label(card,"RepTitle","評判",34,18,200,44,30,T.line,FontRole.Display);
        float delta=reputation.Value-openingReputation;
        UiKit.Label(card,"RepValue",$"{reputation.Value:F0}",34,62,140,80,64,T.line,FontRole.Display);
        UiKit.Label(card,"RepDelta",(delta>=0?"▲ +":"▼ ")+delta.ToString("0.#"),160,86,120,40,26,delta>=0?T.basil:T.berry,FontRole.Display);
        var bar=UiKit.Bar(card,"RepBar",34,148,330,24,T.cheese);bar.Set(reputation.Value/reputation.Maximum);
        UiKit.Label(card,"RepNote","期限内が多いと、評判が上がり、注文が増えます",34,180,360,44,15,T.inkSub,FontRole.Black,TextAlignmentOptions.TopLeft,true);
        var divider=UiKit.Box(card,"Divider",400,24,3,184);UiKit.Fill(divider,T.paperDeep,0);
        UiKit.Label(card,"PizzaTitle","ピザの出来",430,18,300,44,30,T.line,FontRole.Display);
        for(int s=3;s>=1;s--){float x=430+(3-s)*110;UiKit.Label(card,"Stars"+s,new string('★',s),x,70,110,36,22,T.cheese,FontRole.Black,TextAlignmentOptions.Center);UiKit.Label(card,"Count"+s,stats.StarCounts[s].ToString(),x,104,110,56,40,T.line,FontRole.Display,TextAlignmentOptions.Center);}
        string staff=string.Join("、",cooking.PackedByStaff.Select(p=>$"{p.Key} は {p.Value}枚"));
        UiKit.Label(card,"Makers",$"自分で作ったのは {cooking.PackedByPlayer}枚"+(staff.Length>0?"、"+staff:""),430,172,370,48,16,T.inkSub,FontRole.Black,TextAlignmentOptions.TopLeft,true);
    }
    private void BuildAccidents()
    {
        var accidents=trips.Where(t=>t.AccidentCount>0).GroupBy(t=>t.Person.Name).Select(g=>$"{g.Key} が {g.Sum(t=>t.AccidentCount)}回").ToList();
        if(accidents.Count==0)return;
        var card=UiKit.Card(right,"Accidents",0,592,820,72,T.cheeseLight,16,6,3);rows.Add((card,3,0,null,null));
        UiKit.Badge(card,"Badge","事故",24,18,T.cheese,T.line,36,18);
        UiKit.Label(card,"Text",string.Join("、",accidents)+" 事故にあいました(停止15秒)",110,0,690,72,22,T.line,FontRole.Black);
    }

    private void Update()
    {
        if(UIThemeDriver.RenderingSuspended)return;
        if(router.Current!=ScreenId.Settlement)return;
        if((pending || !built) && clock.Phase==DayPhase.Ended && economy.Ledger.Settled){pending=false;BuildContent();}
        float t=Time.unscaledTime-shownAt;
        int page=Mathf.Min(4,Mathf.FloorToInt(t/1.1f));if(page>revealed)revealed=page;
        for(int i=0;i<legendDots.Length;i++)legendDots[i].color=i<revealed?T.basil:i==revealed?T.cheese:T.paper;
        foreach(var r in rows)
        {
            r.row.gameObject.SetActive(r.page<=revealed);
            if(r.value==null)continue;
            float k=Mathf.Clamp01((t-r.page*1.1f)/.9f);if(revealed>=4 && t>100)k=1;decimal shown=r.target*(decimal)(1-(1-k)*(1-k));
            r.value.text=r.format=="signed"?UiKit.Signed(shown):(shown<0?"−":"")+"¥"+System.Math.Abs(System.Math.Round(shown,0)).ToString("#,0");
        }
        nextButton.Text=revealed<4?"次へ ▶":campaign.IsBankrupt?"結果を見る ▶":"次の日へ ▶";skipButton.gameObject.SetActive(revealed<4);
        nextButton.Interactable=clock.Phase==DayPhase.Ended;
    }
}
