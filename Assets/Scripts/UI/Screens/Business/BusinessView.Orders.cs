using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>The order list on the dispatch board: every current order with its progress, the time until it is baked
/// and the delivery promise. Baked tickets are dragged from here (or from their pin) into a courier's lane.</summary>
public sealed partial class BusinessView
{
    [Header("Dispatch orders")]
    [SerializeField] private RectTransform dispatchOrderContent,readyCount;
    [SerializeField] private TextMeshProUGUI dispatchOrderTitle;
    [SerializeField] private DropZone ordersZone;
    private string dispatchOrdersSignature="";
    private readonly Dictionary<Order,float> cancelledNotices=new();
    private readonly HashSet<Order> observedOrders=new();
    private readonly List<(Order order,RectTransform card,TextMeshProUGUI detail,TextMeshProUGUI progress,TextMeshProUGUI countdown,RectTransform state,Image[] steps)> dispatchOrderRows=new();
    private ScoreManager deadlineScore;
    private const float RowHeight=132;
    public string OrderCountdown(Order order)
    {
        if(order.Status==OrderStatus.Cancelled)return "キャンセル：閉店または受付猶予終了";
        float left=order.Deadline-clock.ElapsedSeconds;
        if(left>0)return $"お届け目安まで {Mathf.CeilToInt(left)}秒";
        if(order.Status==OrderStatus.Dispatched)return $"{Mathf.FloorToInt(-left)}秒遅れ・減点で配達中";
        if(deadlineScore==null)deadlineScore=Find<ScoreManager>();
        return $"{Mathf.FloorToInt(-left)}秒遅れ ／ 受付あと {Mathf.Max(0,Mathf.CeilToInt(deadlineScore.CancellationAt(order)-clock.ElapsedSeconds))}秒";
    }
    private void BuildDispatchOrders()
    {
        var panel=UiKit.Card(dispatch,"Orders",32,212,344,540,T.paper);
        var glow=UiKit.Stretch(panel,"Glow",-8,-8,-8,-8);var g=UiKit.Fill(glow,T.cheese,24);g.enabled=false;glow.SetAsFirstSibling();
        var hit=panel.gameObject.AddComponent<Image>();hit.color=Color.clear;
        // A ticket dragged back here leaves its lane (before departure nothing is paid).
        ordersZone=panel.gameObject.AddComponent<DropZone>();ordersZone.Glow=g;
        dispatchOrderTitle=UiKit.Label(panel,"Title","注文",18,10,90,44,T.subheadingSize,T.line,FontRole.Display);
        readyCount=UiKit.Badge(panel,"ReadyCount","焼き上がり 0",100,18,T.basil,Color.white,30,16);
        var viewport=UiKit.Box(panel,"Viewport",8,62,328,428);
        viewport.gameObject.AddComponent<RectMask2D>();
        var back=viewport.gameObject.AddComponent<Image>();back.color=Color.clear;
        dispatchOrderContent=UiKit.Box(viewport,"Content",0,0,328,428);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();
        scroll.content=dispatchOrderContent;
        scroll.viewport=viewport;
        scroll.horizontal=false;
        scroll.vertical=true;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=T.largeSpace;
        UiKit.Label(panel,"Hint","伝票を、下の配達員のレーンへドラッグ",18,496,308,32,T.captionSize,T.inkSub,FontRole.Black);
    }
    private void BindDispatchOrders(){ordersZone.Accepts=p=>p!=null && p.Kind==DragKind.Bagged;ordersZone.Dropped=p=>handover.Unbag(p.Order);}
    private void RefreshDispatchOrders()
    {
        foreach(var order in observedOrders.Where(o=>o.Status==OrderStatus.Cancelled))
            if(!cancelledNotices.ContainsKey(order))cancelledNotices[order]=Time.unscaledTime+T.cancelledOrderNoticeSeconds;
        foreach(var order in cancelledNotices.Where(p=>Time.unscaledTime>p.Value).Select(p=>p.Key).ToArray())
        {cancelledNotices.Remove(order);observedOrders.Remove(order);}
        foreach(var order in orders.Orders)observedOrders.Add(order);
        var travelling=flow.ActiveTrips.SelectMany(t=>t.Deliveries).Where(d=>!d.Resolved).Select(d=>d.Order);
        var shown=orders.Orders.Concat(travelling).Concat(cancelledNotices.Keys).Distinct().OrderBy(o=>o.Deadline).ToList();
        UiKit.SetBadge(readyCount,$"焼き上がり {handover.RackOrders.Count()}",T.basil,Color.white);
        string sig=string.Join(",",shown.Select(o=>o.Id));
        if(sig!=dispatchOrdersSignature)
        {
            dispatchOrdersSignature=sig;dispatchOrderRows.Clear();
            for(int i=dispatchOrderContent.childCount-1;i>=0;i--)Destroy(dispatchOrderContent.GetChild(i).gameObject);
            for(int i=0;i<shown.Count;i++)
            {
                var o=shown[i];
                var card=UiKit.Card(dispatchOrderContent,"Order_"+Number(o),4,i*RowHeight+4,316,RowHeight-10,T.paperDeep,12,3,2);
                UiKit.Label(card,"Title",$"#{Number(o)}  {ShopStrings.House(map,o.DestinationNodeId)}",12,4,180,32,T.bodySize,T.line,FontRole.Black);
                var state=UiKit.Badge(card,"State","",0,8,T.disabled,T.line,26,T.captionSize);
                var detail=UiKit.Label(card,"Detail","",12,36,292,24,T.captionSize,T.line,FontRole.Black);
                detail.textWrappingMode=TextWrappingModes.NoWrap;detail.overflowMode=TextOverflowModes.Ellipsis;
                var progress=UiKit.Label(card,"Progress","",12,60,292,24,T.captionSize,T.info,FontRole.Black);
                progress.enableAutoSizing=true;progress.fontSizeMin=12;progress.textWrappingMode=TextWrappingModes.NoWrap;progress.overflowMode=TextOverflowModes.Ellipsis;
                var countdown=UiKit.Label(card,"Countdown","",12,84,292,30,T.captionSize,T.inkSub,FontRole.Bold);
                countdown.enableAutoSizing=true;countdown.fontSizeMin=12;countdown.overflowMode=TextOverflowModes.Ellipsis;
                var steps=new Image[5];
                for(int k=0;k<5;k++)steps[k]=UiKit.Fill(UiKit.Box(card,"Step"+k,12+k*59,116,55,5),T.disabled,2.5f);
                var hit=card.gameObject.AddComponent<Image>();hit.color=Color.clear;
                var source=card.gameObject.AddComponent<DragSource>();
                source.Payload=()=>o.Status==OrderStatus.Pending && o.IsReady?new DragPayload{Kind=handover.IsBagged(o)?DragKind.Bagged:DragKind.Box,Order=o,Sprite="box_closed",Size=new Vector2(96,96)}:null;
                source.Clicked=()=>TapOrder(o);
                dispatchOrderRows.Add((o,card,detail,progress,countdown,state,steps));
            }
            dispatchOrderContent.sizeDelta=new Vector2(328,Mathf.Max(428,shown.Count*RowHeight+8));
            if(shown.Count==0)UiKit.Label(dispatchOrderContent,"Empty","注文を待っています。\n調理中の注文もここに表示します。",16,16,296,100,T.bodySize,T.inkSub,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        }
        foreach(var (o,card,detail,progress,countdown,state,steps) in dispatchOrderRows)
        {
            var (text,fill,ink)=OrderState(o);UiKit.SetBadge(state,text,fill,ink);state.anchoredPosition=new Vector2(316-10-state.sizeDelta.x,-8);
            detail.text=o.PizzaName+" ["+o.Ingredients+"]";
            progress.text=OrderProgress(o);
            string time=OrderCountdown(o);
            if(o.Status==OrderStatus.Dispatched)
            {
                var delivery=flow.ActiveTrips.SelectMany(t=>t.Deliveries).FirstOrDefault(d=>d.Order==o);
                if(delivery!=null)time=$"到着まで {Mathf.Max(0,Mathf.CeilToInt(delivery.ArrivesAt-clock.ElapsedSeconds))}秒 ・ "+time;
            }
            countdown.text=time;countdown.color=o.Deadline<=clock.ElapsedSeconds && o.Status!=OrderStatus.Delivered?T.berry:T.inkSub;
            bool mine=handover.IsBagged(o) && handover.Holder(o)==roster.SelectedPerson;
            UiKit.FaceOf(card).color=o.Status==OrderStatus.Cancelled?T.berryLight:mine?T.cheeseLight:o.IsReady && o.Status==OrderStatus.Pending?T.paper:T.paperDeep;
            int stage=OrderStage(o);
            for(int k=0;k<5;k++)steps[k].color=k<stage-1?T.basil:k==stage-1?(stage>=5?T.info:T.cheese):new Color(T.disabled.r,T.disabled.g,T.disabled.b,.5f);
        }
    }
    /// <summary>What happens to the order now, with the time until it is baked when the kitchen can estimate it.</summary>
    public string OrderProgress(Order o)
    {
        if(o.Status==OrderStatus.Cancelled)return "キャンセル";
        if(o.Status==OrderStatus.Dispatched)return "配達中";
        if(o.Status==OrderStatus.Delivered)return "お届け済み";
        if(handover.IsBagged(o))return handover.Holder(o).Name+" のレーン";
        if(o.IsReady)return "焼き上がり済み";
        var eta=cooking.ReadyIn(o);var job=cooking.JobFor(o);
        if(eta.HasValue)return $"焼き上がり 約{Mathf.Max(1,Mathf.CeilToInt(eta.Value))}秒";
        if(job!=null && job.Worker==PizzaWorker.Player)return "あなたが調理中";
        return crew.People.Count>0?"調理待ち":"作り手なし（自分で作る）";
    }
    /// <summary>1 調理, 2 焼き, 3 梱包済み, 4 積載, 5 配達中 (0 = waiting for a cook).</summary>
    public int OrderStage(Order o)
    {
        if(o.Status==OrderStatus.Dispatched || o.Status==OrderStatus.Delivered)return 5;if(o.Status==OrderStatus.Cancelled)return 0;
        if(handover.IsBagged(o))return 4;if(o.IsReady)return 3;
        var job=cooking.JobFor(o);if(job==null)return 0;return job.Stage>=PizzaStage.Baking?2:1;
    }
    private (string text,Color fill,Color ink) OrderState(Order o)
    {
        if(o.Status==OrderStatus.Cancelled)return ("キャンセル",T.berry,Color.white);
        if(o.Status==OrderStatus.Dispatched)return ("配達中",T.info,Color.white);
        float left=o.Deadline-clock.ElapsedSeconds;
        if(left<=0)return ("遅れ",T.berry,Color.white);
        if(left/Mathf.Max(.01f,o.TimeLimit)<=T.deadlineUrgent)return ("もうすぐ期限",T.berry,Color.white);
        if(handover.IsBagged(o)){var holder=handover.Holder(o);return (holder.Name,T.CourierColor(holder.Name),Color.white);}
        if(o.IsReady)return ("焼けた",T.basil,Color.white);
        var job=cooking.JobFor(o);
        if(job!=null && job.NeedsDecision)return (ShopStrings.FailureWaiting,T.berry,Color.white);
        if(job==null && cooking.IsRedo(o))return (ShopStrings.Redo,T.cheese,T.line);
        if(job==null)return ("調理待ち",T.disabled,T.line);
        if(job.QueuedForOven)return (cooking.StageCaption(o),T.cheese,T.line);
        if(job.Stage>=PizzaStage.Baking)return ("焼いている",T.cheese,T.line);
        return ("調理中",job.Worker==PizzaWorker.Player?T.tomato:T.info,Color.white);
    }
}
