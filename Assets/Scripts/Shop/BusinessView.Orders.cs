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
        UiKit.Label(panel,"Hint","梱包済み → 積む → 届け先へ",16,48,338,30,T.captionSize,T.inkSub,FontRole.Black);
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
        string sig=(person?.Id)+"|"+string.Join(",",shown.Select(o=>o.Id+o.Status+o.Baking+handover.Holder(o)?.Id+(planner.Route?.HasVisited(o.DestinationNodeId)??false)));
        dispatchOrderTitle.text=$"現在の注文 {shown.Count}件  ↕スクロール";
        if(sig!=dispatchOrdersSignature)
        {
            dispatchOrdersSignature=sig;dispatchOrderRows.Clear();
            for(int i=dispatchOrderContent.childCount-1;i>=0;i--)Destroy(dispatchOrderContent.GetChild(i).gameObject);
            dispatchOrderContent.sizeDelta=new Vector2(350,Mathf.Max(450,shown.Count*166));
            for(int i=0;i<shown.Count;i++)
            {
                var o=shown[i];var card=UiKit.Card(dispatchOrderContent,"Order_"+Number(o),4,i*166+4,338,154,T.paperDeep,12,2,2);
                UiKit.Label(card,"Title",$"#{Number(o)} → {ShopStrings.House(map,o.DestinationNodeId)}",12,6,314,32,T.subheadingSize,T.line,FontRole.Black);
                string state=o.Status==OrderStatus.Cancelled?"キャンセル":o.Status==OrderStatus.Dispatched?"配達中":handover.IsBagged(o)?handover.Holder(o).Name+" に積載中":cooking.StageCaption(o);
                if(o.Status==OrderStatus.Pending && o.IsReady && !handover.IsBagged(o) && person!=null && handover.BagOf(person).Count>=person.Capacity)state="バッグ満杯・先に注文を外す";
                var stateText=UiKit.Label(card,"State",state,12,38,314,26,T.captionSize,T.inkSub,FontRole.Bold);
                var countdown=UiKit.Label(card,"Countdown","",12,68,314,26,T.captionSize,T.line,FontRole.Black);dispatchOrderRows.Add((o,countdown,stateText));
                bool own=person!=null && handover.Holder(o)==person;
                var load=UiKit.Button(card,"Load_"+Number(o),own?"外す":"積む",own?StickerKind.Secondary:StickerKind.Primary,12,104,140,40,T.bodySize,()=>ToggleDispatchOrder(o));
                load.Interactable=o.Status==OrderStatus.Pending && person!=null && person.IsAvailable && (own || handover.BagRejection(o,person).Length==0);
                bool visited=planner.Route!=null && planner.Route.HasVisited(o.DestinationNodeId);
                var visit=UiKit.Button(card,"Visit_"+Number(o),visited && own?"道順に追加済":"届け先へ",StickerKind.Secondary,164,104,162,40,T.bodySize,()=>TapPin(o.DestinationNodeId));
                visit.Interactable=own && !visited && o.Status==OrderStatus.Pending;
            }
            if(shown.Count==0)UiKit.Label(dispatchOrderContent,"Empty","注文を待っています。\n調理中の注文もここに表示します。",16,16,318,100,T.bodySize,T.inkSub,FontRole.Bold,TextAlignmentOptions.TopLeft,true);
        }
        foreach(var row in dispatchOrderRows)
        {
            row.countdown.text=OrderCountdown(row.order);
            if(row.order.Status==OrderStatus.Dispatched){var delivery=flow.ActiveTrips.SelectMany(t=>t.Deliveries).FirstOrDefault(d=>d.Order==row.order);if(delivery!=null)row.state.text=$"配達中・到着まで {Mathf.Max(0,Mathf.CeilToInt(delivery.ArrivesAt-clock.ElapsedSeconds))}秒";}
            row.countdown.color=row.order.Deadline<=clock.ElapsedSeconds?T.berry:T.line;
        }
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
