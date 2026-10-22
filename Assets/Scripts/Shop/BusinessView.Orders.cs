using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed partial class BusinessView
{
    private RectTransform dispatchOrderContent;
    private TextMeshProUGUI dispatchOrderTitle;
    private string dispatchOrdersSignature="";
    private readonly Dictionary<Order,float> cancelledNotices=new();
    private readonly HashSet<Order> observedOrders=new();
    private readonly List<(Order order,TextMeshProUGUI countdown,TextMeshProUGUI state)> dispatchOrderRows=new();
    private ScoreManager deadlineScore;
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
        var panel=UiKit.Card(dispatch,"Orders",32,370,370,556,T.paper);
        dispatchOrderTitle=UiKit.Label(panel,"Title","現在の注文",16,12,338,34,T.subheadingSize,T.line,FontRole.Black);
        UiKit.Label(panel,"Hint","調理 → 焼き → 梱包 → 積載 → 配達",16,48,338,30,T.captionSize,T.inkSub,FontRole.Black);
        var viewport=UiKit.Box(panel,"Viewport",10,88,350,450);
        viewport.gameObject.AddComponent<RectMask2D>();
        var hit=viewport.gameObject.AddComponent<Image>();hit.color=Color.clear;
        dispatchOrderContent=UiKit.Box(viewport,"Content",0,0,350,450);
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=dispatchOrderContent;scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=T.largeSpace;
    }
    private void RefreshDispatchOrders()
    {
        foreach(var order in observedOrders.Where(o=>o.Status==OrderStatus.Cancelled))
            if(!cancelledNotices.ContainsKey(order))cancelledNotices[order]=Time.unscaledTime+T.cancelledOrderNoticeSeconds;
        foreach(var order in cancelledNotices.Where(p=>Time.unscaledTime>p.Value).Select(p=>p.Key).ToArray())
        {cancelledNotices.Remove(order);observedOrders.Remove(order);}
        foreach(var order in orders.Orders)observedOrders.Add(order);
        var travelling=flow.ActiveTrips.SelectMany(t=>t.Deliveries).Where(d=>!d.Resolved).Select(d=>d.Order);
        var shown=orders.Orders.Concat(travelling).Concat(cancelledNotices.Keys).Distinct().OrderBy(o=>o.Deadline).ToList();
        var person=roster.SelectedPerson;
        string sig=UiKit.Ref(person)+(person!=null?handover.BagOf(person).Count.ToString():"")+"|"+string.Join(",",shown.Select(o=>o.Id+o.Status+o.Baking+cooking.JobFor(o)?.Stage+cooking.JobFor(o)?.Worker+handover.Holder(o)?.Id+(planner.Route?.HasVisited(o.DestinationNodeId)??false)));
        dispatchOrderTitle.text=$"現在の注文 {shown.Count}件";
        if(sig!=dispatchOrdersSignature)
        {
            dispatchOrdersSignature=sig;dispatchOrderRows.Clear();
            for(int i=dispatchOrderContent.childCount-1;i>=0;i--)Destroy(dispatchOrderContent.GetChild(i).gameObject);
            // Compact rows: every order shows its 5-stage progress and time; buttons appear only once it is packed.
            float y=0;
            for(int i=0;i<shown.Count;i++)
            {
                var o=shown[i];bool own=person!=null && handover.Holder(o)==person;
                bool actionable=o.Status==OrderStatus.Pending && o.IsReady;float h=actionable?128:84;
                var bg=own?T.cheeseLight:o.Status==OrderStatus.Cancelled?T.berryLight:T.paperDeep;
                var card=UiKit.Card(dispatchOrderContent,"Order_"+Number(o),4,y+4,338,h-8,bg,12,2,2);
                UiKit.Label(card,"Title",$"#{Number(o)} → {ShopStrings.House(map,o.DestinationNodeId)}",12,4,200,30,T.bodySize,T.line,FontRole.Black);
                var (state,color,ink)=OrderState(o,person);
                var badge=UiKit.Badge(card,"State",state,0,8,color,ink,28,T.captionSize);badge.anchoredPosition=new Vector2(338-10-badge.sizeDelta.x,-8);
                int stage=OrderStage(o);
                for(int k=0;k<5;k++)
                {
                    var seg=UiKit.Box(card,"Step"+k,12+k*63,40,58,10);
                    UiKit.Fill(seg,k<stage-1?T.basil:k==stage-1?(stage>=5?T.info:T.cheese):new Color(T.disabled.r,T.disabled.g,T.disabled.b,.5f),5);
                }
                var countdown=UiKit.Label(card,"Countdown","",12,54,314,24,T.captionSize,T.line,FontRole.Black);
                dispatchOrderRows.Add((o,countdown,badge.Find("Text").GetComponent<TextMeshProUGUI>()));
                if(actionable)
                {
                    var load=UiKit.Button(card,"Load_"+Number(o),own?"外す":"積む",own?StickerKind.Secondary:StickerKind.Primary,12,80,140,38,T.bodySize,()=>ToggleDispatchOrder(o));
                    load.Interactable=person!=null && person.IsAvailable && (own || handover.BagRejection(o,person).Length==0);
                    bool visited=planner.Route!=null && planner.Route.HasVisited(o.DestinationNodeId);
                    var visit=UiKit.Button(card,"Visit_"+Number(o),visited && own?"道順に追加済":"届け先へ",StickerKind.Secondary,164,80,162,38,T.bodySize,()=>TapPin(o.DestinationNodeId));
                    visit.Interactable=own && !visited;
                }
                y+=h;
            }
            dispatchOrderContent.sizeDelta=new Vector2(350,Mathf.Max(450,y+8));
            if(shown.Count==0)UiKit.Label(dispatchOrderContent,"Empty","注文を待っています。\n調理中の注文もここに表示します。",16,16,318,100,T.bodySize,T.inkSub,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        }
        foreach(var row in dispatchOrderRows)
        {
            string text=OrderCountdown(row.order);
            if(row.order.Status==OrderStatus.Dispatched){var delivery=flow.ActiveTrips.SelectMany(t=>t.Deliveries).FirstOrDefault(d=>d.Order==row.order);if(delivery!=null)text=$"到着まで {Mathf.Max(0,Mathf.CeilToInt(delivery.ArrivesAt-clock.ElapsedSeconds))}秒 ・ "+text;}
            row.countdown.text=text;row.countdown.color=row.order.Deadline<=clock.ElapsedSeconds?T.berry:T.line;
        }
    }
    /// <summary>1 調理, 2 焼き, 3 梱包済み, 4 積載, 5 配達中 (0 = waiting for a cook).</summary>
    public int OrderStage(Order o)
    {
        if(o.Status==OrderStatus.Dispatched || o.Status==OrderStatus.Delivered)return 5;if(o.Status==OrderStatus.Cancelled)return 0;
        if(handover.IsBagged(o))return 4;if(o.IsReady)return 3;
        var job=cooking.JobFor(o);if(job==null)return 0;return job.Stage>=PizzaStage.Baking?2:1;
    }
    private (string text,Color fill,Color ink) OrderState(Order o,DeliveryPerson person)
    {
        if(o.Status==OrderStatus.Cancelled)return ("キャンセル",T.berry,Color.white);
        if(o.Status==OrderStatus.Dispatched)return ("配達中",T.info,Color.white);
        if(handover.IsBagged(o))return (handover.Holder(o).Name+" に積載",T.cheese,T.line);
        if(o.IsReady)return (person!=null && handover.BagOf(person).Count>=person.Capacity?"梱包済・バッグ満杯":"梱包済み",T.basil,Color.white);
        var job=cooking.JobFor(o);
        if(job==null)return ("調理待ち",T.disabled,T.line);
        if(job.Stage>=PizzaStage.Baking)return ("焼いている",T.cheese,T.line);
        return (job.Worker==PizzaWorker.Player?"あなたが調理中":(job.Staff!=null?job.Staff.Name:"内勤")+" 調理中",job.Worker==PizzaWorker.Player?T.tomato:T.info,Color.white);
    }
    public bool ToggleDispatchOrder(Order order)
    {
        if(handover.Holder(order)==roster.SelectedPerson && handover.IsBagged(order))return handover.Unbag(order);
        bool ok=handover.Bag(order,roster.SelectedPerson);
        if(!ok)Toast(handover.LastMessage,T.berry);
        return ok;
    }
    public void UndoRoad()
    {
        var route=planner.Route;if(route==null || route.EdgeIds.Count==0)return;
        planner.ReplaceRoute(route.EdgeIds.Take(route.EdgeIds.Count-1).ToArray());
    }
    public void ReturnHome()
    {
        var route=planner.Route;var person=roster.SelectedPerson;if(route==null || person==null)return;
        var path=RoadPathfinder.FindShortestPath(map.Edges,route.CurrentNodeId,map.StoreNodeId,e=>map.GetTravelTime(e.Id,person));
        if(path!=null)planner.ReplaceRoute(route.EdgeIds.Concat(path).ToArray());
    }
}
