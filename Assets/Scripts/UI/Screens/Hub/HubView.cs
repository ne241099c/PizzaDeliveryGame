using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>開店準備の事務室 (UI_Mock/Hub.png): top bar, tabs, cards and the start button. Presentation only: every action
/// calls the existing managers. The frame is the prefab Assets/Prefabs/UI/HubView.prefab; each card is an instance of a
/// card prefab (Assets/Prefabs/UI/Cards), placed in Content and rebuilt when the data changes.</summary>
public sealed class HubView : MonoBehaviour
{
    [Header("Frame")]
    [SerializeField] private RectTransform content,dialog;
    [SerializeField] private TextMeshProUGUI dayText,phaseText,cashText,reputationText,hintText,noteText,messageText;
    [SerializeField] private TextMeshProUGUI forecastWages,forecastFixed,forecastMaintenance,forecastTraining,promiseText;
    [SerializeField] private RectTransform cashDelta;
    [SerializeField] private UiBar reputationBar;
    [SerializeField] private StickerButton[] tabs=new StickerButton[4];
    /// <summary>Count badges on the staff and applicant tabs ([2] and [3] stay empty).</summary>
    [SerializeField] private RectTransform[] tabBadges=new RectTransform[4];
    [SerializeField] private StickerButton startButton,menuButton;
    [SerializeField] private Slider promiseSlider;
    [SerializeField] private TrainingDialogView trainingDialog;
    [Header("Card prefabs")]
    [SerializeField] private CourierCardView courierCard;
    [SerializeField] private ApplicantCardView applicantCard;
    [SerializeField] private KitchenCardView kitchenCard;
    [SerializeField] private FacilityCardView facilityCard;
    [SerializeField] private FindTeammateCardView findTeammateCard;
    [SerializeField] private ManagementPanelView managementPanel;

    private CampaignManager campaign;private GameClock clock;private BusinessEconomy economy;private CourierRoster roster;private KitchenCrew crew;
    private RecruitmentManager recruitment;private TrainingManager training;private FacilityManager facilities;private OrderSpawner spawner;
    private ReputationManager reputation;
    private GameMenu menu;
    private CourierProgression progression;
    private ScreenRouter router;
    private MapManager map;
    private ManagementPanelView management;
    private string signature="";private float messageUntil;private string lastMessage="";
    private readonly List<(int day,decimal revenue,decimal profit,decimal cash)> records=new();
    private const float CardW=356,CardH=610,Gap=28,FacilityW=300,FacilityGap=22;
    private UITheme T=>UiKit.T;
    public int SelectedTab {get;private set;}
    public RectTransform Content=>content;

    private void Awake()
    {
        campaign=Find<CampaignManager>();
        clock=Find<GameClock>();
        economy=Find<BusinessEconomy>();
        roster=Find<CourierRoster>();
        crew=Find<KitchenCrew>();
        recruitment=Find<RecruitmentManager>();training=Find<TrainingManager>();facilities=Find<FacilityManager>();spawner=Find<OrderSpawner>();
        reputation=Find<ReputationManager>();
        menu=Find<GameMenu>();
        progression=Find<CourierProgression>();
        router=Find<ScreenRouter>();
        map=Find<MapManager>();
    }
    private static TComp Find<TComp>() where TComp:UnityEngine.Object=>FindFirstObjectByType<TComp>();
    private void Start()
    {
        for(int i=0;i<tabs.Length;i++){int tab=i;tabs[i].Button.onClick.AddListener(()=>ShowTab(tab));}
        menuButton.Button.onClick.AddListener(()=>menu.ShowMenu());
        startButton.Button.onClick.AddListener(()=>campaign.StartBusiness());
        UiKit.BindSlider(promiseSlider,v=>spawner.SetPromiseSeconds(v));
        trainingDialog.Close.Button.onClick.AddListener(CloseTraining);
        UiKit.Show(dialog,false);
        ShowTab(0);
    }
    private void OnEnable(){if(clock!=null)clock.OnDayEnded+=Record;}
    private void OnDisable(){if(clock!=null)clock.OnDayEnded-=Record;}
    private void Record()
    {
        var l=economy.Ledger;
        if(campaign.DayNumber==1)records.Clear();
        records.RemoveAll(r=>r.day==campaign.DayNumber);
        records.Add((campaign.DayNumber,l.Revenue,l.Profit,economy.Cash));
    }
    public void ShowTab(int tab)
    {
        SelectedTab=tab;
        for(int i=0;i<tabs.Length;i++)tabs[i].Kind=i==tab?StickerKind.Primary:StickerKind.Secondary;
        signature="";
    }

    private void Update()
    {
        if(UiRendering.Suspended)return;
        if(router!=null && router.Current!=ScreenId.Hub)return;
        ShowTopBar();
        ShowForecast();
        startButton.Interactable=clock.Phase==DayPhase.BeforeOpening && !campaign.IsBankrupt;
        string message=FirstNonEmpty(recruitment.LastMessage,training.LastMessage,facilities.LastMessage);
        if(message!=lastMessage){lastMessage=message;messageUntil=Time.unscaledTime+4;}
        messageText.text=Time.unscaledTime<messageUntil?UiStrings.Japanese(message):"";
        hintText.text=map.TodayCityInfo+"  ／  "+Hint();
        string next=Signature();
        if(next!=signature){signature=next;Rebuild();}
    }
    private void ShowTopBar()
    {
        dayText.text=campaign.DayNumber+ShopStrings.Day;
        cashText.text=UiKit.Yen(economy.Cash);
        var previous=records.LastOrDefault(r=>r.day==campaign.DayNumber-1);
        decimal delta=previous.day>0?previous.profit:0;
        UiKit.SetBadge(cashDelta,(delta>=0?"▲ ":"▼ ")+UiKit.Signed(delta).Replace("¥",""),delta>=0?T.basil:T.berry,Color.white);
        cashDelta.gameObject.SetActive(previous.day>0);
        reputationText.text=Mathf.RoundToInt(reputation.Value).ToString();
        reputationBar.Set(reputation.Value/reputation.Maximum);
        SetTabCount(0,$"{recruitment.TeamCount}人",T.paper);
        SetTabCount(1,(recruitment.Applicants.Count+recruitment.KitchenApplicants.Count)+"人",T.cheese);
        noteText.text=ShopStrings.TeamNote;
    }
    private void SetTabCount(int tab,string text,Color fill)
    {
        var badge=tabBadges[tab];UiKit.SetBadge(badge,text,fill,T.line,56);
        badge.anchoredPosition=new Vector2(236-14-badge.sizeDelta.x,-26);
    }
    private void ShowForecast()
    {
        forecastWages.text=UiKit.Yen(economy.TeamWages);
        forecastFixed.text=UiKit.Yen(economy.BaseFixedCost(campaign.DayNumber));
        forecastMaintenance.text=UiKit.Yen(facilities.Maintenance);
        forecastTraining.text=UiKit.Yen(training.Assignments.Where(a=>!a.Completed).Sum(a=>a.Paid));
        promiseText.text=$"{spawner.PromiseSeconds:F0}<size=24>秒</size>";
        UiKit.SyncPromise(promiseSlider,spawner,clock);
        if(management==null)return;
        UiKit.SyncPromise(management.Promise,spawner,clock);
        management.ShowPromise($"{spawner.PromiseSeconds:F0}<size=28>秒</size>",
            $"短いほど注文が増え、長いほど減ります。\n需要 x{spawner.DemandFactor(spawner.PromiseSeconds):0.00} ・ 約{spawner.ExpectedInterval:F0}秒ごとに注文\n"
            +$"下限 {spawner.MinimumPromiseSeconds:F1}秒（往復と調理の時間）\n評判 {reputation.Value:F0} ・ 注文 x{reputation.DemandFactor:0.00}");
    }
    private static string FirstNonEmpty(params string[] values)=>values.FirstOrDefault(v=>!string.IsNullOrEmpty(v))??"";
    /// <summary>Everything the cards show; a change rebuilds them.</summary>
    private string Signature()
    {
        var parts=new List<string>{SelectedTab.ToString(),clock.Phase.ToString(),recruitment.MaximumPeople.ToString(),((long)economy.Cash).ToString()};
        parts.AddRange(roster.People.Select(p=>UiKit.Ref(p)+p.Capacity+p.Status+string.Join("/",Enumerable.Range(0,5).Select(i=>p.Ability((CourierAbility)i).ToString("F2")+":"+p.Experience((CourierAbility)i).ToString("F1")))));
        parts.AddRange(crew.People.Select(c=>UiKit.Ref(c)));
        parts.AddRange(recruitment.Applicants.Select(a=>UiKit.Ref(a)));
        parts.AddRange(recruitment.KitchenApplicants.Select(a=>UiKit.Ref(a)));
        parts.AddRange(training.Assignments.Select(a=>a.PersonId+a.Ability));
        var f=facilities.Capture();
        parts.Add(f.oven+"/"+f.bike+"/"+f.expansion+"/"+f.workbench+"/"+f.menu+"/"+facilities.Advertisement);
        parts.Add(records.Count.ToString());
        return string.Join("|",parts);
    }
    private string Hint()
    {
        if(crew.Count==0)return "内勤がいないと、ピザはすべてあなたが作ります。求人で内勤を探せます。";
        var weakest=roster.People.OrderBy(p=>p.LocalKnowledge).FirstOrDefault();
        if(weakest!=null && weakest.LocalKnowledge<.4f)return $"土地勘は、裏道を通って配達すると育ちます。{weakest.Name} は土地勘が低いので、訓練で伸ばすのも手です。";
        if(facilities.Workbenches<=2 && crew.Count>=2)return "内勤が2人なら、作業台を増やすと同時に作れる枚数が増えます。";
        return $"まとめて配達すると、1回の往復で最大{DeliveryPerson.MaximumCapacity}軒に届けられます。地図で順番を決めましょう。";
    }

    // ---- cards ----
    /// <summary>Rebuilds the cards now (normally they follow the data on the next frame).</summary>
    public void RefreshNow(){signature=Signature();Rebuild();}
    private bool Preparing=>clock.Phase==DayPhase.BeforeOpening;
    private void Rebuild()
    {
        // Detached first, so the new cards are the only ones under Content in this frame.
        for(int i=content.childCount-1;i>=0;i--){var old=content.GetChild(i);old.SetParent(null,false);Destroy(old.gameObject);}
        management=null;
        switch(SelectedTab)
        {
            case 0:BuildTeam();break;
            case 1:BuildApplicants();break;
            case 2:BuildFacilities();break;
            default:BuildManagement();break;
        }
    }
    /// <summary>A horizontal scroller filling Content; cards go into the returned row.</summary>
    private RectTransform Scroller()
    {
        var viewport=UiKit.Stretch(content,"Viewport");
        var mask=viewport.gameObject.AddComponent<RectMask2D>();mask.padding=new Vector4(-8,-12,-8,-16);
        var row=UiKit.Box(viewport,"Row",0,0,10,624);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content=row;
        scroll.horizontal=true;
        scroll.vertical=false;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.viewport=viewport;
        ScrollNavigation.Configure(scroll,true);
        return row;
    }
    private static T Place<T>(T prefab,Transform parent,string name,float x) where T:Component
    {
        var card=Instantiate(prefab,parent);card.name=name;
        var r=(RectTransform)card.transform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,0);
        return card;
    }
    private void BuildTeam()
    {
        var row=Scroller();float x=0;
        foreach(var person in roster.People){ShowCourier(Place(courierCard,row,"Courier_"+person.Name,x),person);x+=CardW+Gap;}
        foreach(var staff in crew.People){ShowKitchen(Place(kitchenCard,row,"Kitchen_"+staff.Name,x),staff,null);x+=CardW+Gap;}
        var add=Place(findTeammateCard,row,"FindTeammate",x);
        add.Show(recruitment.Applicants.Count+recruitment.KitchenApplicants.Count);
        add.SeeApplicants.Button.onClick.AddListener(()=>ShowTab(1));
        row.sizeDelta=new Vector2(x+CardW+8,624);
    }
    private void ShowCourier(CourierCardView card,DeliveryPerson person)
    {
        var assignment=training.Assignments.FirstOrDefault(a=>a.PersonId==person.Id && !a.Completed);
        var status=assignment!=null?("訓練予約",T.cheese,T.line):person.Status==DeliveryStatus.Idle?("待機",T.basil,Color.white):(ShopStrings.Status(person.Status),T.info,Color.white);
        card.ShowHeader(person.Name,T.CourierColor(person.Name),(ShopStrings.Courier,T.paper,T.line),status);
        for(int i=0;i<5;i++){var a=(CourierAbility)i;card.ShowAbility(i,ShopStrings.Ability(a),person.Ability(a),person.Potential(a));}
        card.ShowCapacity(person.Capacity,(int)person.Potential(CourierAbility.Capacity));
        if(assignment!=null)
        {
            bool capacity=assignment.Ability==CourierAbility.Capacity;
            card.ShowTraining($"{ShopStrings.Ability(assignment.Ability)} +{(capacity?"1":"0.1")} を訓練",$"営業の前半は休み。費用 {UiKit.Yen(assignment.Paid)}");
        }
        else
        {
            card.ShowTraining(null,null);
            bool canGrow=progression!=null && progression.CanGrow(person);
            var best=progression!=null?progression.ClosestGrowth(person):CourierAbility.Driving;
            float fraction=progression!=null?progression.ExperienceFraction(person,best):0;
            card.ShowExperience(canGrow?ShopStrings.GrowthExperience(best):ShopStrings.GrowthMaximum,
                canGrow?fraction:1,canGrow?ShopStrings.GrowthNext(fraction,progression.GrowthPerLevel):"");
        }
        card.ShowWage(UiKit.Yen(economy.DailyWage(person)));
        card.Train.Button.onClick.AddListener(()=>OpenTraining(person));
        card.CancelTraining.Button.onClick.AddListener(()=>training.Cancel(person));
        card.Fire.Button.onClick.AddListener(()=>recruitment.Fire(person));
        card.Train.Interactable=Preparing && person.IsAvailable;
        card.CancelTraining.Interactable=Preparing;
        card.Fire.Interactable=Preparing && assignment==null && roster.People.Count>1;
    }
    private static string KitchenRank(KitchenStaff s)=>s.Speed+s.Care<1f?"見習い":s.Speed+s.Care<1.4f?"一人前":"ベテラン";
    /// <summary>A kitchen staff member; with an applicant, the card offers 雇う instead of 解雇.</summary>
    private void ShowKitchen(KitchenCardView card,KitchenStaff staff,KitchenApplicant applicant)
    {
        card.Show(staff.Name,KitchenRank(staff),staff.Speed,staff.Care,ShopStrings.KitchenInfo,UiKit.Yen(economy.DailyWage(staff)));
        string reason=applicant!=null?UiStrings.Japanese(recruitment.HireRejection(applicant)):"";
        card.ShowMode(applicant!=null,"採用 "+UiKit.Yen(recruitment.HiringCost(staff)),reason);
        if(applicant!=null)
        {
            card.Hire.Button.onClick.AddListener(()=>recruitment.Hire(applicant));
            card.Hire.Interactable=Preparing && reason.Length==0;
        }
        else{card.Fire.Button.onClick.AddListener(()=>recruitment.Fire(staff));card.Fire.Interactable=Preparing;}
    }
    private void BuildApplicants()
    {
        var row=Scroller();float x=0;
        foreach(var applicant in recruitment.Applicants)
        {
            ShowApplicant(Place(applicantCard,row,"Applicant_"+applicant.Person.Name,x),applicant);
            x+=CardW+Gap;
        }
        foreach(var k in recruitment.KitchenApplicants)
        {
            ShowKitchen(Place(kitchenCard,row,"KitchenApplicant_"+k.Staff.Name,x),k.Staff,k);
            x+=CardW+Gap;
        }
        if(x==0)UiKit.Label(row,"Empty","今日の求人はありません。明日また来ます。",0,260,800,40,24,T.inkSub,FontRole.Black);
        row.sizeDelta=new Vector2(Mathf.Max(x,10),624);
    }
    private void ShowApplicant(ApplicantCardView card,Applicant applicant)
    {
        var person=applicant.Person;
        card.ShowHeader(person.Name,T.CourierColor(person.Name),(ShopStrings.Courier,T.paper,T.line),(ShopStrings.Archetype(person.Archetype),T.cheese,T.line));
        for(int i=0;i<5;i++){var a=(CourierAbility)i;card.ShowAbility(i,ShopStrings.Ability(a),person.Ability(a),person.Potential(a));}
        card.ShowDetails($"{person.Capacity}（最大 {person.Potential(CourierAbility.Capacity):F0}）",
            "得意: "+string.Join("・",applicant.Strengths.Select(i=>ShopStrings.Ability((CourierAbility)i))),
            ShopStrings.Wage+" "+UiKit.Yen(economy.DailyWage(person)),"採用 "+UiKit.Yen(recruitment.HiringCost(person)));
        card.ShowRejection(UiStrings.Japanese(recruitment.HireRejection(applicant)));
        card.Hire.Button.onClick.AddListener(()=>recruitment.Hire(applicant));
    }
    private void BuildFacilities()
    {
        var row=Scroller();
        var items=new[]{(FacilityType.Oven,"fac_oven"),(FacilityType.Workbench,"fac_bench"),(FacilityType.Bike,"fac_bike"),(FacilityType.Menu,"ticket"),(FacilityType.Advertising,"fac_ad")};
        for(int i=0;i<items.Length;i++)ShowFacility(Place(facilityCard,row,"Facility_"+items[i].Item1,i*(FacilityW+FacilityGap)),items[i].Item1,items[i].Item2);
        row.sizeDelta=new Vector2(items.Length*(FacilityW+FacilityGap),624);
    }
    private string FacilityLevel(FacilityType type)=>type switch
    {
        FacilityType.Oven=>ShopStrings.FacilityLevel(facilities.Ovens,facilities.MaximumLevel(type)),
        FacilityType.Workbench=>ShopStrings.FacilityLevel(facilities.Workbenches,facilities.MaximumLevel(type)),
        FacilityType.Bike=>ShopStrings.FacilityLevel(facilities.BikeLevel,facilities.MaximumLevel(type)),
        FacilityType.Menu=>$"いま {facilities.MenuUpgrades+1}種類",
        FacilityType.Expansion=>$"上限 {facilities.MaximumPeople}人",
        _=>facilities.Advertisement>0?$"今日 x{facilities.AdvertisingDemand(facilities.Advertisement):0.00}":"今日はなし"
    };
    private string FacilityEffect(FacilityType type)=>type switch
    {
        FacilityType.Oven=>"窯を1台増やす\n同時に焼ける枚数 +1",
        FacilityType.Workbench=>"作業台を1台増やす\n同時に作れる枚数 +1",
        FacilityType.Bike=>$"速さ x{facilities.NextBikeSpeed:0.00}\n事故率 x{facilities.NextBikeRisk:0.00}",
        FacilityType.Menu=>facilities.MenuUpgrades==0?"ペパロニを解禁\n基本の得点 120":"シーフードを解禁\n基本の得点 150",
        FacilityType.Expansion=>"雇える人数 +1",
        _=>"今日の注文が増える\n(その日だけ)"
    };
    private void ShowFacility(FacilityCardView card,FacilityType type,string picture)
    {
        card.Show(UiKit.Art(picture),ShopStrings.Facility(type),FacilityLevel(type),facilities.IsMaximum(type) && type!=FacilityType.Advertising?(type==FacilityType.Menu?ShopStrings.MenuCompleted:ShopStrings.FacilityCompleted):FacilityEffect(type));
        if(type==FacilityType.Advertising)
        {
            card.ShowTiers(facilities.AdvertisingTiers,
                t=>$"{(t==1?"小":t==2?"中":"大")}  {UiKit.Yen(facilities.Price(type,t))}  x{facilities.AdvertisingDemand(t):0.00}",
                t=>Preparing && facilities.Rejection(type,t).Length==0);
            for(int tier=1;tier<=facilities.AdvertisingTiers;tier++)
            {
                int t=tier;
                card.Tier(t).Button.onClick.AddListener(()=>facilities.Purchase(FacilityType.Advertising,t));
            }
            return;
        }
        bool max=facilities.IsMaximum(type);string reason=facilities.Rejection(type);
        card.ShowPurchase(max?"最大です":"次の費用 "+UiKit.Yen(facilities.Price(type)),max?"":$"維持費 +{UiKit.Yen((decimal)facilities.MaintenanceOf(type))}/日",
            max?"":UiStrings.Japanese(reason),Preparing && reason.Length==0);
        card.Buy.Button.onClick.AddListener(()=>facilities.Purchase(type));
    }
    private void BuildManagement()
    {
        management=Place(managementPanel,content,"Management",0);
        UiKit.BindSlider(management.Promise,v=>spawner.SetPromiseSeconds(v));
        var shown=records.Skip(Mathf.Max(0,records.Count-7)).ToList();
        management.ShowHistory($"累計売上 {UiKit.Yen(campaign.TotalRevenue)}  ・  最高売上 {UiKit.Yen(campaign.BestDailyRevenue)}",shown);
        ShowForecast();
    }

    // ---- training dialog ----
    public void OpenTraining(DeliveryPerson person)
    {
        trainingDialog.Show(person.Name);
        for(int i=0;i<6;i++)
        {
            var a=(CourierAbility)i;bool cap=a==CourierAbility.Capacity;float now=person.Ability(a),max=person.Potential(a);
            string after=cap?$"{now:F0} → {Mathf.Min(max,now+1):F0}":$"{UiKit.Ability(now)} → {UiKit.Ability(Mathf.Min(max,now+.1f))}";
            var button=trainingDialog.Ability(i);
            button.Text=$"{ShopStrings.Ability(a)}  {after}  {UiKit.Yen(training.Cost(person,a))}";
            button.Interactable=training.Rejection(person,a).Length==0;
            button.Button.onClick.RemoveAllListeners();
            button.Button.onClick.AddListener(()=>{if(training.Request(person,a))CloseTraining();});
        }
        UiKit.Show(dialog,true);dialog.SetAsLastSibling();
    }
    public void CloseTraining()=>UiKit.Show(dialog,false);
}
