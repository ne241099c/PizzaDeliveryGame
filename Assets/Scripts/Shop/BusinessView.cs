using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum ShopZone { Kitchen, Handover, Dispatch }

/// <summary>営業中の店内シーン (UI_Mock/Main.png, ShopDispatch.png): one shop with a kitchen, a handover counter and
/// a full-screen dispatch map. The camera pans between zones; game rules stay in the existing managers.</summary>
public sealed partial class BusinessView : MonoBehaviour
{
    private ScreenRouter router;private GameClock clock;private BusinessEconomy economy;private ReputationManager reputation;private OrderSpawner spawner;
    private OrderListUI orders;private CookingManager cooking;private KitchenCrew crew;private DeliveryPersonSelector roster;private HandoverManager handover;
    private DeliveryFlowController flow;private DeliveryPlannerUI planner;private MapManager map;private TitleMenuUI menu;private FacilityManager facilities;private CampaignManager campaign;
    private UITheme T=>UiKit.T;
    private RectTransform world,hud,rail,railContent,dispatch,dragLayer,toasts;
    private CanvasGroup worldGroup,hudGroup,dispatchGroup;
    private TextMeshProUGUI dayText,timeText,cashText,reputationText,promiseText;
    private UiBar timeBar,reputationBar;private RectTransform cashDelta;
    // Unity must retain generated control references across an Editor script reload.
    [SerializeField, HideInInspector] private StickerButton[] zoneTabs=new StickerButton[3];
    private ShopHand hand;
    private readonly Dictionary<Order,int> numbers=new();private int nextNumber=1;
    private float pan,panFrom,panTo,panStarted;
    public ShopZone Zone {get;private set;}=ShopZone.Kitchen;
    public RectTransform MapArea {get;private set;}
    public const float WorldWidth=2880,HandoverPan=-960;

    private void Awake()
    {
        router=Find<ScreenRouter>();clock=Find<GameClock>();economy=Find<BusinessEconomy>();reputation=Find<ReputationManager>();spawner=Find<OrderSpawner>();
        orders=Find<OrderListUI>();cooking=Find<CookingManager>();crew=Find<KitchenCrew>();roster=Find<DeliveryPersonSelector>();handover=Find<HandoverManager>();
        flow=Find<DeliveryFlowController>();planner=Find<DeliveryPlannerUI>();map=Find<MapManager>();menu=Find<TitleMenuUI>();facilities=Find<FacilityManager>();campaign=Find<CampaignManager>();
        Build();
    }
    private static TComp Find<TComp>() where TComp:UnityEngine.Object=>FindFirstObjectByType<TComp>();
    private void OnEnable()
    {
        if(clock==null)return;clock.OnDayStarted+=DayStarted;orders.OrdersChanged+=AssignNumbers;flow.Returned+=Returned;flow.Message+=FlowMessage;
        if(router!=null)router.Changed+=ScreenChanged;
    }
    private void OnDisable()
    {
        if(clock==null)return;clock.OnDayStarted-=DayStarted;orders.OrdersChanged-=AssignNumbers;flow.Returned-=Returned;flow.Message-=FlowMessage;
        if(router!=null)router.Changed-=ScreenChanged;
        if(map!=null)map.PointerInputEnabled=true;
    }
    private void DayStarted(){numbers.Clear();nextNumber=1;observedOrders.Clear();cancelledNotices.Clear();dispatchOrdersSignature="";ShowZone(ShopZone.Kitchen,true);}
    private void AssignNumbers(){foreach(var o in orders.Orders){observedOrders.Add(o);if(!numbers.ContainsKey(o))numbers[o]=nextNumber++;}}
    public int Number(Order o){if(!numbers.TryGetValue(o,out int n)){AssignNumbers();numbers.TryGetValue(o,out n);}return n;}
    private void ScreenChanged(ScreenId id){if(id!=ScreenId.Business){hand?.Cancel();if(map!=null)map.PointerInputEnabled=true;SetMinimap(false);}}

    private void Build()
    {
        var root=(RectTransform)transform;
        MapArea=root.Find("MapArea") as RectTransform;if(MapArea==null)MapArea=UiKit.Stretch(root,"MapArea"); // scene-authored so MapViewportUI can reference it
        world=UiKit.Box(root,"World",0,0,WorldWidth,1080);worldGroup=UiKit.Group(world);
        BuildKitchen();BuildHandover();
        dispatch=UiKit.Stretch(root,"Dispatch");dispatchGroup=UiKit.Group(dispatch);BuildDispatch();
        BuildHud();BuildRail();
        // zone tabs (bottom centre in the shop; bottom left on the map)
        string[] names={ShopStrings.KitchenTab,ShopStrings.HandoverTab,ShopStrings.DispatchTab};
        var tabs=UiKit.Box(root,"ZoneTabs",590,968,740,96);
        for(int i=0;i<3;i++){var z=(ShopZone)i;zoneTabs[i]=UiKit.Button(tabs,"Zone"+i,names[i],StickerKind.Secondary,i*250,0,236,82,34,()=>ShowZone(z),FontRole.Display);}
        toasts=UiKit.Box(root,"Toasts",1450,330,420,300);
        dragLayer=UiKit.Stretch(root,"Hand");hand=dragLayer.gameObject.AddComponent<ShopHand>();hand.Init(dragLayer);
        ShowZone(ShopZone.Kitchen,true);
    }

    // ---- HUD ----
    private void BuildHud()
    {
        hud=UiKit.Card(transform,"Hud",32,20,1856,96,T.paper);hudGroup=UiKit.Group(hud);
        UiKit.Icon(hud,"Clock","icon_clock",28,24,46,T.line);
        dayText=UiKit.Label(hud,"Day","1日目 営業中",84,12,220,28,18,T.inkSub,FontRole.Black);
        timeText=UiKit.Label(hud,"Time","5:00",84,34,130,56,40,T.line,FontRole.Display);
        timeBar=UiKit.Bar(hud,"TimeBar",224,46,300,26,T.basil);
        UiKit.Icon(hud,"Coin","icon_coin",560,24,46,T.line);
        cashText=UiKit.Label(hud,"Cash","¥0",614,14,220,66,40,T.line,FontRole.Display);
        cashDelta=UiKit.Badge(hud,"CashDelta","▲ +0",820,30,T.basil,Color.white,34,18);
        UiKit.Icon(hud,"Star","icon_star",960,24,46,T.line);
        reputationText=UiKit.Label(hud,"Reputation","50",1014,14,80,66,38,T.line,FontRole.Display);
        reputationBar=UiKit.Bar(hud,"ReputationBar",1092,36,140,24,T.cheese);
        UiKit.Label(hud,"PromiseTitle",ShopStrings.Promise,1268,24,120,46,20,T.line,FontRole.Black);
        UiKit.Button(hud,"PromiseMinus","",StickerKind.Secondary,1392,16,58,58,20,()=>spawner.SetPromiseSeconds(spawner.PromiseSeconds-10),FontRole.Black,"icon_minus");
        promiseText=UiKit.Label(hud,"Promise","60秒",1454,14,120,66,36,T.line,FontRole.Display,TextAlignmentOptions.Center);
        UiKit.Button(hud,"PromisePlus","",StickerKind.Secondary,1578,16,58,58,20,()=>spawner.SetPromiseSeconds(spawner.PromiseSeconds+10),FontRole.Black,"icon_plus");
        UiKit.Button(hud,"MenuButton","",StickerKind.Secondary,1772,14,64,64,20,()=>menu.ShowMenu(),FontRole.Black,"icon_menu");
    }
    private void RefreshHud()
    {
        int seconds=Mathf.CeilToInt(clock.RemainingSeconds);
        string phase=clock.Phase==DayPhase.Open?ShopStrings.Open:ShopStrings.Closed;
        dayText.text=$"{campaign.DayNumber}{ShopStrings.Day} {phase}";timeText.text=$"{seconds/60}:{seconds%60:00}";
        float fraction=clock.RemainingSeconds/clock.BusinessDuration;timeBar.Set(fraction,-1,fraction<=T.closingWarning?T.berry:T.basil);
        cashText.text=UiKit.Yen(economy.Cash);UiKit.SetBadge(cashDelta,"▲ +"+UiKit.Yen(economy.DailyRevenue).Replace("¥",""),T.basil,Color.white);
        reputationText.text=Mathf.RoundToInt(reputation.Value).ToString();reputationBar.Set(reputation.Value/reputation.Maximum);
        promiseText.text=$"{spawner.PromiseSeconds:F0}<size=20>秒</size>";
    }

    // ---- ticket rail ----
    private readonly List<(Order order,RectTransform card)> tickets=new();private string railSignature="";
    private void BuildRail()
    {
        rail=UiKit.Box(transform,"TicketRail",0,124,1920,196);
        var rod=UiKit.Box(rail,"Rod",24,8,1872,14);UiKit.Fill(rod,T.steel,7);var rodEdge=UiKit.Stretch(rod,"Edge",0,0,0,0);UiKit.Fill(rodEdge,new Color(0,0,0,0),0);
        var viewport=UiKit.Box(rail,"Viewport",24,10,1872,186);viewport.gameObject.AddComponent<RectMask2D>().padding=new Vector4(-4,-4,-4,-10);
        railContent=UiKit.Box(viewport,"Content",0,0,10,186);var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=railContent;scroll.horizontal=true;scroll.vertical=false;scroll.movementType=ScrollRect.MovementType.Clamped;
    }
    private void RefreshRail()
    {
        var shown=orders.Orders.OrderBy(o=>o.Deadline).ThenBy(o=>Number(o)).ToList();
        string sig=string.Join(",",shown.Select(o=>o.Id));
        if(sig!=railSignature)
        {
            railSignature=sig;foreach(var t in tickets)Destroy(t.card.gameObject);tickets.Clear();
            for(int i=0;i<shown.Count;i++)tickets.Add((shown[i],TicketCard(railContent,shown[i],i*248,6)));
            railContent.sizeDelta=new Vector2(Mathf.Max(10,shown.Count*248),186);
        }
        foreach(var (order,card) in tickets)UpdateTicket(order,card);
    }
    private RectTransform TicketCard(Transform parent,Order order,float x,float y)
    {
        var card=UiKit.Card(parent,"Ticket_"+Number(order),x,y,232,160,T.paper,12);
        var clip=UiKit.Box(card,"Clip",100,-12,32,22);UiKit.Fill(clip,T.steel,6);
        UiKit.Label(card,"Number","#"+Number(order),16,14,80,30,20,T.line,FontRole.Bold);
        UiKit.Badge(card,"Status","順番待ち",96,14,T.disabled,T.line,28,15);
        UiKit.Label(card,"Pizza",order.PizzaName,16,48,200,40,28,T.line,FontRole.Display);
        UiKit.Label(card,"Destination","→ "+ShopStrings.House(map,order.DestinationNodeId),16,88,200,30,19,T.inkSub,FontRole.Black);
        UiKit.Bar(card,"Deadline",16,134,200,14,T.basil);
        UiKit.Label(card,"Seconds","",16,112,200,24,T.captionSize,T.line,FontRole.Black);
        var source=card.gameObject.AddComponent<DragSource>();var hit=card.gameObject.AddComponent<Image>();hit.color=new Color(0,0,0,0);
        source.Payload=()=>order.Baking==BakingState.Waiting && cooking.ManualControlEnabled?new DragPayload{Kind=DragKind.Ticket,Order=order,Sprite="ticket",Size=new Vector2(150,110)}:null;
        source.Clicked=()=>ClickTicket(order);
        return card;
    }
    private void UpdateTicket(Order order,RectTransform card)
    {
        float left=order.Deadline-clock.ElapsedSeconds,fraction=Mathf.Clamp01(left/Mathf.Max(.01f,order.TimeLimit));
        bool urgent=fraction<=T.deadlineUrgent;
        card.Find("Seconds").GetComponent<TextMeshProUGUI>().text=OrderCountdown(order);
        card.Find("Deadline").GetComponent<UiBar>().Set(fraction,-1,T.DeadlineColor(fraction));
        string text;Color fill,ink=Color.white;
        var job=cooking.JobFor(order);
        if(left<=0){text="遅れ・減点で受付中";fill=T.berry;}
        else if(urgent){text="もうすぐ目安";fill=T.berry;}
        else if(order.IsReady){text=handover.IsBagged(order)?(handover.Holder(order)?.Name+" のバッグ"):"梱包済み";fill=T.basil;}
        else if(job==null){text="順番待ち";fill=T.disabled;ink=T.line;}
        else if(job.Stage==PizzaStage.Baked){text="梱包待ち";fill=T.basil;}
        else if(job.Stage==PizzaStage.Baking){text="焼いている";fill=T.cheese;ink=T.line;}
        else if(job.Worker==PizzaWorker.Player){text="あなたが調理中";fill=T.tomato;}
        else{text=(job.Staff!=null?job.Staff.Name:"内勤")+" 調理中";fill=T.info;}
        var badge=(RectTransform)card.Find("Status");UiKit.SetBadge(badge,text,fill,ink);badge.anchoredPosition=new Vector2(232-16-badge.sizeDelta.x,-14);
        card.Find("Edge").GetComponent<Image>().color=urgent?T.berry:T.line;
    }
    private void ClickTicket(Order order)
    {
        if(order.IsReady){ShowZone(ShopZone.Dispatch);return;}
        if(order.Baking!=BakingState.Waiting)return;
        for(int b=0;b<cooking.BenchCount;b++)if(cooking.ClaimRejection(order,b).Length==0){cooking.Claim(order,b);ShowZone(ShopZone.Kitchen);return;}
        Toast(cooking.ClaimRejection(order,0),T.berry);
    }

    // ---- zones ----
    public void ShowZone(ShopZone zone,bool immediate=false)
    {
        if(zone==ShopZone.Handover)zone=ShopZone.Dispatch;
        Zone=zone;for(int i=0;i<zoneTabs.Length;i++)if(zoneTabs[i]!=null)zoneTabs[i].Kind=i==(int)zone?StickerKind.Primary:StickerKind.Secondary;
        bool dispatching=zone==ShopZone.Dispatch;
        worldGroup.alpha=dispatching?0:1;worldGroup.blocksRaycasts=worldGroup.interactable=!dispatching;
        UiKit.Show(dispatch,dispatching);hudGroup.alpha=dispatching?0:1;hudGroup.blocksRaycasts=!dispatching;UiKit.Show(rail,!dispatching);
        // On the map the tabs move to the bottom left (厨房 / 指示盤) so the summary bar keeps the width.
        var tabs=(RectTransform)zoneTabs[0].transform.parent;tabs.anchoredPosition=dispatching?new Vector2(32,-968):new Vector2(590,-968);
        UiKit.Show(zoneTabs[1],false);zoneTabs[0].Label.fontSize=zoneTabs[2].Label.fontSize=T.subheadingSize;((RectTransform)zoneTabs[0].transform).sizeDelta=new Vector2(dispatching?136:236,82);
        ((RectTransform)zoneTabs[2].transform).anchoredPosition=new Vector2(dispatching?146:250,0);((RectTransform)zoneTabs[2].transform).sizeDelta=new Vector2(dispatching?136:236,82);
        float target=zone==ShopZone.Handover?HandoverPan:0;panFrom=world.anchoredPosition.x;panTo=target;panStarted=Time.unscaledTime;
        if(immediate){world.anchoredPosition=new Vector2(target,0);panFrom=target;}
        if(map!=null)map.PointerInputEnabled=!dispatching;
        SetMinimap(dispatching);
        if(dispatching)EnterDispatch();
    }
    private void Pan()
    {
        float t=Mathf.Clamp01((Time.unscaledTime-panStarted)/.35f);t=1-(1-t)*(1-t)*(1-t);
        world.anchoredPosition=new Vector2(Mathf.Lerp(panFrom,panTo,t),0);
    }
    private void Keys()
    {
        var k=Keyboard.current;if(k==null || menu.MenuOpen || menu.HelpOpen)return;
        if(k.digit1Key.wasPressedThisFrame)ShowZone(ShopZone.Kitchen);if(k.digit2Key.wasPressedThisFrame)ShowZone(ShopZone.Dispatch);if(k.digit3Key.wasPressedThisFrame)ShowZone(ShopZone.Dispatch);
        if(k.qKey.wasPressedThisFrame)ShowZone(ShopZone.Kitchen);if(k.eKey.wasPressedThisFrame)ShowZone(ShopZone.Dispatch);
    }

    // ---- notifications ----
    private readonly List<(RectTransform card,float until)> toastCards=new();
    public void Toast(string message,Color accent,Action onClick=null,string icon="icon_bell")
    {
        if(UIThemeDriver.RenderingSuspended || string.IsNullOrEmpty(message))return;
        var card=UiKit.Card(toasts,"Toast",20,0,400,74,T.paper,14,4,3,accent);
        UiKit.Icon(card,"Icon",icon,16,18,38,T.line);
        UiKit.Label(card,"Text",message,64,6,320,62,18,T.line,FontRole.Black,TextAlignmentOptions.MidlineLeft,true);
        if(onClick!=null){var b=card.gameObject.AddComponent<Button>();card.gameObject.AddComponent<StickerCardAction>().Apply();b.onClick.AddListener(()=>onClick());}
        toastCards.Insert(0,(card,Time.unscaledTime+5));
    }
    private void RefreshToasts()
    {
        for(int i=toastCards.Count-1;i>=0;i--)if(Time.unscaledTime>toastCards[i].until || i>=3){if(toastCards[i].card!=null)Destroy(toastCards[i].card.gameObject);toastCards.RemoveAt(i);}
        for(int i=0;i<toastCards.Count;i++)toastCards[i].card.anchoredPosition=Vector2.Lerp(toastCards[i].card.anchoredPosition,new Vector2(0,-i*90),.25f);
    }
    private void Returned(DeliveryTrip trip){Toast($"{trip.Person.Name} が帰還  配達指示へ →",T.basil,()=>ShowZone(ShopZone.Handover));}
    private void FlowMessage(string message){if(message.Contains("cancelled"))Toast("注文キャンセル："+(clock.Phase==DayPhase.Open?"受付猶予を過ぎました。":"閉店しました。"),T.berry);if(message.StartsWith("ACCIDENT"))Toast("事故: "+UiStrings.Japanese(message).Replace("ACCIDENT: ",""),T.berry,null,"icon_flame");}

    private void Update()
    {
        if(UIThemeDriver.RenderingSuspended)return;
        if(router.Current!=ScreenId.Business)return;
        Keys();Pan();RefreshHud();RefreshRail();RefreshKitchen();RefreshHandover();RefreshDispatch();RefreshToasts();
    }
}
