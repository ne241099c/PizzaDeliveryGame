using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>開店準備の事務室 (UI_Mock/Hub.png): top bar, tabs, team/applicant/facility/management cards and the start button.
/// Presentation only: every action calls the existing managers. The frame (bars, tabs, start button) is the prefab
/// Assets/Prefabs/UI/HubView.prefab (ScreenPrefabs); the cards in Content follow the data and are built here.</summary>
public sealed class HubView : MonoBehaviour
{
    private CampaignManager campaign;private GameClock clock;private BusinessEconomy economy;private DeliveryPersonSelector roster;private KitchenCrew crew;
    private RecruitmentManager recruitment;private TrainingManager training;private FacilityManager facilities;private OrderSpawner spawner;private ReputationManager reputation;
    private TitleMenuUI menu;private CourierProgression progression;private ScreenRouter router;private CookingManager cooking;
    private UITheme T=>UiKit.T;
    [SerializeField] private RectTransform content,dialog;
    [SerializeField] private TextMeshProUGUI dayText,phaseText,cashText,reputationText,hintText,noteText,forecastWages,forecastFixed,forecastMaintenance,forecastTraining,promiseText,messageText;
    [SerializeField] private RectTransform cashDelta;[SerializeField] private UiBar reputationBar;
    [SerializeField] private StickerButton[] tabs=new StickerButton[4];
    /// <summary>Count badges on the staff and applicant tabs ([2] and [3] stay empty).</summary>
    [SerializeField] private RectTransform[] tabBadges=new RectTransform[4];
    [SerializeField] private StickerButton startButton,menuButton;
    [SerializeField] private Slider promiseSlider;
    private RectTransform dialogCard;private Slider managementSlider;private TextMeshProUGUI managementValue,managementDetail;
    private string signature="";private float messageUntil;private string lastMessage="";
    private readonly List<(int day,decimal revenue,decimal profit,decimal cash)> records=new();
    public int SelectedTab {get;private set;}
    public RectTransform Content=>content;

    private void Awake()
    {
        campaign=Find<CampaignManager>();clock=Find<GameClock>();economy=Find<BusinessEconomy>();roster=Find<DeliveryPersonSelector>();crew=Find<KitchenCrew>();
        recruitment=Find<RecruitmentManager>();training=Find<TrainingManager>();facilities=Find<FacilityManager>();spawner=Find<OrderSpawner>();reputation=Find<ReputationManager>();
        menu=Find<TitleMenuUI>();progression=Find<CourierProgression>();router=Find<ScreenRouter>();cooking=Find<CookingManager>();
    }
    private void Start()
    {
        for(int i=0;i<tabs.Length;i++){int tab=i;tabs[i].Button.onClick.AddListener(()=>ShowTab(tab));}
        menuButton.Button.onClick.AddListener(()=>menu.ShowMenu());
        startButton.Button.onClick.AddListener(()=>campaign.StartBusiness());
        UiKit.BindSlider(promiseSlider,v=>spawner.SetPromiseSeconds(v));
        UiKit.Show(dialog,false);ShowTab(0);
    }
    private static TComp Find<TComp>() where TComp:UnityEngine.Object=>FindFirstObjectByType<TComp>();
    private void OnEnable(){if(clock!=null)clock.OnDayEnded+=Record;}
    private void OnDisable(){if(clock!=null)clock.OnDayEnded-=Record;}
    private void Record()
    {
        var l=economy.Ledger;if(campaign.DayNumber==1)records.Clear();
        records.RemoveAll(r=>r.day==campaign.DayNumber);records.Add((campaign.DayNumber,l.Revenue,l.Profit,economy.Cash));
    }

    public void ShowTab(int tab){SelectedTab=tab;for(int i=0;i<tabs.Length;i++)tabs[i].Kind=i==tab?StickerKind.Primary:StickerKind.Secondary;signature="";}

    private void Update()
    {
        if(UIThemeDriver.RenderingSuspended)return;
        if(router!=null && router.Current!=ScreenId.Hub)return;
        dayText.text=campaign.DayNumber+ShopStrings.Day;
        cashText.text=UiKit.Yen(economy.Cash);
        var previous=records.LastOrDefault(r=>r.day==campaign.DayNumber-1);decimal delta=records.Count>0 && previous.day>0?previous.profit:0;
        UiKit.SetBadge(cashDelta,(delta>=0?"▲ ":"▼ ")+UiKit.Signed(delta).Replace("¥",""),delta>=0?T.basil:T.berry,Color.white);
        cashDelta.gameObject.SetActive(previous.day>0);
        reputationText.text=Mathf.RoundToInt(reputation.Value).ToString();reputationBar.Set(reputation.Value/reputation.Maximum);
        UiKit.SetBadge(tabBadges[0],$"{recruitment.TeamCount}人",T.paper,T.line,56);tabBadges[0].anchoredPosition=new Vector2(236-14-tabBadges[0].sizeDelta.x,-26);
        UiKit.SetBadge(tabBadges[1],(recruitment.Applicants.Count+recruitment.KitchenApplicants.Count)+"人",T.cheese,T.line,56);tabBadges[1].anchoredPosition=new Vector2(236-14-tabBadges[1].sizeDelta.x,-26);
        noteText.text=ShopStrings.TeamNote;
        decimal trainingFees=training.Assignments.Where(a=>!a.Completed).Sum(a=>a.Paid);
        forecastWages.text=UiKit.Yen(economy.TeamWages);forecastFixed.text=UiKit.Yen(economy.BaseFixedCost(campaign.DayNumber));
        forecastMaintenance.text=UiKit.Yen(facilities.Maintenance);forecastTraining.text=UiKit.Yen(trainingFees);
        promiseText.text=$"{spawner.PromiseSeconds:F0}<size=24>秒</size>";UiKit.SyncPromise(promiseSlider,spawner,clock);UiKit.SyncPromise(managementSlider,spawner,clock);if(managementValue!=null)managementValue.text=$"{spawner.PromiseSeconds:F0}<size=28>秒</size>";if(managementDetail!=null)managementDetail.text=$"短いほど注文が増え、長いほど減ります。\n需要 x{spawner.DemandFactor(spawner.PromiseSeconds):0.00} ・ 約{spawner.ExpectedInterval:F0}秒ごとに注文\n下限 {spawner.MinimumPromiseSeconds:F1}秒（往復と調理の時間）\n評判 {reputation.Value:F0} ・ 注文 x{reputation.DemandFactor:0.00}";
        startButton.Interactable=clock.Phase==DayPhase.BeforeOpening && !campaign.IsBankrupt;
        string message=FirstNonEmpty(recruitment.LastMessage,training.LastMessage,facilities.LastMessage);
        if(message!=lastMessage){lastMessage=message;messageUntil=Time.unscaledTime+4;}
        messageText.text=Time.unscaledTime<messageUntil?Japanese(message):"";
        hintText.text=FindFirstObjectByType<MapManager>().TodayCityInfo+"  ／  "+Hint();
        string next=Signature();if(next!=signature){signature=next;Rebuild();}
    }
    private static string FirstNonEmpty(params string[] values)=>values.FirstOrDefault(v=>!string.IsNullOrEmpty(v))??"";
    private static string Japanese(string message)=>UiStrings.Japanese(message);
    private string Signature()
    {
        var parts=new List<string>{SelectedTab.ToString(),clock.Phase.ToString(),recruitment.MaximumPeople.ToString(),((long)economy.Cash).ToString()};
        parts.AddRange(roster.People.Select(p=>UiKit.Ref(p)+p.Capacity+p.Driving.ToString("F2")+p.LocalKnowledge.ToString("F2")+p.Status));
        parts.AddRange(crew.People.Select(c=>UiKit.Ref(c)));parts.AddRange(recruitment.Applicants.Select(a=>UiKit.Ref(a)));parts.AddRange(recruitment.KitchenApplicants.Select(a=>UiKit.Ref(a)));
        parts.AddRange(training.Assignments.Select(a=>a.PersonId+a.Ability));parts.Add(facilities.Capture().oven+"/"+facilities.Capture().bike+"/"+facilities.Capture().expansion+"/"+facilities.Capture().workbench+"/"+facilities.Advertisement);
        parts.Add(records.Count.ToString());
        return string.Join("|",parts);
    }
    private string Hint()
    {
        if(crew.Count==0)return "内勤がいないと、ピザはすべてあなたが作ります。求人で内勤を探せます。";
        var weakest=roster.People.OrderBy(p=>p.LocalKnowledge).FirstOrDefault();
        if(weakest!=null && weakest.LocalKnowledge<.4f)return $"土地勘は、裏道を通って配達すると育ちます。{weakest.Name} は土地勘が低いので、訓練で伸ばすのも手です。";
        if(facilities.Workbenches<=2 && crew.Count>=2)return "内勤が2人なら、作業台を増やすと同時に作れる枚数が増えます。";
        return "まとめて配達すると、1回の往復で2軒に届けられます。地図で順番を決めましょう。";
    }

    // ---- cards ----
    private void Rebuild()
    {
        for(int i=content.childCount-1;i>=0;i--)Destroy(content.GetChild(i).gameObject);
        switch(SelectedTab)
        {
            case 0:BuildTeam();break;case 1:BuildApplicants();break;case 2:BuildFacilities();break;default:BuildManagement();break;
        }
    }
    private RectTransform Scroller(out RectTransform rowContent)
    {
        var viewport=UiKit.Stretch(content,"Viewport");var mask=viewport.gameObject.AddComponent<RectMask2D>();mask.padding=new Vector4(-8,-12,-8,-16);
        rowContent=UiKit.Box(viewport,"Row",0,0,10,624);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=rowContent;scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=40;
        return viewport;
    }
    private const float CardW=356,CardH=610,Gap=28;
    private void BuildTeam()
    {
        Scroller(out var row);float x=0;
        foreach(var person in roster.People){CourierCard(row,person,x);x+=CardW+Gap;}
        foreach(var staff in crew.People){KitchenCard(row,staff,x,false,null);x+=CardW+Gap;}
        AddCard(row,x);x+=CardW;row.sizeDelta=new Vector2(x+8,624);
    }
    private Color CourierColor(DeliveryPerson p)=>T.CourierColor(p.Name);
    private static string Initial(string name)=>string.IsNullOrEmpty(name)?"?":name.Substring(0,1).ToUpperInvariant();
    private RectTransform CardShell(Transform parent,float x,string name)=>UiKit.Card(parent,name,x,0,CardW,CardH,T.paper);
    private void Header(RectTransform card,string name,Color color,params (string text,Color fill,Color ink)[] badges)
    {
        UiKit.Avatar(card,"Avatar",20,20,92,color,Initial(name));
        UiKit.Label(card,"Name",name,126,14,214,54,34,T.line,FontRole.Display);
        float bx=126;foreach(var b in badges){var badge=UiKit.Badge(card,"Badge",b.text,bx,72,b.fill,b.ink,30,16);bx+=badge.sizeDelta.x+8;}
    }
    private void AbilityRow(RectTransform card,int index,string label,float value,float potential,float y,Color fill,float scale=1)
    {
        UiKit.Label(card,"AbilityName"+index,label,22,y,84,28,18,T.line,FontRole.Black);
        var bar=UiKit.Bar(card,"AbilityBar"+index,104,y+5,182,20,fill,true);bar.Set(value/scale,potential/scale);
        UiKit.Label(card,"AbilityValue"+index,UiKit.Ability(value),288,y,50,28,20,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight);
    }
    private void CourierCard(Transform parent,DeliveryPerson person,float x)
    {
        var card=CardShell(parent,x,"Courier_"+person.Name);
        var assignment=training.Assignments.FirstOrDefault(a=>a.PersonId==person.Id && !a.Completed);
        var status=assignment!=null?("訓練予約",T.cheese,T.line):person.Status==DeliveryStatus.Idle?("待機",T.basil,Color.white):(ShopStrings.Status(person.Status),T.info,Color.white);
        Header(card,person.Name,CourierColor(person),(ShopStrings.Courier,T.paper,T.line),status);
        for(int i=0;i<5;i++){var a=(CourierAbility)i;AbilityRow(card,i,ShopStrings.Ability(a),person.Ability(a),person.Potential(a),130+i*34,T.barFill);}
        UiKit.Label(card,"CapacityName",ShopStrings.Ability(CourierAbility.Capacity),22,300,84,28,18,T.line,FontRole.Black);
        for(int s=0;s<Mathf.Max(person.Capacity,(int)person.Potential(CourierAbility.Capacity));s++)
        {
            var slot=UiKit.Box(card,"Slot"+s,104+s*52,302,44,26);
            if(s<person.Capacity){UiKit.Fill(UiKit.Stretch(slot,"Edge"),T.line,8);UiKit.Fill(UiKit.Stretch(slot,"Face",2,2,2,2),T.barFill,6);}
            else UiKit.Dashed(slot,T.inkSub,6,4,2);
        }
        UiKit.Label(card,"CapacityValue",person.Capacity.ToString(),288,300,50,28,20,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        float y=340;
        if(assignment!=null)
        {
            var box=UiKit.Card(card,"TrainingBox",20,y,316,62,T.cheeseLight,10,0,2);
            bool capacity=assignment.Ability==CourierAbility.Capacity;
            UiKit.Label(box,"Title",$"{ShopStrings.Ability(assignment.Ability)} +{(capacity?"1":"0.1")} を訓練",14,4,290,28,18,T.line,FontRole.Black);
            UiKit.Label(box,"Detail",$"営業の前半は休み。費用 {UiKit.Yen(assignment.Paid)}",14,30,290,26,16,T.inkSub,FontRole.Bold);y+=74;
        }
        else
        {
            var best=Enumerable.Range(0,5).Select(i=>(CourierAbility)i).Where(a=>person.Ability(a)<person.Potential(a)).OrderByDescending(a=>person.Experience(a)).FirstOrDefault();
            float threshold=progression!=null?progression.ExperiencePerLevel:200;
            UiKit.Label(card,"XpName","経験 "+ShopStrings.Ability(best),22,y,110,26,16,T.inkSub,FontRole.Black);
            var xp=UiKit.Bar(card,"XpBar",132,y+6,110,16,T.basil);xp.Set(person.Experience(best)/threshold);
            UiKit.Label(card,"XpNext",$"次 +{(progression!=null?progression.GrowthPerLevel:.02f):0.00}まで",248,y,96,26,15,T.inkSub,FontRole.Bold);y+=44;
        }
        UiKit.DashedLine(card,20,y,316,T.disabled);
        UiKit.Label(card,"WageName",ShopStrings.Wage,22,y+16,100,44,20,T.line,FontRole.Black);
        UiKit.Label(card,"Wage",UiKit.Yen(economy.DailyWage(person)),130,y+10,206,52,36,T.line,FontRole.Display,TextAlignmentOptions.MidlineRight);
        bool prep=clock.Phase==DayPhase.BeforeOpening;
        if(assignment!=null)
        {
            var cancel=UiKit.Button(card,"CancelTraining",ShopStrings.CancelTraining,StickerKind.Secondary,20,CardH-96,200,70,22,()=>training.Cancel(person));cancel.Interactable=prep;
            var fire=UiKit.Button(card,"Fire",ShopStrings.Fire,StickerKind.Danger,232,CardH-96,104,70,22,null);fire.Interactable=false;
        }
        else
        {
            var train=UiKit.Button(card,"Train",ShopStrings.Train,StickerKind.Secondary,20,CardH-96,180,70,24,()=>OpenTraining(person));train.Interactable=prep && person.IsAvailable;
            var fire=UiKit.Button(card,"Fire",ShopStrings.Fire,StickerKind.Danger,212,CardH-96,124,70,24,()=>recruitment.Fire(person));fire.Interactable=prep && roster.People.Count>1;
        }
    }
    private static string KitchenRank(KitchenStaff s)=>s.Speed+s.Care<1f?"見習い":s.Speed+s.Care<1.4f?"一人前":"ベテラン";
    private void KitchenCard(Transform parent,KitchenStaff staff,float x,bool applicant,KitchenApplicant hire)
    {
        var card=CardShell(parent,x,(applicant?"KitchenApplicant_":"Kitchen_")+staff.Name);
        Header(card,staff.Name,T.info,(ShopStrings.Kitchen,T.info,Color.white),(KitchenRank(staff),T.paper,T.line));
        UiKit.Label(card,"SpeedName",ShopStrings.Speed,22,130,84,28,18,T.line,FontRole.Black);var speed=UiKit.Bar(card,"Speed",104,135,182,20,T.info);speed.Set(staff.Speed);
        UiKit.Label(card,"SpeedValue",UiKit.Ability(staff.Speed),288,130,50,28,20,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        UiKit.Label(card,"CareName",ShopStrings.Care,22,164,84,28,18,T.line,FontRole.Black);var care=UiKit.Bar(card,"Care",104,169,182,20,T.info);care.Set(staff.Care);
        UiKit.Label(card,"CareValue",UiKit.Ability(staff.Care),288,164,50,28,20,T.line,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        var info=UiKit.Card(card,"Info",20,214,316,110,T.infoLight,10,0,2,T.info);
        UiKit.Label(info,"Text",ShopStrings.KitchenInfo,14,8,288,96,16,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        float y=346;UiKit.DashedLine(card,20,y,316,T.disabled);
        UiKit.Label(card,"WageName",ShopStrings.Wage,22,y+16,100,44,20,T.line,FontRole.Black);
        UiKit.Label(card,"Wage",UiKit.Yen(economy.DailyWage(staff)),130,y+10,206,52,36,T.line,FontRole.Display,TextAlignmentOptions.MidlineRight);
        bool prep=clock.Phase==DayPhase.BeforeOpening;
        if(applicant)
        {
            UiKit.Label(card,"HireCost","採用 "+UiKit.Yen(recruitment.HiringCost(staff)),22,y+70,316,30,18,T.inkSub,FontRole.Black);
            string reason=recruitment.HireRejection(hire);
            var b=UiKit.Button(card,"Hire",ShopStrings.Hire,StickerKind.Primary,20,CardH-96,316,70,26,()=>recruitment.Hire(hire));b.Interactable=prep && reason.Length==0;
            if(reason.Length>0)UiKit.Label(card,"Reason",Japanese(reason),22,CardH-140,316,36,15,T.berry,FontRole.Bold,TextAlignmentOptions.MidlineLeft,true);
        }
        else
        {
            var train=UiKit.Button(card,"TrainLater","訓練(準備中)",StickerKind.Secondary,20,CardH-96,180,70,20,null);train.Interactable=false;
            var fire=UiKit.Button(card,"Fire",ShopStrings.Fire,StickerKind.Danger,212,CardH-96,124,70,24,()=>recruitment.Fire(staff));fire.Interactable=prep;
        }
    }
    private void AddCard(Transform parent,float x)
    {
        var card=UiKit.Box(parent,"FindTeammate",x,0,CardW,CardH);UiKit.Fill(UiKit.Stretch(card,"Face"),new Color(T.paper.r,T.paper.g,T.paper.b,.45f),T.cardRadius);
        card.sizeDelta=new Vector2(CardW,CardH);UiKit.Dashed(card,T.line,16,10,3);
        var plus=UiKit.Box(card,"Plus",CardW/2-50,170,100,100);UiKit.Dashed(plus,T.line,10,8,3);UiKit.Icon(plus,"Icon","icon_plus",25,25,50,T.line);
        UiKit.Label(card,"Title",ShopStrings.FindTeammate,0,290,CardW,50,32,T.line,FontRole.Display,TextAlignmentOptions.Center);
        UiKit.Label(card,"Detail",$"人数制限なし\n求人は{recruitment.Applicants.Count+recruitment.KitchenApplicants.Count}人 来ています",0,350,CardW,64,18,T.inkSub,FontRole.Black,TextAlignmentOptions.Center,true);
        UiKit.Button(card,"SeeApplicants",ShopStrings.SeeApplicants,StickerKind.Secondary,58,430,240,70,24,()=>ShowTab(1));
    }
    private void BuildApplicants()
    {
        Scroller(out var row);float x=0;
        foreach(var applicant in recruitment.Applicants){ApplicantCard(row,applicant,x);x+=CardW+Gap;}
        foreach(var k in recruitment.KitchenApplicants){KitchenCard(row,k.Staff,x,true,k);x+=CardW+Gap;}
        if(x==0)UiKit.Label(row,"Empty","今日の求人はありません。明日また来ます。",0,260,800,40,24,T.inkSub,FontRole.Black);
        row.sizeDelta=new Vector2(Mathf.Max(x,10),624);
    }
    private void ApplicantCard(Transform parent,Applicant applicant,float x)
    {
        var person=applicant.Person;var card=CardShell(parent,x,"Applicant_"+person.Name);
        Header(card,person.Name,T.CourierColor(person.Name),(ShopStrings.Courier,T.paper,T.line),(ShopStrings.Archetype(person.Archetype),T.cheese,T.line));
        for(int i=0;i<5;i++){var a=(CourierAbility)i;AbilityRow(card,i,ShopStrings.Ability(a),person.Ability(a),person.Potential(a),130+i*34,T.barFill);}
        UiKit.Label(card,"CapacityName",ShopStrings.Ability(CourierAbility.Capacity),22,300,84,28,18,T.line,FontRole.Black);
        UiKit.Label(card,"CapacityValue",$"{person.Capacity}（最大 {person.Potential(CourierAbility.Capacity):F0}）",104,300,232,28,18,T.line,FontRole.Bold);
        UiKit.Label(card,"Strengths","得意: "+string.Join("・",applicant.Strengths.Select(i=>ShopStrings.Ability((CourierAbility)i))),22,340,316,28,18,T.basil,FontRole.Black);
        float y=384;UiKit.DashedLine(card,20,y,316,T.disabled);
        UiKit.Label(card,"WageName",ShopStrings.Wage+" "+UiKit.Yen(economy.DailyWage(person)),22,y+12,316,34,20,T.line,FontRole.Black);
        UiKit.Label(card,"HireCost","採用 "+UiKit.Yen(recruitment.HiringCost(person)),22,y+46,316,34,20,T.inkSub,FontRole.Black);
        string reason=recruitment.HireRejection(applicant);
        var b=UiKit.Button(card,"Hire",ShopStrings.Hire,StickerKind.Primary,20,CardH-96,316,70,26,()=>recruitment.Hire(applicant));b.Interactable=reason.Length==0;
        if(reason.Length>0)UiKit.Label(card,"Reason",Japanese(reason),22,CardH-140,316,36,15,T.berry,FontRole.Bold,TextAlignmentOptions.MidlineLeft,true);
    }
    private void BuildFacilities()
    {
        var items=new[]{(FacilityType.Oven,"fac_oven"),(FacilityType.Workbench,"fac_bench"),(FacilityType.Bike,"fac_bike"),(FacilityType.Menu,"ticket"),(FacilityType.Advertising,"fac_ad")};
        float w=300;for(int i=0;i<items.Length;i++)FacilityCard(content,items[i].Item1,items[i].Item2,i*(w+22),w);
    }
    private void FacilityCard(Transform parent,FacilityType type,string picture,float x,float w)
    {
        var card=UiKit.Card(parent,"Facility_"+type,x,0,w,CardH,T.paper);
        UiKit.Picture(card,"Picture",picture,w/2-56,22,112,112);
        UiKit.Label(card,"Title",ShopStrings.Facility(type),0,146,w,44,30,T.line,FontRole.Display,TextAlignmentOptions.Center);
        string level=type switch
        {
            FacilityType.Oven=>$"いま {facilities.Ovens}台",FacilityType.Workbench=>$"いま {facilities.Workbenches}台",FacilityType.Bike=>$"Lv {facilities.BikeLevel}",
            FacilityType.Menu=>$"いま {facilities.MenuUpgrades+1}種類",FacilityType.Expansion=>$"上限 {facilities.MaximumPeople}人",_=>facilities.Advertisement>0?$"今日 x{facilities.AdvertisingDemand(facilities.Advertisement):0.00}":"今日はなし"
        };
        var levelBadge=UiKit.Badge(card,"Level",level,0,196,T.cheeseLight,T.line,32,18,120);levelBadge.anchoredPosition=new Vector2((w-levelBadge.sizeDelta.x)/2f,-196);
        string effect=type switch
        {
            FacilityType.Oven=>"窯を1台増やす\n同時に焼ける枚数 +1",FacilityType.Workbench=>"作業台を1台増やす\n同時に作れる枚数 +1",
            FacilityType.Bike=>$"速さ x{facilities.NextBikeSpeed:0.00}\n事故率 x{facilities.NextBikeRisk:0.00}",FacilityType.Menu=>facilities.MenuUpgrades==0?"ペパロニを解禁\n基本の得点 120":"シーフードを解禁\n基本の得点 150",FacilityType.Expansion=>"雇える人数 +1",_=>"今日の注文が増える\n(その日だけ)"
        };
        UiKit.Label(card,"Effect",effect,20,244,w-40,80,19,T.line,FontRole.Bold,TextAlignmentOptions.Top,true);
        bool prep=clock.Phase==DayPhase.BeforeOpening;
        if(type==FacilityType.Advertising)
        {
            for(int tier=1;tier<=facilities.AdvertisingTiers;tier++)
            {
                int t=tier;string text=$"{(t==1?"小":t==2?"中":"大")}  {UiKit.Yen(facilities.Price(type,t))}  x{facilities.AdvertisingDemand(t):0.00}";
                var b=UiKit.Button(card,"Ad"+t,text,StickerKind.Secondary,20,330+(t-1)*86,w-40,68,20,()=>facilities.Purchase(FacilityType.Advertising,t));
                b.Interactable=prep && facilities.Rejection(type,t).Length==0;
            }
            return;
        }
        bool max=facilities.IsMaximum(type);
        UiKit.Label(card,"Price",max?"最大です":"次の費用 "+UiKit.Yen(facilities.Price(type)),20,340,w-40,36,22,T.line,FontRole.Black,TextAlignmentOptions.Center);
        UiKit.Label(card,"Maintenance",max?"":$"維持費 +{UiKit.Yen((decimal)facilities.MaintenanceOf(type))}/日",20,380,w-40,30,18,T.inkSub,FontRole.Bold,TextAlignmentOptions.Center);
        string reason=facilities.Rejection(type);
        var buy=UiKit.Button(card,"Buy","購入",StickerKind.Primary,20,CardH-96,w-40,70,26,()=>facilities.Purchase(type));buy.Interactable=prep && reason.Length==0;
        if(reason.Length>0 && !max)UiKit.Label(card,"Reason",Japanese(reason),20,CardH-140,w-40,36,15,T.berry,FontRole.Bold,TextAlignmentOptions.Center,true);
    }
    private void BuildManagement()
    {
        var policy=UiKit.Card(content,"Policy",0,0,620,CardH,T.paper);
        UiKit.Label(policy,"Title",ShopStrings.Promise,28,24,560,48,32,T.line,FontRole.Display);
        managementSlider=UiKit.Slider(policy,"PromiseSlider",34,124,390,36,T.cheese,v=>spawner.SetPromiseSeconds(v));
        managementValue=UiKit.Label(policy,"Value",$"{spawner.PromiseSeconds:F0}<size=28>秒</size>",436,94,160,84,56,T.line,FontRole.Display,TextAlignmentOptions.Center);
        managementDetail=UiKit.Label(policy,"Detail","",28,206,564,200,20,T.line,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        var history=UiKit.Card(content,"History",650,0,938,CardH,T.paper);
        UiKit.Label(history,"Title","これまでの営業",28,24,600,48,32,T.line,FontRole.Display);
        UiKit.Label(history,"Totals",$"累計売上 {UiKit.Yen(campaign.TotalRevenue)}  ・  最高売上 {UiKit.Yen(campaign.BestDailyRevenue)}",28,80,880,32,20,T.inkSub,FontRole.Black);
        var shown=records.Skip(Mathf.Max(0,records.Count-7)).ToList();
        if(shown.Count==0){UiKit.Label(history,"Empty","まだ記録はありません。営業を始めましょう。",28,140,880,40,22,T.inkSub,FontRole.Black);return;}
        decimal scale=Math.Max(1,shown.Max(r=>Math.Abs(r.profit)));
        for(int i=0;i<shown.Count;i++)
        {
            var r=shown[i];float y=136+i*62;
            UiKit.Label(history,"Day"+i,r.day+ShopStrings.Day,28,y,100,48,22,T.line,FontRole.Black);
            UiKit.Label(history,"Revenue"+i,"売上 "+UiKit.Yen(r.revenue),130,y,220,48,20,T.line,FontRole.Bold);
            float width=(float)(Math.Abs(r.profit)/scale)*260;var bar=UiKit.Box(history,"Bar"+i,360,y+14,Mathf.Max(6,width),22);UiKit.Fill(bar,r.profit>=0?T.basil:T.berry,8);
            UiKit.Label(history,"Profit"+i,UiKit.Signed(r.profit),640,y,130,48,22,r.profit>=0?T.basil:T.berry,FontRole.Black,TextAlignmentOptions.MidlineRight);
            UiKit.Label(history,"Cash"+i,UiKit.Yen(r.cash),780,y,130,48,20,T.inkSub,FontRole.Bold,TextAlignmentOptions.MidlineRight);
        }
    }

    // ---- training dialog ----
    public void OpenTraining(DeliveryPerson person)
    {
        if(dialogCard!=null)Destroy(dialogCard.gameObject);
        dialogCard=UiKit.Card(dialog,"TrainingDialog",560,200,800,620,T.paper);
        UiKit.Label(dialogCard,"Title",$"{person.Name} の訓練",36,28,700,52,36,T.line,FontRole.Display);
        UiKit.Label(dialogCard,"Note","営業の前半は休みになり、その間は配達できません。",36,88,720,32,20,T.inkSub,FontRole.Black);
        for(int i=0;i<6;i++)
        {
            var a=(CourierAbility)i;bool cap=a==CourierAbility.Capacity;float now=person.Ability(a),max=person.Potential(a);
            string after=cap?$"{now:F0} → {Mathf.Min(max,now+1):F0}":$"{UiKit.Ability(now)} → {UiKit.Ability(Mathf.Min(max,now+.1f))}";
            string text=$"{ShopStrings.Ability(a)}  {after}  {UiKit.Yen(training.Cost(person,a))}";
            var b=UiKit.Button(dialogCard,"Train"+i,text,StickerKind.Secondary,36+(i%2)*372,140+(i/2)*104,356,84,22,()=>{if(training.Request(person,a))CloseTraining();});
            b.Interactable=training.Rejection(person,a).Length==0;
        }
        UiKit.Button(dialogCard,"Close","やめる",StickerKind.Secondary,560,520,204,72,24,CloseTraining);
        UiKit.Show(dialog,true);dialog.SetAsLastSibling();
    }
    public void CloseTraining(){UiKit.Show(dialog,false);if(dialogCard!=null){Destroy(dialogCard.gameObject);dialogCard=null;}}
}
