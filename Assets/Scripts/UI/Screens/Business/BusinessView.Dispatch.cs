using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>指示盤 (UI_Mock/Batch.png, ShopDispatch.png): the map fills the screen, one lane per courier at the bottom.
/// ① drop tickets into a courier's lane (several = one trip) ② choose a leg and click its roads, then 出発.
/// The automatic route is plain on purpose (shortest distance, blind to traffic and local knowledge).</summary>
public sealed partial class BusinessView
{
    [Header("Dispatch")]
    [SerializeField] private RectTransform stepsBar,bubble,minimapFrame,laneRow;
    [SerializeField] private StickerButton zoomIn,zoomOut,fitButton;
    [SerializeField] private RectTransform[] steps=new RectTransform[2];
    [SerializeField] private Image[] stepCircles=new Image[2];
    [SerializeField] private GameObject[] stepNumbers=new GameObject[2],stepChecks=new GameObject[2];
    /// <summary>The four sides of the minimap's view frame: top, bottom, left, right.</summary>
    [SerializeField] private RectTransform[] minimapEdges=new RectTransform[4];
    [SerializeField] private TextMeshProUGUI bubbleText;
    [SerializeField] private StickerButton trafficButton;
    [SerializeField] private RawImage minimapImage;
    private Camera minimapCamera;private RenderTexture minimapTexture;
    private sealed class Lane
    {
        public DeliveryPerson Person;public RectTransform Root,StateBadge,Diff;public TextMeshProUGUI Time,Note;
        public StickerButton Depart,Reset,Swap,Edit;public UiBar Stamina,ReturnProgress;public DropZone Zone;public readonly List<DropZone> Slots=new();
    }
    private readonly List<Lane> lanes=new();private string laneSignature="";
    private readonly Dictionary<DeliveryPerson,Dictionary<(string,string),string>> waypoints=new();
    private bool pressing,dragging;private Vector2 pressPosition,lastPointer;
    private Order pinPress;private bool pinDragging;private int routeLegPress=-1;private bool routeDragging;
    private Transform pinRoot;
    private sealed class Pin {public Transform Root;public SpriteRenderer Sprite,Glow,Pill,PillEdge,NumberBack;public TextMeshPro Number,Label;}
    private readonly Dictionary<string,Pin> pins=new();

    private readonly List<SpriteRenderer> autoDots=new();
    private Order nearFor;private readonly Dictionary<string,float> nearSeconds=new();
    private static bool guided;
    public static bool ShowCongestion {get;private set;}=true;
    [SerializeField, Min(1)] private float nearPinSeconds=8;
    private const float LaneTop=764,LaneHeight=296;
    private bool focusLaneRequested;
    private DeliveryPerson editingPerson;
    private int editingLeg=-1;

    private void BuildDispatch()
    {
        // steps bar (to the right of the 厨房/配達指示 toggle)
        stepsBar=UiKit.Box(dispatch,"Steps",32,122,760,76);
        string[] names={ShopStrings.Step1,ShopStrings.Step2};float[] widths={360,320};float x=0;
        if(steps==null || steps.Length!=2)steps=new RectTransform[2]; // older scenes saved four steps
        for(int i=0;i<2;i++)
        {
            var pill=UiKit.Card(stepsBar,"Step"+i,x,4,widths[i],68,T.paper,34,4,3);steps[i]=pill;
            var circle=UiKit.Box(pill,"Circle",10,10,48,48);var c=circle.gameObject.AddComponent<Image>();c.sprite=UiKit.Circle;c.raycastTarget=false;stepCircles[i]=c;
            stepNumbers[i]=UiKit.Label(circle,"Number",(i+1).ToString(),0,0,48,48,24,Color.white,FontRole.Display,TextAlignmentOptions.Center).gameObject;
            stepChecks[i]=UiKit.Icon(circle,"Check","icon_check",9,9,30,Color.white).gameObject;
            UiKit.Label(pill,"Text",names[i],68,0,widths[i]-78,68,T.bodySize,T.line,FontRole.Display);x+=widths[i];
            if(i==0){UiKit.Icon(stepsBar,"Arrow","icon_play",x+10,26,24,T.inkSub);x+=44;}
        }
        BuildDispatchOrders();
        // guide bubble
        bubble=UiKit.Card(dispatch,"Bubble",1140,126,620,112,T.paper,18,6,3);
        UiKit.Badge(bubble,"Here","いまここ",20,14,T.cheese,T.line,30,16);
        bubbleText=UiKit.Label(bubble,"Text","",20,50,560,56,T.captionSize,T.line,FontRole.Black,TextAlignmentOptions.TopLeft,true);
        // map controls
        var zoom=UiKit.Card(dispatch,"Zoom",1808,250,76,150,T.paper,14,6,3);
        zoomIn=UiKit.Button(zoom,"ZoomIn","",StickerKind.Secondary,4,4,68,68,20,null,FontRole.Black,"icon_plus");
        zoomOut=UiKit.Button(zoom,"ZoomOut","",StickerKind.Secondary,4,78,68,68,20,null,FontRole.Black,"icon_minus");
        fitButton=UiKit.Button(dispatch,"Fit","",StickerKind.Secondary,1808,418,76,76,20,null,FontRole.Black,"icon_target");
        trafficButton=UiKit.Button(dispatch,"Traffic","混雑",StickerKind.Selected,1808,510,76,76,16,null,FontRole.Black);
        // minimap (above the lanes, bottom right)
        var mini=UiKit.Card(dispatch,"Minimap",1640,584,248,168,T.paper,14,6,3);
        minimapImage=UiKit.Stretch(mini,"Image",6,6,6,6).gameObject.AddComponent<RawImage>();minimapImage.raycastTarget=false;
        minimapFrame=UiKit.Box(minimapImage.transform,"Frame",0,0,40,30);
        string[] sides={"T","B","L","R"};
        for(int i=0;i<4;i++){minimapEdges[i]=UiKit.Box(minimapFrame,sides[i],0,0,1,1);UiKit.Fill(minimapEdges[i],T.tomato,0);}
        // one lane per courier
        var laneViewport=UiKit.Box(dispatch,"LaneViewport",32,LaneTop,1856,LaneHeight);
        laneViewport.gameObject.AddComponent<RectMask2D>().padding=new Vector4(-8,-12,-8,-12);
        var laneHit=laneViewport.gameObject.AddComponent<Image>();laneHit.color=Color.clear;
        laneRow=UiKit.Box(laneViewport,"Lanes",0,0,1856,LaneHeight);
        var laneScroll=laneViewport.gameObject.AddComponent<ScrollRect>();
        laneScroll.viewport=laneViewport;
        laneScroll.content=laneRow;
        laneScroll.horizontal=true;
        laneScroll.vertical=false;
        laneScroll.movementType=ScrollRect.MovementType.Clamped;
        ScrollNavigation.Configure(laneScroll,true);
        string legend=$"<color=#{ColorUtility.ToHtmlStringRGB(T.tomato)}>行き・配達先へ ━</color>  /  <color=#{ColorUtility.ToHtmlStringRGB(T.info)}>帰り・お店へ ━</color>  動く丸が進行方向 ／ レーンは横スクロール";
        UiKit.Label(dispatch,"RouteLegend",legend,430,724,1120,32,T.captionSize,T.line,FontRole.Black);
        UiKit.Show(dispatch,false);
    }
    private void BindDispatch()
    {
        zoomIn.Button.onClick.AddListener(()=>Zoom(.8f,Vector2.zero,false));
        zoomOut.Button.onClick.AddListener(()=>Zoom(1.25f,Vector2.zero,false));
        fitButton.Button.onClick.AddListener(FitMap);
        trafficButton.Button.onClick.AddListener(()=>{ShowCongestion=!ShowCongestion;});
        ScrollNavigation.Configure(laneRow.GetComponentInParent<ScrollRect>(),true);
        BindDispatchOrders();
    }
    private TextMeshProUGUI Chip(Transform parent,float x,float w,string icon)
    {
        var card=UiKit.Card(parent,"Chip",x,0,w,62,T.paper,14,4,3);UiKit.Icon(card,"Icon",icon,12,15,32,T.line);
        return UiKit.Label(card,"Text","",48,0,w-58,62,26,T.line,FontRole.Display);
    }
    private void EnterDispatch()
    {
        if(roster.SelectedPerson==null || !roster.SelectedPerson.IsAvailable)
        {var withBag=roster.People.FirstOrDefault(p=>p.IsAvailable && handover.BagOf(p).Count>0);if(withBag!=null)handover.Select(withBag);}
        FitMap();
    }
    /// <summary>Fit the whole map into the area left free by the order list, the controls and the lanes.</summary>
    public void FitMap()
    {
        var cam=Camera.main;if(cam==null || map==null || !map.IsReady)return;
        float minX=map.Nodes.Min(n=>n.Position.x)-.9f,maxX=map.Nodes.Max(n=>n.Position.x)+.9f,minY=map.Nodes.Min(n=>n.Position.y)-.9f,maxY=map.Nodes.Max(n=>n.Position.y)+.9f;
        float fl=400f/1920,fr=1610f/1920,fb=(1080-LaneTop+20)/1080f,ft=(1080-250f)/1080,aspect=cam.aspect;
        float size=Mathf.Max((maxY-minY)/(2*(ft-fb)),(maxX-minX)/(2*aspect*(fr-fl)));cam.orthographicSize=size;
        var centre=new Vector2((minX+maxX)/2,(minY+maxY)/2);var offset=new Vector2(((fl+fr)/2-.5f)*2*size*aspect,((fb+ft)/2-.5f)*2*size);
        cam.transform.position=new Vector3(centre.x-offset.x,centre.y-offset.y,cam.transform.position.z);
    }

    // ---- routes: the lane's visit order, plus selected waypoint roads ----
    private Dictionary<(string,string),string> Waypoints(DeliveryPerson p)
    {
        if(!waypoints.TryGetValue(p,out var w))waypoints[p]=w=new Dictionary<(string,string),string>();
        return w;
    }
    private List<string> Stops(IReadOnlyList<Order> bag)
    {
        var stops=new List<string>{map.StoreNodeId};
        foreach(var o in bag)if(stops[stops.Count-1]!=o.DestinationNodeId)stops.Add(o.DestinationNodeId);
        stops.Add(map.StoreNodeId);return stops;
    }
    private int distanceCacheRevision=-1;
    private readonly Dictionary<(string,string),List<string>> distancePaths=new();
    // These distance-only paths are immutable during one map revision. Clear on seed/day/size changes.
    private List<string> Path(string a,string b)
    {
        if(distanceCacheRevision!=map.MapRevision){distanceCacheRevision=map.MapRevision;distancePaths.Clear();}
        if(!distancePaths.TryGetValue((a,b),out var path))
        {
            path=RoadPathfinder.FindShortestPath(map.Edges,a,b,e=>e.Distance);
            distancePaths[(a,b)]=path;
        }
        return path;
    }
    /// <summary>Legs from one stop to the next. The automatic route takes the shortest distance; the player's route
    /// passes the waypoint dragged onto that leg.</summary>
    private List<(string from,string to,List<string> edges)> Legs(DeliveryPerson p,IReadOnlyList<Order> bag,bool automatic)
    {
        var legs=new List<(string,string,List<string>)>();if(map==null || !map.IsReady || bag.Count==0)return legs;
        var stops=Stops(bag);var w=automatic?null:Waypoints(p);
        for(int i=0;i+1<stops.Count;i++)
        {
            string a=stops[i],b=stops[i+1];List<string> edges=null;
            if(w!=null && w.TryGetValue((a,b),out var via))edges=Via(a,b,via);
            edges??=Path(a,b);if(edges==null)return new List<(string,string,List<string>)>();
            legs.Add((a,b,edges));
        }
        return legs;
    }
    /// <summary>A leg forced through a waypoint: an intersection id, or "E:" + a road id (the road is driven in the shorter direction).</summary>
    private List<string> Via(string a,string b,string via)
    {
        if(!via.StartsWith("E:")){var first=Path(a,via);var second=Path(via,b);return first!=null && second!=null?first.Concat(second).ToList():null;}
        var road=map.GetEdge(via.Substring(2));List<string> best=null;float bestLength=float.PositiveInfinity;
        foreach(var (start,end) in new[]{(road.NodeAId,road.NodeBId),(road.NodeBId,road.NodeAId)})
        {
            if(!road.TryTraverse(start,out var to) || to!=end)continue;
            var first=Path(a,start);var second=Path(end,b);if(first==null || second==null)continue;
            var edges=first.Append(road.Id).Concat(second).ToList();float length=edges.Sum(id=>map.GetEdge(id).Distance);
            if(length<bestLength){bestLength=length;best=edges;}
        }
        return best;
    }
    private List<string> RouteEdges(DeliveryPerson p,IReadOnlyList<Order> bag,bool automatic)=>Legs(p,bag,automatic).SelectMany(l=>l.edges).ToList();
    private float Seconds(IEnumerable<string> edges,DeliveryPerson p)=>clock.ToRealSeconds(edges.Sum(id=>map.GetTravelTime(id,p)));
    /// <summary>Keep the planner's route equal to the selected lane (order + waypoints).</summary>
    public void SyncRoute()
    {
        var person=roster.SelectedPerson;if(person==null || !person.IsAvailable || planner.Route==null || clock.Phase!=DayPhase.Open)return;
        var bag=handover.BagOf(person);if(bag.Count==0)return;
        if(!orders.SelectedOrders.SequenceEqual(bag))return;
        var edges=RouteEdges(person,bag,false);
        if(edges.Count>0 && !planner.Route.EdgeIds.SequenceEqual(edges))planner.ReplaceRoute(edges);
    }
    /// <summary>Route a leg through a node (the handle dropped there). legIndex counts from the store.</summary>
    public bool SetWaypoint(DeliveryPerson person,int legIndex,string nodeId)
    {
        var legs=Legs(person,handover.BagOf(person),false);if(legIndex<0 || legIndex>=legs.Count)return false;
        var (a,b,_)=legs[legIndex];
        if(nodeId==a || nodeId==b){Waypoints(person).Remove((a,b));SyncRoute();return true;}
        if(Via(a,b,nodeId)==null){Toast("そこを通る道がありません。",T.berry,null,"icon_map");return false;}
        Waypoints(person)[(a,b)]=nodeId;SyncRoute();return true;
    }
    /// <summary>Route a leg along a road (the red line dragged onto it).</summary>
    public bool SetWaypointRoad(DeliveryPerson person,int legIndex,string edgeId)=>SetWaypoint(person,legIndex,"E:"+edgeId);
    public void ResetRoute(DeliveryPerson person){if(person==null)return;Waypoints(person).Clear();SyncRoute();}
    public bool SwapDeliveryOrder(DeliveryPerson person)
    {
        var bag=handover.BagOf(person);
        if(!person.IsAvailable || bag.Count<2)return false;
        bool moved=handover.Move(bag[1],person,0);
        if(moved){handover.Select(person);editingPerson=null;editingLeg=-1;SyncRoute();}
        return moved;
    }
    private string RouteEditCaption()
    {
        var legs=Legs(editingPerson,handover.BagOf(editingPerson),false);
        if(editingLeg<0 || editingLeg>=legs.Count)return "道を選択";
        var leg=legs[editingLeg];
        string Name(string id)=>id==map.StoreNodeId?ShopStrings.Store:ShopStrings.House(map,id);
        return Name(leg.from)+" → "+Name(leg.to);
    }
    public bool BeginRouteEdit(DeliveryPerson person)
    {
        if(person==null || !person.IsAvailable || handover.BagOf(person).Count==0)return false;
        handover.Select(person);
        int count=Legs(person,handover.BagOf(person),false).Count;
        if(count==0)return false;
        editingLeg=editingPerson==person?(editingLeg+1)%count:0;
        editingPerson=person;
        return true;
    }
    public void Depart(DeliveryPerson person)
    {
        if(person==null)return;
        if(!handover.Select(person)){Toast($"{person.Name} は店にいません。",T.cheese,null,"icon_bike");return;}
        SyncRoute();
        string reason=flow.DispatchRejectionReason(planner.SelectedPlan,person);
        if(reason.Length>0){Toast(handover.BagOf(person).Count==0?"レーンに伝票を入れましょう。":UiStrings.Japanese(reason),T.berry,null,"icon_bike");return;}
        editingPerson=null;editingLeg=-1;planner.Dispatch();Toast($"{person.Name} 出発!",T.basil,null,"icon_bike");guided=true;waypoints.Remove(person);
        var next=roster.People.FirstOrDefault(p=>p.IsAvailable && handover.BagOf(p).Count>0);if(next!=null)handover.Select(next);
    }
    private int Late(List<string> edges,DeliveryPerson person,IReadOnlyList<Order> bag)
    {
        float t=clock.ElapsedSeconds;var arrival=new Dictionary<string,float>();string at=map.StoreNodeId;
        foreach(var id in edges)
        {
            t+=clock.ToRealSeconds(map.GetTravelTime(id,person));
            map.GetEdge(id).TryTraverse(at,out at);
            if(!arrival.ContainsKey(at))arrival[at]=t;
        }
        return bag.Count(o=>!arrival.TryGetValue(o.DestinationNodeId,out var when) || when>o.Deadline);
    }

    // ---- lanes ----
    private DragPayload Held=>ShopHand.Instance!=null?ShopHand.Instance.Held:null;
    private static bool Carries(DragPayload p)=>p!=null && p.Order!=null && (p.Kind==DragKind.Box || p.Kind==DragKind.Bagged);
    private string LaneRejection(DragPayload p,DeliveryPerson person)
    {
        if(!Carries(p))return "伝票だけ入れられます。";
        if(p.Kind==DragKind.Box)return handover.BagRejection(p.Order,person);
        if(handover.Holder(p.Order)==person)return "";
        if(!person.IsAvailable)return person.Name+" は店にいません。";
        if(handover.BagOf(person).Count>=person.Capacity)return $"いっぱい（{person.Capacity}箱まで）";
        return "";
    }
    public bool LaneDrop(DragPayload p,DeliveryPerson person,int index=-1)
    {
        if(LaneRejection(p,person).Length>0){Toast(LaneRejection(p,person),T.berry,null,"icon_bike");return false;}
        bool ok=p.Kind==DragKind.Box?handover.Bag(p.Order,person) && (index<0 || handover.Move(p.Order,person,index)):handover.Move(p.Order,person,index);
        if(!ok)Toast(handover.LastMessage,T.berry,null,"icon_bike");
        return ok;
    }
    private void RebuildLanes()
    {
        foreach(var lane in lanes)
        {
            ShopHand.Instance?.Unregister(lane.Zone);
            foreach(var z in lane.Slots)ShopHand.Instance?.Unregister(z);
            Destroy(lane.Root.gameObject);
        }
        lanes.Clear();
        var people=roster.People.ToList();int n=people.Count;if(n==0)return;
        float gap=20,w=Mathf.Max(T.deliveryLaneMinWidth,(1856-gap*(n-1))/n);
        laneRow.sizeDelta=new Vector2(Mathf.Max(1856,n*(w+gap)-gap),LaneHeight);
        int idle=people.Count(p=>p.IsAvailable);
        for(int k=0;k<n;k++)
        {
            var p=people[k];bool here=p.IsAvailable,selected=here && roster.SelectedPerson==p,only=here && idle==1;
            var lane=new Lane{Person=p};lanes.Add(lane);
            lane.Root=UiKit.Card(laneRow,"Lane_"+p.Name,k*(w+gap),0,w,LaneHeight,here?T.paper:T.paperDeep,18,6,selected || only?4:3,selected || only?T.tomato:(Color?)null);
            var glow=UiKit.Stretch(lane.Root,"Glow",-8,-8,-8,-8);var g=UiKit.Fill(glow,T.cheese,24);g.enabled=false;glow.SetAsFirstSibling();
            var hit=lane.Root.gameObject.AddComponent<Image>();hit.color=Color.clear;
            lane.Zone=lane.Root.gameObject.AddComponent<DropZone>();lane.Zone.Glow=g;
            lane.Zone.Accepts=pl=>LaneRejection(pl,p).Length==0;lane.Zone.Dropped=pl=>LaneDrop(pl,p);ShopHand.Instance?.Register(lane.Zone);
            var click=lane.Root.gameObject.AddComponent<DragSource>();
            click.Clicked=()=>
            {
                if(p.IsAvailable)handover.Select(p);
                else Toast($"{p.Name} は{ShopStrings.Status(p.Status)}です。",T.cheese,null,"icon_bike");
            };
            // header: avatar, name, state; depart on the right
            UiKit.Avatar(lane.Root,"Avatar",16,12,56,here?T.CourierColor(p.Name):T.disabled,p.Name.Substring(0,1));
            var name=UiKit.Label(lane.Root,"Name",p.Name,82,10,Mathf.Min(170,w-280),36,28,T.line,FontRole.Display);
            lane.StateBadge=UiKit.Badge(lane.Root,"State",here?"待機中":ShopStrings.Status(p.Status),82,46,here?T.basil:T.disabled,here?Color.white:T.line,24,16);
            lane.Depart=UiKit.Button(lane.Root,"Depart_"+p.Name,ShopStrings.Depart+" ▶",StickerKind.Primary,w-166,12,150,58,28,()=>Depart(p),FontRole.Display);
            UiKit.Show(lane.Depart,here);
            // quick status: stamina, driving, local knowledge, load
            float sw=(w-32)/3f;
            lane.Stamina=StatBar(lane.Root,"Stamina","体力",16,80,sw-10,p.CurrentStamina,T.basil);
            StatBar(lane.Root,"Driving","運転",16+sw,80,sw-10,p.Driving,T.info);
            StatBar(lane.Root,"Knowledge","土地勘",16+sw*2,80,sw-10,p.LocalKnowledge,T.info);
            // bag slots (load = number of slots)
            var bag=handover.BagOf(p);int cap=p.Capacity;float slotW=Mathf.Min(176,(w-32-(cap-1)*10f)/cap);
            // While away, the return gauge takes the place of the bag slots.
            for(int s=0;s<cap && here;s++)
            {
                var slot=UiKit.Box(lane.Root,"Slot"+s,16+s*(slotW+10),108,slotW,60);int index=s;
                var zone=slot.gameObject.AddComponent<DropZone>();
                zone.Accepts=pl=>LaneRejection(pl,p).Length==0;
                zone.Dropped=pl=>LaneDrop(pl,p,index);
                lane.Slots.Add(zone);
                ShopHand.Instance?.Register(zone);
                if(s<bag.Count)
                {
                    var o=bag[s];UiKit.Card(slot,"Ticket",0,0,slotW,60,T.paper,10,3,2);
                    UiKit.Label(slot,"Title",$"{s+1}  #{Number(o)} {ShortHouse(o)}",10,4,slotW-16,30,18,T.line,FontRole.Black);
                    UiKit.Label(slot,"Pizza",o.PizzaName,10,32,slotW-16,24,16,T.inkSub,FontRole.Bold);
                    var source=slot.gameObject.AddComponent<DragSource>();var img=slot.gameObject.AddComponent<Image>();img.color=Color.clear;
                    source.Payload=()=>new DragPayload{Kind=DragKind.Bagged,Order=o,Sprite="box_closed",Size=new Vector2(96,96)};
                    source.Clicked=()=>handover.Unbag(o);slot.name="Bag_"+Number(o);
                }
                else
                {
                    UiKit.Dashed(slot,T.inkSub,10,6,2);var img=slot.gameObject.AddComponent<Image>();img.color=Color.clear;
                    UiKit.Icon(slot,"Plus","icon_plus",slotW/2-12,18,24,T.inkSub);
                    var add=slot.gameObject.AddComponent<DragSource>();
                    add.Clicked=()=>
                    {
                        var next=handover.RackOrders.FirstOrDefault();
                        if(next==null)Toast("焼き上がった注文がありません。",T.cheese,null,"icon_box");
                        else LaneDrop(new DragPayload{Kind=DragKind.Box,Order=next},p);
                    };
                }
            }
            // footer: trip time, difference from the automatic route, fit with the promise
            lane.Time=UiKit.Label(lane.Root,"Time","",16,168,110,32,30,T.line,FontRole.Display);
            lane.Diff=UiKit.Badge(lane.Root,"Diff","",128,174,T.basil,Color.white,26,16);
            lane.Note=UiKit.Label(lane.Root,"Note","",16,176,w-32,40,16,T.inkSub,FontRole.Black);
            lane.ReturnProgress=UiKit.Bar(lane.Root,"ReturnProgress",16,120,w-32,32,T.info);
            UiKit.Show(lane.ReturnProgress,!here);
            lane.Swap=UiKit.Button(lane.Root,"Swap_"+p.Name,ShopStrings.SwapDeliveryOrder,StickerKind.Secondary,16,232,148,40,16,()=>SwapDeliveryOrder(p));
            lane.Swap.Interactable=here && bag.Count>1;
            lane.Edit=UiKit.Button(lane.Root,"Edit_"+p.Name,ShopStrings.ChooseRoad,StickerKind.Secondary,176,232,210,40,16,()=>BeginRouteEdit(p));
            lane.Edit.Interactable=here && bag.Count>0;
            lane.Reset=UiKit.Button(lane.Root,"Reset_"+p.Name,ShopStrings.ResetRoute,StickerKind.Secondary,w-146,232,130,40,16,()=>ResetRoute(p));
        }
    }
    private void RequestLaneFocus()=>focusLaneRequested=true;
    private void FocusSelectedLane()
    {
        if(!focusLaneRequested)return;
        var lane=lanes.FirstOrDefault(l=>l.Person==roster.SelectedPerson);
        if(lane==null)return;
        var scroll=laneRow.GetComponentInParent<ScrollRect>();
        Canvas.ForceUpdateCanvases();
        float hidden=laneRow.rect.width-scroll.viewport.rect.width;
        if(hidden>0)
        {
            float left=-laneRow.anchoredPosition.x,x=lane.Root.anchoredPosition.x;
            float target=x<left?x:x+lane.Root.rect.width>left+scroll.viewport.rect.width?x+lane.Root.rect.width-scroll.viewport.rect.width:left;
            scroll.StopMovement();scroll.horizontalNormalizedPosition=Mathf.Clamp01(target/hidden);
        }
        focusLaneRequested=false;
    }
    private string ShortHouse(Order o)=>ShopStrings.House(map,o.DestinationNodeId).Replace("さん宅","");
    private UiBar StatBar(Transform parent,string name,string title,float x,float y,float w,float value,Color color)
    {
        UiKit.Label(parent,name+"Title",title,x,y-4,58,24,16,T.inkSub,FontRole.Black);
        var bar=UiKit.Bar(parent,name,x+60,y+2,Mathf.Max(30,w-60),12,color);bar.Set(Mathf.Clamp01(value));return bar;
    }
    private void RefreshLanes()
    {
        var held=Held;bool holding=Carries(held);
        foreach(var lane in lanes)
        {
            var p=lane.Person;bool here=p.IsAvailable;var bag=handover.BagOf(p);
            lane.Stamina.Set(Mathf.Clamp01(p.CurrentStamina));
            var trip=p.CurrentTrip;
            lane.ReturnProgress.Set(trip==null?1:Mathf.InverseLerp(trip.StartedAt,trip.ReturnsAt,clock.ElapsedSeconds));
            UiKit.Show(lane.ReturnProgress,!here && trip!=null);
            bool editing=editingPerson==p && editingLeg>=0;
            lane.Edit.Text=editing?ShopStrings.RoadSection(editingLeg+1):ShopStrings.ChooseRoad;
            lane.Edit.Kind=editing?StickerKind.Selected:StickerKind.Secondary;
            string state=here?(bag.Count>0?$"{bag.Count} / {p.Capacity}箱":"待機中"):p.Status==DeliveryStatus.Training?"訓練中":$"{ShopStrings.Status(p.Status)}・あと{Mathf.Max(0,(p.CurrentTrip?.ReturnsAt??0)-clock.ElapsedSeconds):F0}秒";
            UiKit.SetBadge(lane.StateBadge,state,here?(bag.Count>=p.Capacity?T.cheese:T.basil):T.disabled,here && bag.Count<p.Capacity?Color.white:T.line);
            var note=lane.Note;note.color=T.inkSub;string noteText="";bool showTime=false,showDiff=false,showReset=false;
            if(!here)noteText="帰ってきたら、ここに入れられます";
            else if(holding)
            {
                string reason=LaneRejection(held,p);
                if(reason.Length>0){noteText="入れられません："+reason;note.color=T.berry;}
                else
                {
                    var with=bag.Where(o=>o!=held.Order).Append(held.Order).ToList();var without=bag.Where(o=>o!=held.Order).ToList();
                    float together=Seconds(RouteEdges(p,with,true),p);
                    if(without.Count==0)noteText=$"この1件で 往復 約{together:F0}秒";
                    else
                    {
                        float apart=Seconds(RouteEdges(p,without,true),p)+Seconds(RouteEdges(p,new[]{held.Order},true),p);
                        noteText=$"まとめると 約{together:F0}秒（別々だと 約{apart:F0}秒）";note.color=together<apart-.5f?T.basil:T.inkSub;
                    }
                }
            }
            else if(bag.Count==0)noteText="伝票をここへドラッグ";
            else
            {
                var mine=RouteEdges(p,bag,false);var auto=RouteEdges(p,bag,true);float time=Seconds(mine,p),autoTime=Seconds(auto,p);
                lane.Time.text=$"{time:F1}<size=18>秒</size>";showTime=true;
                bool changed=!mine.SequenceEqual(auto);float saved=autoTime-time;
                UiKit.SetBadge(lane.Diff,!changed?"自動ルート":Mathf.Abs(saved)<.05f?"自動と同じ":saved>0?$"自動より −{saved:F1}秒":$"自動より +{-saved:F1}秒",!changed?T.paperDeep:saved>=0?T.basil:T.cheese,!changed || saved<0?T.line:Color.white);
                showDiff=true;showReset=changed;
                lane.Diff.anchoredPosition=new Vector2(16+lane.Time.GetPreferredValues().x+10,-174);
                int late=Late(mine,p,bag);noteText=late==0?"往復・目安に間に合う":$"{late}件 目安に遅れそう";note.color=late==0?T.basil:T.berry;
                note.rectTransform.anchoredPosition=new Vector2(16,-200);
                note.rectTransform.sizeDelta=new Vector2(lane.Root.sizeDelta.x-32,24);
            }
            if(!showTime)
            {
                note.rectTransform.anchoredPosition=new Vector2(16,-176);
                note.rectTransform.sizeDelta=new Vector2(lane.Root.sizeDelta.x-32,40);
            }
            note.text=noteText;lane.Time.gameObject.SetActive(showTime);lane.Diff.gameObject.SetActive(showDiff);UiKit.Show(lane.Reset,showReset);
            if(lane.Depart!=null)lane.Depart.Interactable=here && bag.Count>0 && clock.Phase==DayPhase.Open;
        }
    }

    private int Step()=>roster.People.Any(p=>p.IsAvailable && handover.BagOf(p).Count>0)?1:0;
    private void RefreshDispatch()
    {
        if(editingPerson!=null && (!editingPerson.IsAvailable || roster.SelectedPerson!=editingPerson || handover.BagOf(editingPerson).Count==0)){editingPerson=null;editingLeg=-1;}
        UpdatePins();
        if(Zone!=ShopZone.Dispatch)return;
        RefreshDispatchOrders();
        string sig=string.Join(",",roster.People.Select(p=>UiKit.Ref(p)+p.Status+p.Capacity+string.Join("/",handover.BagOf(p).Select(o=>o.Id))))+"|"+UiKit.Ref(roster.SelectedPerson);
        if(sig!=laneSignature){laneSignature=sig;RebuildLanes();}
        FocusSelectedLane();SyncRoute();RefreshLanes();
        Navigate();UpdateMinimap();UpdateRouteMarks();
        int step=Step();
        for(int i=0;i<2;i++)
        {
            bool done=i<step,current=i==step;UiKit.FaceOf(steps[i]).color=current?T.cheese:T.paper;
            stepCircles[i].color=done?T.basil:current?T.tomato:T.disabled;
            stepNumbers[i].SetActive(!done);stepChecks[i].SetActive(done);
        }
        bool showGuide=editingPerson!=null || !guided || Carries(Held);UiKit.Show(bubble,showGuide);
        bubbleText.text=editingPerson!=null?RouteEditCaption()+ShopStrings.RoadEditHint:Carries(Held)?"伝票を持つと、近い注文のピンが光ります。配達員のレーンに入れると、まとめて運びます。"
            :step==0?(handover.RackOrders.Any()?"焼き上がった伝票を、下の配達員のレーンへドラッグします。":"焼き上がった注文が、左の一覧とピンに出ます。")
            :ShopStrings.RouteControlsHint;
        trafficButton.Kind=ShowCongestion?StickerKind.Selected:StickerKind.Secondary;
    }

    // ---- pins on the map (world space, constant screen size) ----
    private string pinViewSignature;
    private float Px=>Camera.main.orthographicSize*2f/Mathf.Max(1,Camera.main.pixelHeight);
    private void UpdatePins()
    {
        if(map==null || !map.IsReady)return;var cam=Camera.main;if(cam==null)return;
        if(pinRoot==null){pinRoot=new GameObject("ShopMapPins").transform;pinRoot.SetParent(map.transform,false);}
        bool show=router.Current==ScreenId.Business && Zone==ShopZone.Dispatch;
        pinRoot.gameObject.SetActive(show);
        if(!show)
        {
            pinViewSignature=null;
            return;
        }
        float px=Px;
        foreach(var id in pins.Keys.Where(id=>!map.HasNode(id)).ToArray()){Destroy(pins[id].Root.gameObject);pins.Remove(id);}
        var person=roster.SelectedPerson;var bag=person!=null && person.IsAvailable?handover.BagOf(person):System.Array.Empty<Order>();
        var held=Held;UpdateNear(held);
        var waiting=orders.Orders.Where(o=>o.Status==OrderStatus.Pending)
            .Concat(flow.ActiveTrips.SelectMany(t=>t.Deliveries).Where(d=>!d.Resolved).Select(d=>d.Order))
            .Distinct().OrderBy(o=>o.Deadline).ToList();
        string signature=map.MapRevision+"/"+cam.transform.position.ToString("R")+"/"+cam.orthographicSize.ToString("R")+"/"+cam.pixelHeight+"/"+person?.Id+"/"+held?.Order?.Id+"/"+string.Join(",",bag.Select(o=>o.Id))+"/"+string.Join(",",waiting.Select(o=>o.Id+":"+OrderStage(o)+":"+(cooking.JobFor(o)?.QueuedForOven??false)))+"/"+T.line.GetHashCode()+T.paper.GetHashCode()+T.basil.GetHashCode();
        if(signature==pinViewSignature)return;pinViewSignature=signature;
        foreach(var node in map.Nodes)
        {
            if(!node.IsStore && node.NodeType!=NodeType.DeliveryAddress)continue;
            var activeOrder=waiting.FirstOrDefault(o=>o.DestinationNodeId==node.Id);
            if(!node.IsStore && activeOrder==null)
            {
                if(pins.TryGetValue(node.Id,out var inactive))inactive.Root.gameObject.SetActive(false);
                continue;
            }
            if(!pins.TryGetValue(node.Id,out var pin))pin=pins[node.Id]=MakePin(node);
            pin.Root.gameObject.SetActive(true);
            pin.Root.position=new Vector3(node.Position.x,node.Position.y,-1);pin.Root.localScale=Vector3.one*px;
            if(node.IsStore)
            {
                pin.Sprite.sprite=UiKit.Art("shop_badge");
                pin.Sprite.transform.localPosition=Vector3.zero;
                pin.Sprite.transform.localScale=Vector3.one*40;
                pin.Number.text="";
                SetPinLabel(pin,ShopStrings.Store,T.paper,T.line);
                pin.Glow.enabled=false;
                continue;
            }
            int visit=bag.Select(o=>o.DestinationNodeId).Distinct().ToList().IndexOf(node.Id);
            var order=waiting.FirstOrDefault(o=>o.DestinationNodeId==node.Id);
            bool red=visit>=0 || (order!=null && order.IsReady);
            bool compact=map.City!=null && map.CitySettings.blockSize/px<T.cityDetailMinimumBlockPixels;
            pin.Sprite.sprite=UiKit.Art("pin_white");
            pin.Sprite.transform.localScale=Vector3.one*(compact?Mathf.Max(T.cityCompactPinPixels,T.orderPinNumberSize*.2f):red?50:42);
            pin.Sprite.transform.localPosition=new Vector3(0,compact?14:red?36:30,0);
            pin.Number.text=order!=null?"#"+Number(order):"";
            pin.Number.fontSize=T.orderPinNumberSize;
            pin.Number.rectTransform.sizeDelta=new Vector2(110,42);
            pin.Number.fontStyle=FontStyles.Normal;
            pin.Number.outlineWidth=0;
            pin.Number.color=T.line;
            pin.Number.transform.localPosition=new Vector3(0,compact?18:red?44:36,0);
            pin.NumberBack.enabled=true;
            pin.NumberBack.transform.localPosition=pin.Number.transform.localPosition;
            float numberWidth=Mathf.Max(T.orderPinNumberBackgroundPixels,pin.Number.GetPreferredValues().x+T.smallSpace);
            pin.NumberBack.transform.localScale=new Vector3(numberWidth,T.orderPinNumberBackgroundPixels,1)/1.28f;
            bool near=Carries(held) && nearSeconds.TryGetValue(node.Id,out _);
            pin.Glow.enabled=near;
            if(near)SetPinLabel(pin,nearSeconds[node.Id]<.5f?"同じ届け先":$"近い・{nearSeconds[node.Id]:F0}秒",T.basil,Color.white);
            else SetPinLabel(pin,(!compact && (map.City==null || map.CitySettings.blockSize/px>=T.cityLabelMinimumBlockPixels))?ShopStrings.House(map,node.Id):"",T.paper,T.line);
            pin.Sprite.color=PinColor(order);
        }
    }
    /// <summary>While a ticket is held, the other orders within a short ride of its address light up.</summary>
    public Color PinColor(Order order)
    {
        if(order==null)return T.disabled;
        if(order.IsReady)return T.orderReadyPin;
        var job=cooking.JobFor(order);
        if(job==null || job.QueuedForOven)return T.orderWaitingPin;
        if(job.Stage>=PizzaStage.Baked)return T.orderReadyPin;
        return job.Stage>=PizzaStage.Baking?T.orderBakingPin:T.orderMakingPin;
    }
    private void UpdateNear(DragPayload held)
    {
        var order=Carries(held)?held.Order:null;if(order==nearFor)return;nearFor=order;nearSeconds.Clear();if(order==null)return;
        var courier=roster.SelectedPerson??roster.People.FirstOrDefault();if(courier==null)return;
        foreach(var other in orders.Orders.Where(o=>o!=order && o.Status==OrderStatus.Pending).Select(o=>o.DestinationNodeId).Distinct())
        {
            var path=Path(order.DestinationNodeId,other);if(path==null)continue;float s=Seconds(path,courier);
            if(s<=nearPinSeconds)nearSeconds[other]=s;
        }
    }
    private void SetPinLabel(Pin pin,string text,Color fill,Color ink)
    {
        if(pin.Label.text!=text)
        {
            pin.Label.text=text;
            float width=text.Length==0?0:pin.Label.GetPreferredValues(text).x+20;
            pin.Pill.size=new Vector2(width/25f,32/25f);
            pin.PillEdge.size=new Vector2((width+7)/25f,39/25f);
        }
        pin.Pill.enabled=pin.PillEdge.enabled=text.Length>0;pin.Pill.color=fill;pin.Label.color=ink;
    }
    private Pin MakePin(MapNode node)
    {
        var pin=new Pin{Root=new GameObject("Pin_"+node.Id).transform};pin.Root.SetParent(pinRoot,false);
        pin.Glow=new GameObject("Glow").AddComponent<SpriteRenderer>();
        pin.Glow.transform.SetParent(pin.Root,false);
        pin.Glow.sprite=UiKit.Circle;
        pin.Glow.color=new Color(T.cheese.r,T.cheese.g,T.cheese.b,.6f);
        pin.Glow.sortingOrder=39;
        pin.Glow.transform.localScale=Vector3.one*72/1.28f;
        pin.Glow.transform.localPosition=new Vector3(0,34,0);
        pin.Glow.enabled=false;
        pin.Sprite=new GameObject("Pin").AddComponent<SpriteRenderer>();pin.Sprite.transform.SetParent(pin.Root,false);pin.Sprite.sortingOrder=40;
        pin.NumberBack=new GameObject("NumberBack").AddComponent<SpriteRenderer>();
        pin.NumberBack.transform.SetParent(pin.Root,false);pin.NumberBack.sprite=UiKit.Circle;
        pin.NumberBack.color=T.paper;pin.NumberBack.sortingOrder=41;pin.NumberBack.enabled=false;
        pin.Number=new GameObject("Number").AddComponent<TextMeshPro>();
        pin.Number.transform.SetParent(pin.Root,false);
        pin.Number.font=UiKit.Font(FontRole.Display);
        pin.Number.fontSize=220;
        pin.Number.color=T.line;
        pin.Number.alignment=TextAlignmentOptions.Center;
        pin.Number.rectTransform.sizeDelta=new Vector2(80,40);
        pin.Number.enableAutoSizing=false;
        pin.Number.textWrappingMode=TextWrappingModes.NoWrap;
        pin.Number.GetComponent<MeshRenderer>().sortingOrder=42;
        pin.Pill=new GameObject("Pill").AddComponent<SpriteRenderer>();
        pin.Pill.transform.SetParent(pin.Root,false);
        pin.Pill.sprite=UiKit.MapLabel;
        pin.Pill.drawMode=SpriteDrawMode.Sliced;
        pin.Pill.color=T.paper;
        pin.Pill.sortingOrder=43;
        pin.Pill.transform.localScale=Vector3.one*25;
        pin.Pill.transform.localPosition=new Vector3(0,node.IsStore?-46:-22,0);
        pin.PillEdge=new GameObject("PillEdge").AddComponent<SpriteRenderer>();
        pin.PillEdge.transform.SetParent(pin.Pill.transform,false);
        pin.PillEdge.sprite=UiKit.MapLabel;
        pin.PillEdge.drawMode=SpriteDrawMode.Sliced;
        pin.PillEdge.color=T.line;
        pin.PillEdge.sortingOrder=42;
        pin.Label=new GameObject("Label").AddComponent<TextMeshPro>();
        pin.Label.transform.SetParent(pin.Root,false);
        pin.Label.font=UiKit.Font(FontRole.Black);
        pin.Label.fontSize=180;
        pin.Label.color=T.line;
        pin.Label.alignment=TextAlignmentOptions.Center;
        pin.Label.rectTransform.sizeDelta=new Vector2(240,34);
        pin.Label.transform.localPosition=new Vector3(0,node.IsStore?-46:-22,0);
        pin.Label.GetComponent<MeshRenderer>().sortingOrder=44;
        pin.Label.text="";return pin;
    }

    // ---- route marks: coloured route legs, and the automatic route as grey dots ----
    private Vector2 NodeAt(string id)=>map.Nodes.First(n=>n.Id==id).Position;
    private List<Vector2> Polyline(string start,IEnumerable<string> edges)
    {
        var points=new List<Vector2>();string at=start;
        foreach(var id in edges)
        {
            var e=map.GetEdge(id);if(!e.TryTraverse(at,out var next))break;
            var road=RoadGeometry.Points(map,e);
            if(at!=e.NodeAId)System.Array.Reverse(road);
            for(int i=0;i+1<road.Length;i++){points.Add(road[i]);points.Add(road[i+1]);}
            at=next;
        }
        return points;
    }
    private static Vector2 Along(List<Vector2> points,float fraction)
    {
        float total=0;for(int i=0;i+1<points.Count;i+=2)total+=(points[i+1]-points[i]).magnitude;float goal=total*fraction;
        for(int i=0;i+1<points.Count;i+=2)
        {
            float len=(points[i+1]-points[i]).magnitude;
            if(goal<=len)return Vector2.Lerp(points[i],points[i+1],len<=0?0:goal/len);
            goal-=len;
        }
        return points.Count>0?points[points.Count-1]:Vector2.zero;
    }
    private void UpdateRouteMarks()
    {
        if(pinRoot==null)return;float px=Px;
        foreach(var id in pins.Keys.Where(id=>!map.HasNode(id)).ToArray()){Destroy(pins[id].Root.gameObject);pins.Remove(id);}
        var person=roster.SelectedPerson;var bag=person!=null && person.IsAvailable?handover.BagOf(person):System.Array.Empty<Order>();
        var legs=bag.Count>0?Legs(person,bag,false):new List<(string from,string to,List<string> edges)>();
        // Separate outbound and return with coloured, offset strokes and moving round direction markers.
        var dotsOnRoute=new List<(Vector2 at,Color color)>();int stroke=0;
        for(int leg=0;leg<legs.Count;leg++)
        {
            var line=Polyline(legs[leg].from,legs[leg].edges);bool returning=leg==legs.Count-1;
            var color=editingPerson==person && editingLeg==leg?T.cheese:returning?T.info:T.tomato;
            float spacing=T.deliveryRouteDotSpacingPixels*px;
            float carry=Mathf.Repeat(Time.unscaledTime*T.deliveryRouteDotSpeedPixels*px,spacing);
            for(int i=0;i+1<line.Count;i+=2)
            {
                var a=line[i];var d=line[i+1]-a;float len=d.magnitude;if(len<=0)continue;var dir=d/len;
                var side=new Vector2(dir.y,-dir.x)*T.deliveryRouteOffsetPixels*px;
                while(routeStrokes.Count<=stroke)routeStrokes.Add(MakeRouteStroke());
                var draw=routeStrokes[stroke++];draw.enabled=true;draw.startWidth=draw.endWidth=T.deliveryRouteWidthPixels*px;
                draw.startColor=draw.endColor=color;
                draw.SetPosition(0,new Vector3(a.x+side.x,a.y+side.y,-1.25f));
                draw.SetPosition(1,new Vector3(a.x+d.x+side.x,a.y+d.y+side.y,-1.25f));
                for(float t=carry;t<len;t+=spacing)dotsOnRoute.Add((a+dir*t+side,color));
                carry=Mathf.Repeat(carry-len,spacing);
            }
        }
        for(int i=stroke;i<routeStrokes.Count;i++)routeStrokes[i].enabled=false;
        while(routeArrows.Count<dotsOnRoute.Count)routeArrows.Add(MakeDirectionDot());
        for(int i=0;i<routeArrows.Count;i++)
        {
            bool on=i<dotsOnRoute.Count;routeArrows[i].gameObject.SetActive(on);if(!on)continue;
            routeArrows[i].position=new Vector3(dotsOnRoute[i].at.x,dotsOnRoute[i].at.y,-1.3f);routeArrows[i].localScale=Vector3.one*px;
        }
        HideMapArrows=router.Current==ScreenId.Business && Zone==ShopZone.Dispatch;
        // automatic route, shown only once the player's route differs from it
        var dots=new List<Vector2>();
        if(bag.Count>0)
        {
            var auto=RouteEdges(person,bag,true);
            if(!auto.SequenceEqual(legs.SelectMany(l=>l.edges)))
            {
                var line=Polyline(map.StoreNodeId,auto);float step=18*px;
                for(int i=0;i+1<line.Count;i+=2)
                {
                    var a=line[i];
                    var b=line[i+1];
                    var d=(b-a);
                    var o=new Vector2(-d.y,d.x).normalized*22*px;
                    int count=Mathf.Max(1,Mathf.FloorToInt(d.magnitude/step));
                    for(int k=0;k<count;k++)dots.Add(a+d*((k+.5f)/count)+o);
                }
            }
        }
        while(autoDots.Count<dots.Count)
        {
            var s=new GameObject("AutoRoute").AddComponent<SpriteRenderer>();
            s.transform.SetParent(pinRoot,false);
            s.sprite=UiKit.Circle;
            s.color=new Color(.45f,.42f,.4f,.85f);
            s.sortingOrder=13;
            autoDots.Add(s);
        }
        for(int i=0;i<autoDots.Count;i++)
        {
            bool on=i<dots.Count;
            autoDots[i].enabled=on;
            if(on)
            {
                autoDots[i].transform.position=new Vector3(dots[i].x,dots[i].y,-1.2f);
                autoDots[i].transform.localScale=Vector3.one*8*px/1.28f;
            }
        }
    }
    private readonly List<Transform> routeArrows=new();
    /// <summary>The map's own small "&gt;" route arrows give way to the dispatch board's chevrons.</summary>
    public static bool HideMapArrows {get;private set;}
    private readonly List<LineRenderer> routeStrokes=new();
    private LineRenderer MakeRouteStroke()
    {
        var line=new GameObject("RouteLegStroke").AddComponent<LineRenderer>();
        line.transform.SetParent(pinRoot,false);
        line.useWorldSpace=true;
        line.positionCount=2;
        line.numCapVertices=6;
        line.sortingOrder=14;
        var road=FindFirstObjectByType<EdgeView>();if(road!=null)line.sharedMaterial=road.GetComponent<LineRenderer>().sharedMaterial;
        return line;
    }
    private Transform MakeDirectionDot()
    {
        var root=new GameObject("RouteDirectionDot").transform;root.SetParent(pinRoot,false);
        foreach(bool edge in new[]{true,false})
        {
            var s=new GameObject(edge?"Edge":"Face").AddComponent<SpriteRenderer>();
            s.transform.SetParent(root,false);
            s.sprite=UiKit.Circle;
            s.color=edge?T.line:T.paper;
            s.sortingOrder=edge?15:16;
            s.transform.localScale=Vector3.one*(edge?10:6)/1.28f;
        }
        return root;
    }
    // ---- map input: drag to pan, wheel to zoom at the cursor, drag pins to lanes, drag route lines to reroute ----
    private bool OverUi(Vector2 screen)
    {
        if(EventSystem.current==null)return false;var hits=new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);return hits.Count>0;
    }
    private int RouteLegAt(Vector2 screen)
    {
        var cam=Camera.main;
        // anywhere on the red line also grabs the leg under the pointer
        var person=roster.SelectedPerson;if(person==null || !person.IsAvailable)return -1;var bag=handover.BagOf(person);if(bag.Count==0)return -1;
        var legs=Legs(person,bag,false);int best=-1;float bestDistance=22;
        for(int i=0;i<legs.Count;i++)
        {
            var line=Polyline(legs[i].from,legs[i].edges);
            for(int k=0;k+1<line.Count;k+=2)
            {
                float d=SegmentDistance(screen,cam.WorldToScreenPoint(line[k]),cam.WorldToScreenPoint(line[k+1]));
                if(d<bestDistance)
                {
                    bestDistance=d;
                    best=i;
                }
            }
        }
        return best;
    }
    private static float SegmentDistance(Vector2 p,Vector2 a,Vector2 b)
    {
        var ab=b-a;
        float t=ab.sqrMagnitude<=0?0:Mathf.Clamp01(Vector2.Dot(p-a,ab)/ab.sqrMagnitude);
        return (a+ab*t-p).magnitude;
    }
    /// <summary>The road under the pointer (screen distance), with backstreets drawn beside the main road.</summary>
    private MapEdge RoadAt(Vector2 screen,float within=48)
    {
        var cam=Camera.main;MapEdge best=null;float bestDistance=within;
        foreach(var e in map.Edges)
        {
            float dist=RoadGeometry.DistanceToScreenPoint(cam,RoadGeometry.Points(map,e),screen);if(dist<bestDistance){bestDistance=dist;best=e;}
        }
        return best;
    }
    /// <summary>Last map press as seen by the board (for checking input in the editor).</summary>
    public string InputTrace {get;private set;}="";
    private (string,string) dragLeg;private string dragOriginal,dragTarget;
    private void BeginRouteDrag(int index)
    {
        var person=roster.SelectedPerson;var legs=Legs(person,handover.BagOf(person),false);
        dragLeg=(legs[index].from,legs[index].to);Waypoints(person).TryGetValue(dragLeg,out dragOriginal);dragTarget=dragOriginal;
    }
    /// <summary>Live preview: the leg follows the road under the pointer while the line is dragged.</summary>
    private void PreviewRouteDrag(Vector2 screen)
    {
        var person=roster.SelectedPerson;if(person==null)return;var road=RoadAt(screen);
        string target=road!=null?"E:"+road.Id:dragOriginal;if(target==dragTarget)return;
        if(target!=null && Via(dragLeg.Item1,dragLeg.Item2,target)==null)return;
        dragTarget=target;var w=Waypoints(person);if(target==null)w.Remove(dragLeg);else w[dragLeg]=target;SyncRoute();
    }
    private Order PinOrderAt(Vector2 screen)
    {
        var cam=Camera.main;
        foreach(var pair in pins)
        {
            if(!pair.Value.Root.gameObject.activeSelf || !pair.Value.Sprite.enabled)continue;
            Vector2 head=cam.WorldToScreenPoint(pair.Value.Sprite.transform.position);
            Vector2 number=cam.WorldToScreenPoint(pair.Value.Number.transform.position);
            if(Mathf.Min((head-screen).magnitude,(number-screen).magnitude)>36)continue;
            var person=roster.SelectedPerson;var bagged=person!=null?handover.BagOf(person).FirstOrDefault(o=>o.DestinationNodeId==pair.Key):null;
            return bagged??handover.RackOrders.FirstOrDefault(o=>o.DestinationNodeId==pair.Key)??orders.Orders.FirstOrDefault(o=>o.DestinationNodeId==pair.Key && o.Status==OrderStatus.Pending);
        }
        return null;
    }
    private DragPayload PinPayload(Order o)
    {
        if(o==null || o.Status!=OrderStatus.Pending || !o.IsReady)return null;
        return new DragPayload{Kind=handover.IsBagged(o)?DragKind.Bagged:DragKind.Box,Order=o,Sprite="box_closed",Size=new Vector2(96,96)};
    }
    private void Navigate()
    {
        var mouse=Mouse.current;var cam=Camera.main;if(mouse==null || cam==null || menu.MenuOpen || menu.HelpOpen)return;
        Vector2 pos=mouse.position.ReadValue();bool over=OverUi(pos);
        if(mouse.rightButton.wasPressedThisFrame){editingPerson=null;editingLeg=-1;}
        float scroll=mouse.scroll.ReadValue().y;if(scroll!=0 && !over)Zoom(Mathf.Pow(.85f,Mathf.Clamp(scroll/120f,-1,1)),pos,true);
        if(mouse.leftButton.wasPressedThisFrame){if(over || Held!=null)InputTrace=$"press {pos} ignored (over UI {over})";else PointerDown(pos);}
        if(pressing && mouse.leftButton.isPressed)PointerMove(pos);
        if(pressing && mouse.leftButton.wasReleasedThisFrame)PointerUp(pos);
        if(mouse.middleButton.isPressed && !over)PanBy(mouse.delta.ReadValue(),cam);
    }
    // Map pointer steps (screen pixels). The mouse drives them every frame; checks can call them directly.
    public void PointerDown(Vector2 pos)
    {
        pressing=true;dragging=pinDragging=routeDragging=false;pressPosition=lastPointer=pos;
        routeLegPress=editingPerson==null?RouteLegAt(pos):-1;pinPress=routeLegPress<0?PinOrderAt(pos):null;InputTrace=$"press {pos} route leg {routeLegPress}";
    }
    public void PointerMove(Vector2 pos)
    {
        var cam=Camera.main;if(!pressing || cam==null)return;
        if(!dragging && (pos-pressPosition).magnitude>8)
        {
            dragging=true;
            if(routeLegPress>=0){routeDragging=true;BeginRouteDrag(routeLegPress);}
            else if(PinPayload(pinPress) is DragPayload payload && ShopHand.Instance!=null){pinDragging=true;ShopHand.Instance.BeginAt(payload,pos);}
        }
        if(pinDragging)ShopHand.Instance?.MoveAt(pos);
        else if(routeDragging)
        {
            PreviewRouteDrag(pos);
        }
        else if(dragging)PanBy(pos-lastPointer,cam);
        lastPointer=pos;
    }
    public void PointerUp(Vector2 pos)
    {
        if(!pressing)return;pressing=false;
        if(pinDragging){pinDragging=false;ShopHand.Instance?.EndAt(pos);}
        else if(routeDragging)
        {
            routeDragging=false;
            InputTrace+=$" / released {pos} on {RoadAt(pos)?.Id}";
            if(RoadAt(pos)==null)Toast("赤い線を、通りたい道の上まで引っぱって離します。",T.cheese,null,"icon_map");
        }
        else if(!dragging)Tap(pos);
        routeLegPress=-1;pinPress=null;
    }
    private static void PanBy(Vector2 delta,Camera cam)
    {
        float scale=cam.orthographicSize*2f/cam.pixelHeight;
        cam.transform.position-=new Vector3(delta.x,delta.y,0)*scale;
    }
    private void Zoom(float factor,Vector2 screen,bool atCursor)
    {
        var cam=Camera.main;if(cam==null)return;
        Vector3 before=atCursor?cam.ScreenToWorldPoint(screen):cam.transform.position;
        cam.orthographicSize=Mathf.Clamp(cam.orthographicSize*factor,1.2f,40f);
        if(atCursor){Vector3 after=cam.ScreenToWorldPoint(screen);cam.transform.position+=before-after;}
    }
    /// <summary>A tap on a pin puts a baked order into the selected (or first free) lane; on a bagged pin it selects that lane.</summary>
    public void Tap(Vector2 screen)
    {
        if(editingPerson!=null && editingLeg>=0)
        {
            var road=RoadAt(screen);
            if(road!=null && SetWaypointRoad(editingPerson,editingLeg,road.Id))Toast(map.RoadCaption(road.Id)+" を通ります。",T.basil,null,"icon_map");
            return;
        }
        var order=PinOrderAt(screen);if(order!=null)TapOrder(order);
    }
    public void TapOrder(Order order)
    {
        if(order.Status!=OrderStatus.Pending)return;
        if(handover.IsBagged(order)){handover.Select(handover.Holder(order));return;}
        if(!order.IsReady)
        {
            var eta=cooking.ReadyIn(order);
            Toast(eta.HasValue?$"#{Number(order)} はまだ焼けていません（あと約{Mathf.CeilToInt(eta.Value)}秒）。":$"#{Number(order)} はまだ焼けていません。",T.cheese,null,"icon_pizza");
            return;
        }
        QuickBag(order);
    }

    // ---- minimap ----
    private void SetMinimap(bool on)
    {
        if(on && minimapCamera==null && map!=null && map.IsReady)
        {
            minimapTexture=new RenderTexture(480,320,16){name="Minimap"};
            var go=new GameObject("MinimapCamera");
            minimapCamera=go.AddComponent<Camera>();
            minimapCamera.orthographic=true;
            minimapCamera.clearFlags=CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor=T.mapGround;
            minimapCamera.targetTexture=minimapTexture;
            minimapCamera.depth=-10;
            minimapCamera.cullingMask=Camera.main!=null?Camera.main.cullingMask:~0;
            float minX=map.Nodes.Min(n=>n.Position.x)-2,maxX=map.Nodes.Max(n=>n.Position.x)+2,minY=map.Nodes.Min(n=>n.Position.y)-2,maxY=map.Nodes.Max(n=>n.Position.y)+2;
            go.transform.position=new Vector3((minX+maxX)/2,(minY+maxY)/2,-10);
            minimapCamera.aspect=1.5f;
            minimapCamera.orthographicSize=Mathf.Max((maxY-minY)/2,(maxX-minX)/3);
            minimapImage.texture=minimapTexture;
        }
        if(minimapCamera!=null)minimapCamera.enabled=on;
    }
    private void UpdateMinimap()
    {
        var cam=Camera.main;if(minimapCamera==null || cam==null)return;
        var rect=minimapImage.rectTransform.rect;
        Vector3 a=minimapCamera.WorldToViewportPoint(cam.ViewportToWorldPoint(new Vector3(0,0,0))),b=minimapCamera.WorldToViewportPoint(cam.ViewportToWorldPoint(new Vector3(1,1,0)));
        float x0=Mathf.Clamp01(a.x)*rect.width,y0=(1-Mathf.Clamp01(b.y))*rect.height,x1=Mathf.Clamp01(b.x)*rect.width,y1=(1-Mathf.Clamp01(a.y))*rect.height;
        UiKit.Place(minimapFrame,x0,y0,Mathf.Max(4,x1-x0),Mathf.Max(4,y1-y0));var w=minimapFrame.sizeDelta.x;var h=minimapFrame.sizeDelta.y;
        UiKit.Place(minimapEdges[0],0,0,w,2);
        UiKit.Place(minimapEdges[1],0,h-2,w,2);
        UiKit.Place(minimapEdges[2],0,0,2,h);
        UiKit.Place(minimapEdges[3],w-2,0,2,h);
    }
    private void OnDestroy(){if(minimapTexture!=null)minimapTexture.Release();if(minimapCamera!=null)Destroy(minimapCamera.gameObject);}
}
