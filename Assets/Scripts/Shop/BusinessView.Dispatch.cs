using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public sealed partial class BusinessView
{
    private RectTransform stepsBar,courierCard,bubble,bottomBar,routeChips,minimapFrame,switcher;
    [SerializeField, HideInInspector] private RectTransform[] steps=new RectTransform[4];
    private TextMeshProUGUI chipTime,chipCash,chipReputation,bubbleText,courierName,courierState,bagTitle,growthText,routeTime,routeFit;
    private RectTransform readyBadge,speedBadge,bagSlots,growthBox,courierAvatar;
    private StickerButton departButton,suggestButton,redoButton,trafficButton;
    private RawImage minimapImage;private Camera minimapCamera;private RenderTexture minimapTexture;
    private string bagSignature="",chipSignature="";
    private bool pressing,dragging;private Vector2 pressPosition,lastPointer;
    private Transform pinRoot;private readonly Dictionary<string,(Transform pin,SpriteRenderer sprite,TextMeshPro number,TextMeshPro label,SpriteRenderer pill)> pins=new();
    public static bool ShowCongestion {get;private set;}=true;

    private void BuildDispatch()
    {
        // steps bar
        stepsBar=UiKit.Box(dispatch,"Steps",32,22,1180,72);
        string[] names={ShopStrings.Step1,ShopStrings.Step2,ShopStrings.Step3,ShopStrings.Step4};float x=0;
        for(int i=0;i<4;i++)
        {
            float w=i==2?360:i==3?140:250;var pill=UiKit.Card(stepsBar,"Step"+i,x,0,w,64,T.paper,32,4,3);steps[i]=pill;
            var circle=UiKit.Box(pill,"Circle",10,10,44,44);var c=circle.gameObject.AddComponent<Image>();c.sprite=UiKit.Circle;c.raycastTarget=false;
            UiKit.Label(circle,"Number",(i+1).ToString(),0,0,44,44,22,Color.white,FontRole.Display,TextAlignmentOptions.Center);
            UiKit.Label(pill,"Text",names[i],62,0,w-70,64,20,T.line,FontRole.Black);x+=w+(i<3?44:0);
            if(i<3)UiKit.Icon(stepsBar,"Arrow"+i,"icon_play",x-36,20,24,T.inkSub);UiKit.Icon(circle,"Check","icon_check",8,8,28,Color.white);
        }
        // status chips
        var chips=UiKit.Box(dispatch,"Chips",1490,22,400,72);
        chipTime=Chip(chips,0,140,"icon_clock");chipCash=Chip(chips,150,130,"icon_coin");chipReputation=Chip(chips,290,110,"icon_star");
        // courier card
        switcher=UiKit.Box(dispatch,"Switcher",40,112,330,60);
        courierCard=UiKit.Card(dispatch,"CourierCard",40,176,330,330,T.paper);
        courierAvatar=UiKit.Avatar(courierCard,"Avatar",20,18,70,T.info,"?");
        courierName=UiKit.Label(courierCard,"Name","",104,16,140,40,30,T.line,FontRole.Display);
        courierState=UiKit.Label(courierCard,"State","",104,56,200,30,18,T.inkSub,FontRole.Black);
        readyBadge=UiKit.Badge(courierCard,"Ready","準備OK",230,28,T.basil,Color.white,34,16);
        bagTitle=UiKit.Label(courierCard,"BagTitle","バッグ 0 / 2",20,108,290,34,22,T.line,FontRole.Black);
        bagSlots=UiKit.Box(courierCard,"BagSlots",20,148,290,90);
        growthBox=UiKit.Box(courierCard,"Growth",20,248,290,64);UiKit.Dashed(growthBox,T.tomato,10,6,2);
        growthText=UiKit.Label(growthBox,"Text","",12,4,270,56,16,T.tomato,FontRole.Black,TextAlignmentOptions.MidlineLeft,true);
        // guide bubble
        bubble=UiKit.Card(dispatch,"Bubble",1320,140,420,130,T.paper,18,6,3);
        UiKit.Badge(bubble,"Here","いまここ",20,16,T.cheese,T.line,32,16);
        bubbleText=UiKit.Label(bubble,"Text","",20,56,380,64,19,T.line,FontRole.Black,TextAlignmentOptions.TopLeft,true);
        // map controls
        var zoom=UiKit.Card(dispatch,"Zoom",1808,190,76,150,T.paper,14,6,3);
        UiKit.Button(zoom,"ZoomIn","",StickerKind.Secondary,4,4,68,68,20,()=>Zoom(.8f,Vector2.zero,false),FontRole.Black,"icon_plus");
        UiKit.Button(zoom,"ZoomOut","",StickerKind.Secondary,4,78,68,68,20,()=>Zoom(1.25f,Vector2.zero,false),FontRole.Black,"icon_minus");
        UiKit.Button(dispatch,"Fit","",StickerKind.Secondary,1808,360,76,76,20,FitMap,FontRole.Black,"icon_target");
        trafficButton=UiKit.Button(dispatch,"Traffic","混雑",StickerKind.Selected,1808,454,76,76,16,()=>{ShowCongestion=!ShowCongestion;},FontRole.Black);
        // minimap
        var mini=UiKit.Card(dispatch,"Minimap",40,780,248,168,T.paper,14,6,3);
        minimapImage=UiKit.Stretch(mini,"Image",6,6,6,6).gameObject.AddComponent<RawImage>();minimapImage.raycastTarget=false;
        minimapFrame=UiKit.Box(minimapImage.transform,"Frame",0,0,40,30);
        foreach(var side in new[]{"T","B","L","R"}){var s=UiKit.Box(minimapFrame,side,0,0,1,1);UiKit.Fill(s,T.tomato,0);}
        // bottom summary bar
        bottomBar=UiKit.Card(dispatch,"Summary",330,950,1558,108,T.paper,22,6,3);
        routeChips=UiKit.Box(bottomBar,"Route",24,24,560,60);
        var divider=UiKit.Box(bottomBar,"Divider",600,18,3,72);UiKit.Fill(divider,T.paperDeep,0);
        routeTime=UiKit.Label(bottomBar,"Time","—",626,8,170,58,38,T.line,FontRole.Display);
        speedBadge=UiKit.Badge(bottomBar,"Speed","速い",790,24,T.basil,Color.white,32,16);
        routeFit=UiKit.Label(bottomBar,"Fit","",626,62,340,32,17,T.inkSub,FontRole.Black);
        redoButton=UiKit.Button(bottomBar,"Redo",ShopStrings.Redo,StickerKind.Secondary,972,20,164,68,22,()=>planner.ClearRoute());
        suggestButton=UiKit.Button(bottomBar,"Suggest",ShopStrings.Suggested,StickerKind.Secondary,1148,20,188,68,22,()=>planner.ApplySuggestion());
        departButton=UiKit.Button(bottomBar,"Depart",ShopStrings.Depart+" ▶",StickerKind.Primary,1350,8,192,90,34,Depart,FontRole.Display);
        UiKit.Show(dispatch,false);
    }
    private TextMeshProUGUI Chip(Transform parent,float x,float w,string icon)
    {
        var card=UiKit.Card(parent,"Chip",x,0,w,62,T.paper,14,4,3);UiKit.Icon(card,"Icon",icon,12,15,32,T.line);
        return UiKit.Label(card,"Text","",48,0,w-56,62,26,T.line,FontRole.Display);
    }
    private void EnterDispatch()
    {
        if(roster.SelectedPerson==null){var withBag=roster.People.FirstOrDefault(p=>p.IsAvailable && handover.BagOf(p).Count>0);if(withBag!=null)handover.Select(withBag);}
        FitMap();
    }
    /// <summary>Fit the whole map into the area left free by the cards and bars (left 400, right 130, top 110, bottom 140 of 1920x1080).</summary>
    public void FitMap()
    {
        var cam=Camera.main;if(cam==null || map==null || !map.IsReady)return;
        float minX=map.Nodes.Min(n=>n.Position.x)-.9f,maxX=map.Nodes.Max(n=>n.Position.x)+.9f,minY=map.Nodes.Min(n=>n.Position.y)-.9f,maxY=map.Nodes.Max(n=>n.Position.y)+.9f;
        float fl=400f/1920,fr=1790f/1920,fb=140f/1080,ft=970f/1080,aspect=cam.aspect;
        float size=Mathf.Max((maxY-minY)/(2*(ft-fb)),(maxX-minX)/(2*aspect*(fr-fl)));cam.orthographicSize=size;
        var centre=new Vector2((minX+maxX)/2,(minY+maxY)/2);var offset=new Vector2(((fl+fr)/2-.5f)*2*size*aspect,((fb+ft)/2-.5f)*2*size);
        cam.transform.position=new Vector3(centre.x-offset.x,centre.y-offset.y,cam.transform.position.z);
    }
    private void Depart()
    {
        var person=roster.SelectedPerson;if(person==null)return;
        string reason=flow.DispatchRejectionReason(planner.SelectedPlan,person);
        if(reason.Length>0){Toast(UiStrings.Japanese(reason),T.berry,null,"icon_bike");return;}
        planner.Dispatch();Toast($"{person.Name} 出発!",T.basil,null,"icon_bike");
        var next=roster.People.FirstOrDefault(p=>p.IsAvailable && handover.BagOf(p).Count>0);if(next!=null)handover.Select(next);
    }

    private int Step()
    {
        var person=roster.SelectedPerson;
        if(person==null || !person.IsAvailable)return 0;
        if(handover.BagOf(person).Count==0)return 1;
        if(planner.SelectedPlan==null)return 2;
        return 3;
    }
    private void RefreshDispatch()
    {
        UpdatePins();
        if(Zone!=ShopZone.Dispatch)return;
        Navigate();UpdateMinimap();
        int step=Step();
        for(int i=0;i<4;i++)
        {
            bool done=i<step,current=i==step;var face=UiKit.FaceOf(steps[i]);face.color=current?T.cheese:T.paper;
            var circle=steps[i].Find("Circle").GetComponent<Image>();circle.color=done?T.basil:current?T.tomato:T.disabled;
            steps[i].Find("Circle/Number").gameObject.SetActive(!done);steps[i].Find("Circle/Check").gameObject.SetActive(done);
        }
        bubbleText.text=step switch
        {
            0=>"受け渡しの配達員を選びましょう。上の顔を押しても選べます。",
            1=>"受け渡しで箱をバッグに入れましょう。＋を押すと台の箱が入ります。",
            2=>"届け先のピンを、行く順番にタップ。ルートが引かれます。",
            _=>"準備OK! 出発を押しましょう。"
        };
        chipTime.text=$"{Mathf.CeilToInt(clock.RemainingSeconds)/60}:{Mathf.CeilToInt(clock.RemainingSeconds)%60:00}";chipCash.text=UiKit.Yen(economy.Cash);chipReputation.text=Mathf.RoundToInt(reputation.Value).ToString();
        trafficButton.Kind=ShowCongestion?StickerKind.Selected:StickerKind.Secondary;
        var person=roster.SelectedPerson;
        string sig=string.Join(",",roster.People.Select(p=>p.Id+p.Status+handover.BagOf(p).Count))+"|"+(person?.Id)+"|"+string.Join(",",handover.RackOrders.Select(o=>o.Id))+"|"+(planner.SelectedPlan!=null);
        if(sig!=bagSignature){bagSignature=sig;RebuildCourierCard();}
        RefreshRouteSummary(person);
    }
    private void RebuildCourierCard()
    {
        for(int i=switcher.childCount-1;i>=0;i--)Destroy(switcher.GetChild(i).gameObject);
        float x=0;foreach(var p in roster.People)
        {
            var b=UiKit.Button(switcher,"Pick_"+p.Name,p.Name.Substring(0,1),roster.SelectedPerson==p?StickerKind.Selected:StickerKind.Secondary,x,0,56,56,24,null,FontRole.Display);
            var pp=p;b.Button.onClick.AddListener(()=>handover.Select(pp));b.Interactable=p.IsAvailable;b.Label.color=p.IsAvailable?T.CourierColor(p.Name):T.disabled;x+=64;
        }
        var person=roster.SelectedPerson;
        courierCard.gameObject.SetActive(true);
        if(person==null){courierName.text="配達員を選ぶ";courierState.text="";UiKit.SetAvatar(courierAvatar,T.disabled,"?");readyBadge.gameObject.SetActive(false);bagTitle.text="";ClearSlots();growthBox.gameObject.SetActive(false);return;}
        UiKit.SetAvatar(courierAvatar,T.CourierColor(person.Name),person.Name.Substring(0,1));
        courierName.text=person.Name;courierState.text=person.IsAvailable?"店で待機中":ShopStrings.Status(person.Status);
        bool ready=planner.SelectedPlan!=null;UiKit.SetBadge(readyBadge,ready?"準備OK":"準備中",ready?T.basil:T.disabled,ready?Color.white:T.line);readyBadge.gameObject.SetActive(true);
        var bag=handover.BagOf(person);bagTitle.text=$"バッグ  {bag.Count} / {person.Capacity}";
        ClearSlots();
        for(int s=0;s<person.Capacity;s++)
        {
            float w=(290-(person.Capacity-1)*10f)/person.Capacity;w=Mathf.Min(w,90);
            if(s<bag.Count)
            {
                var o=bag[s];var b=UiKit.Button(bagSlots,"Bag_"+Number(o),"#"+Number(o),StickerKind.Selected,s*(w+10),0,w,84,24,()=>handover.Unbag(o),FontRole.Display);
                b.transform.Find("Face/Fill").GetComponent<Image>().color=T.cheeseLight;
            }
            else
            {
                var slot=UiKit.Box(bagSlots,"Empty"+s,s*(w+10),0,w,84);UiKit.Dashed(slot,T.inkSub,10,6,2);
                var add=slot.gameObject.AddComponent<Button>();slot.gameObject.AddComponent<StickerCardAction>().Apply();
                UiKit.Icon(slot,"Plus","icon_plus",w/2-18,24,36,T.inkSub);var p=person;
                add.onClick.AddListener(()=>{var next=handover.RackOrders.FirstOrDefault();if(next==null)Toast("受け渡しの台に箱がありません。",T.cheese,null,"icon_box");else handover.Bag(next,p);});
            }
        }
        float potential=person.Potential(CourierAbility.Capacity);
        growthBox.gameObject.SetActive(person.Capacity<potential);((RectTransform)courierCard).sizeDelta=new Vector2(330,person.Capacity<potential?330:258);
        var training=FindFirstObjectByType<TrainingManager>();
        growthText.text=$"成長で増える\n積載量 {person.Capacity} → {person.Capacity+1}（訓練 {UiKit.Yen(training.Cost(person,CourierAbility.Capacity))}）";
    }
    private void ClearSlots(){for(int i=bagSlots.childCount-1;i>=0;i--)Destroy(bagSlots.GetChild(i).gameObject);}
    private void RefreshRouteSummary(DeliveryPerson person)
    {
        var route=planner.Route;var bag=person!=null?handover.BagOf(person):System.Array.Empty<Order>();
        var visits=route==null?new List<Order>():bag.Where(o=>route.HasVisited(o.DestinationNodeId)).OrderBy(o=>route.NodeIds.ToList().IndexOf(o.DestinationNodeId)).ToList();
        string sig=string.Join(",",visits.Select(o=>o.Id))+"|"+(route?.EdgeIds.Count??0)+"|"+(person?.Id);
        if(sig!=chipSignature)
        {
            chipSignature=sig;for(int i=routeChips.childCount-1;i>=0;i--)Destroy(routeChips.GetChild(i).gameObject);
            float x=0;var store=UiKit.Badge(routeChips,"Store",ShopStrings.Store,x,12,T.tomato,Color.white,40,20);x+=store.sizeDelta.x+10;
            for(int i=0;i<visits.Count;i++)
            {
                UiKit.Icon(routeChips,"Arrow"+i,"icon_play",x+2,22,20,T.line);x+=32;
                var chip=UiKit.Badge(routeChips,"Visit"+i,$"{i+1}  {ShopStrings.House(map,visits[i].DestinationNodeId)}",x,12,T.paper,T.line,40,19);x+=chip.sizeDelta.x+10;
            }
        }
        bool hasRoute=route!=null && route.EdgeIds.Count>0 && person!=null;
        float time=hasRoute?clock.ToRealSeconds(route.GetTime(person)):0;
        routeTime.text=hasRoute?$"{time:F1}<size=22>秒</size>":"—";
        float? best=planner.IndividualReferenceTime.HasValue?clock.ToRealSeconds(planner.IndividualReferenceTime.Value):(float?)null;
        speedBadge.gameObject.SetActive(hasRoute && planner.SelectedPlan!=null && best.HasValue);
        if(best.HasValue){float saved=best.Value-time;bool fast=saved>=-.05f;string difference=Mathf.Abs(saved)<.05f?"個別と同じ":saved>0?$"{saved:F1}秒短縮":$"{-saved:F1}秒遅い";UiKit.SetBadge(speedBadge,difference,fast?T.basil:T.cheese,fast?Color.white:T.line);speedBadge.anchoredPosition=new Vector2(626+routeTime.GetPreferredValues().x+12,-24);}
        if(hasRoute && bag.Count>0)
        {
            int late=Late(route,person,bag);
            routeFit.text=(best.HasValue?$"個別 {best.Value:F1}秒 / ":"")+(late==0?"期限内に到着予定":$"{late}件 期限に遅れそう");routeFit.color=late==0?T.basil:T.berry;
        }
        else routeFit.text=bag.Count>0?"ピンを順番にタップ":"";
        departButton.Interactable=person!=null && flow.CanDispatch(planner.SelectedPlan,person);
        suggestButton.Interactable=planner.Suggestion!=null;redoButton.Interactable=hasRoute;
    }
    private int Late(ManualDeliveryRoute route,DeliveryPerson person,IReadOnlyList<Order> bag)
    {
        float t=clock.ElapsedSeconds;var arrival=new Dictionary<string,float>();
        for(int i=0;i<route.EdgeIds.Count;i++){t+=clock.ToRealSeconds(map.GetTravelTime(route.EdgeIds[i],person));var node=route.NodeIds[i+1];if(!arrival.ContainsKey(node))arrival[node]=t;}
        return bag.Count(o=>!arrival.TryGetValue(o.DestinationNodeId,out var at) || at>o.Deadline);
    }

    // ---- pins on the map (world space, constant screen size) ----
    private void UpdatePins()
    {
        if(map==null || !map.IsReady)return;var cam=Camera.main;if(cam==null)return;
        if(pinRoot==null){pinRoot=new GameObject("ShopMapPins").transform;pinRoot.SetParent(map.transform,false);}
        bool show=router.Current==ScreenId.Business && Zone==ShopZone.Dispatch;pinRoot.gameObject.SetActive(show);if(!show)return;
        float px=cam.orthographicSize*2f/Mathf.Max(1,cam.pixelHeight);
        var person=roster.SelectedPerson;var bag=person!=null?handover.BagOf(person):System.Array.Empty<Order>();var route=planner.Route;
        var bagNodes=bag.Select(o=>o.DestinationNodeId).ToList();
        var pending=orders.Orders.Select(o=>o.DestinationNodeId).ToHashSet();
        foreach(var node in map.Nodes)
        {
            if(!node.IsStore && node.NodeType!=NodeType.DeliveryAddress)continue;
            if(!pins.TryGetValue(node.Id,out var pin))pin=pins[node.Id]=MakePin(node);
            pin.pin.position=new Vector3(node.Position.x,node.Position.y,-1);pin.pin.localScale=Vector3.one*px;
            if(node.IsStore){pin.sprite.sprite=UiKit.Art("shop_badge");pin.sprite.transform.localPosition=Vector3.zero;pin.sprite.transform.localScale=Vector3.one*40;pin.number.text="";pin.label.text=ShopStrings.Store;continue;}
            bool inBag=bagNodes.Contains(node.Id);
            pin.sprite.sprite=UiKit.Art(inBag?"pin_drop":"pin_white");pin.sprite.transform.localScale=Vector3.one*(inBag?50:42);pin.sprite.transform.localPosition=new Vector3(0,inBag?36:30,0);
            int index=-1;if(inBag && route!=null){var ordered=bagNodes.Where(route.HasVisited).Distinct().OrderBy(n=>route.NodeIds.ToList().IndexOf(n)).ToList();index=ordered.IndexOf(node.Id);}
            pin.number.text=index>=0?(index+1).ToString():"";pin.number.transform.localPosition=new Vector3(0,inBag?44:36,0);
            pin.label.text=ShopStrings.House(map,node.Id);
            pin.sprite.color=new Color(1,1,1,inBag || pending.Contains(node.Id)?1:.55f);
        }
    }
    private (Transform,SpriteRenderer,TextMeshPro,TextMeshPro,SpriteRenderer) MakePin(MapNode node)
    {
        var root=new GameObject("Pin_"+node.Id).transform;root.SetParent(pinRoot,false);
        var s=new GameObject("Pin").AddComponent<SpriteRenderer>();s.transform.SetParent(root,false);s.sortingOrder=40;
        var number=new GameObject("Number").AddComponent<TextMeshPro>();number.transform.SetParent(root,false);number.font=UiKit.Font(FontRole.Display);number.fontSize=220;number.color=T.line;number.alignment=TextAlignmentOptions.Center;number.rectTransform.sizeDelta=new Vector2(40,40);number.GetComponent<MeshRenderer>().sortingOrder=42;
        var pill=new GameObject("Pill").AddComponent<SpriteRenderer>();pill.transform.SetParent(root,false);pill.sprite=UiKit.Round;pill.drawMode=SpriteDrawMode.Sliced;pill.color=T.paper;pill.sortingOrder=43;pill.transform.localScale=Vector3.one*25;pill.transform.localPosition=new Vector3(0,-22,0);
        var edge=new GameObject("PillEdge").AddComponent<SpriteRenderer>();edge.transform.SetParent(pill.transform,false);edge.sprite=UiKit.Round;edge.drawMode=SpriteDrawMode.Sliced;edge.color=T.line;edge.sortingOrder=42;
        var label=new GameObject("Label").AddComponent<TextMeshPro>();label.transform.SetParent(root,false);label.font=UiKit.Font(FontRole.Black);label.fontSize=180;label.color=T.line;label.alignment=TextAlignmentOptions.Center;label.rectTransform.sizeDelta=new Vector2(240,34);label.transform.localPosition=new Vector3(0,-22,0);label.GetComponent<MeshRenderer>().sortingOrder=44;
        label.text=node.IsStore?ShopStrings.Store:ShopStrings.House(map,node.Id);float width=label.GetPreferredValues(label.text).x+20;
        pill.size=new Vector2(width/25f,32/25f);edge.size=new Vector2((width+7)/25f,39/25f);
        return (root,s,number,label,pill);
    }

    // ---- map navigation: drag to pan, wheel to zoom at the cursor, tap pins or roads ----
    private bool OverUi(Vector2 screen)
    {
        if(EventSystem.current==null)return false;var hits=new List<RaycastResult>();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);return hits.Count>0;
    }
    private void Navigate()
    {
        var mouse=Mouse.current;var cam=Camera.main;if(mouse==null || cam==null || menu.MenuOpen || menu.HelpOpen)return;
        Vector2 pos=mouse.position.ReadValue();bool over=OverUi(pos);
        float scroll=mouse.scroll.ReadValue().y;if(scroll!=0 && !over)Zoom(Mathf.Pow(.85f,Mathf.Clamp(scroll/120f,-1,1)),pos,true);
        if(mouse.leftButton.wasPressedThisFrame && !over){pressing=true;dragging=false;pressPosition=lastPointer=pos;}
        if(pressing && mouse.leftButton.isPressed)
        {
            if(!dragging && (pos-pressPosition).magnitude>8)dragging=true;
            if(dragging)PanBy(pos-lastPointer,cam);lastPointer=pos;
        }
        if(pressing && mouse.leftButton.wasReleasedThisFrame){pressing=false;if(!dragging)Tap(pos);}
        if(mouse.middleButton.isPressed && !over)PanBy(mouse.delta.ReadValue(),cam);
    }
    private static void PanBy(Vector2 delta,Camera cam){float scale=cam.orthographicSize*2f/cam.pixelHeight;cam.transform.position-=new Vector3(delta.x,delta.y,0)*scale;}
    private void Zoom(float factor,Vector2 screen,bool atCursor)
    {
        var cam=Camera.main;if(cam==null)return;
        Vector3 before=atCursor?cam.ScreenToWorldPoint(screen):cam.transform.position;
        cam.orthographicSize=Mathf.Clamp(cam.orthographicSize*factor,1.2f,40f);
        if(atCursor){Vector3 after=cam.ScreenToWorldPoint(screen);cam.transform.position+=before-after;}
    }
    public void Tap(Vector2 screen)
    {
        var cam=Camera.main;if(cam==null)return;
        foreach(var pair in pins)
        {
            var node=map.Nodes.FirstOrDefault(n=>n.Id==pair.Key);if(node==null || node.IsStore)continue;
            Vector2 head=cam.WorldToScreenPoint(pair.Value.sprite.transform.position);
            if((head-screen).magnitude<=36){TapPin(node.Id);return;}
        }
        map.HandleMapClick(screen);
    }
    public bool TapPin(string nodeId)
    {
        var person=roster.SelectedPerson;var route=planner.Route;
        if(person==null){Toast("先に配達員を選びましょう。",T.cheese,null,"icon_bike");return false;}
        var bag=handover.BagOf(person);if(bag.Count==0){Toast("バッグに箱を入れましょう。",T.cheese,null,"icon_box");return false;}
        if(!bag.Any(o=>o.DestinationNodeId==nodeId)){Toast("この届け先の箱はバッグにありません。",T.cheese,null,"icon_box");return false;}
        if(route==null || route.HasVisited(nodeId)){Toast("この届け先はもう道順に入っています。",T.cheese,null,"icon_map");return false;}
        var edges=route.EdgeIds.ToList();
        var path=RoadPathfinder.FindShortestPath(map.Edges,route.CurrentNodeId,nodeId,e=>map.GetTravelTime(e.Id,person));
        if(path==null){Toast("そこへ行く道がありません。",T.berry,null,"icon_map");return false;}
        edges.AddRange(path);
        var visited=new HashSet<string>(route.NodeIds){nodeId};foreach(var id in path){var e=map.GetEdge(id);visited.Add(e.NodeAId);visited.Add(e.NodeBId);}
        if(bag.All(o=>visited.Contains(o.DestinationNodeId)))
        {var home=RoadPathfinder.FindShortestPath(map.Edges,nodeId,map.StoreNodeId,e=>map.GetTravelTime(e.Id,person));if(home!=null)edges.AddRange(home);}
        planner.ReplaceRoute(edges);return true;
    }

    // ---- minimap ----
    private void SetMinimap(bool on)
    {
        if(on && minimapCamera==null && map!=null && map.IsReady)
        {
            minimapTexture=new RenderTexture(480,320,16){name="Minimap"};
            var go=new GameObject("MinimapCamera");minimapCamera=go.AddComponent<Camera>();minimapCamera.orthographic=true;minimapCamera.clearFlags=CameraClearFlags.SolidColor;
            minimapCamera.backgroundColor=T.mapGround;minimapCamera.targetTexture=minimapTexture;minimapCamera.depth=-10;minimapCamera.cullingMask=Camera.main!=null?Camera.main.cullingMask:~0;
            float minX=map.Nodes.Min(n=>n.Position.x)-2,maxX=map.Nodes.Max(n=>n.Position.x)+2,minY=map.Nodes.Min(n=>n.Position.y)-2,maxY=map.Nodes.Max(n=>n.Position.y)+2;
            go.transform.position=new Vector3((minX+maxX)/2,(minY+maxY)/2,-10);minimapCamera.aspect=1.5f;minimapCamera.orthographicSize=Mathf.Max((maxY-minY)/2,(maxX-minX)/3);
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
        UiKit.Place((RectTransform)minimapFrame.Find("T"),0,0,w,2);UiKit.Place((RectTransform)minimapFrame.Find("B"),0,h-2,w,2);UiKit.Place((RectTransform)minimapFrame.Find("L"),0,0,2,h);UiKit.Place((RectTransform)minimapFrame.Find("R"),w-2,0,2,h);
    }
    private void OnDestroy(){if(minimapTexture!=null)minimapTexture.Release();if(minimapCamera!=null)Destroy(minimapCamera.gameObject);}
}
