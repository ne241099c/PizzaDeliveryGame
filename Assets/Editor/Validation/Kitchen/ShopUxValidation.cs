using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object=UnityEngine.Object;

/// <summary>Focused operations for scroll input, bag controls, route picking, queue display and state pins.</summary>
public static class ShopUxValidation
{
    public static string RunReservations()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        CampaignSaveManager.SessionSavingLocked=true;
        var save=Find<CampaignSaveManager>();using var suppress=save.SuppressSaving();
        using var sample=new SampleMapValidationScope();
        var campaign=Find<CampaignManager>();var clock=Find<GameClock>();var roster=Find<CourierRoster>();
        var orders=Find<OrderBook>();var hand=Find<HandoverManager>();var cook=Find<CookingManager>();
        var business=Find<BusinessView>();var flow=Find<DeliveryFlowController>();var planner=Find<DeliveryPlanner>();
        int count=0;void Check(bool ok,string label){if(!ok)throw new Exception("Reservation UX: "+label);count++;}
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;Find<OrderSpawner>().enabled=false;
            Find<KitchenCrew>().Restore(Array.Empty<KitchenStaff>());campaign.StartBusiness();
            business.ShowZone(ShopZone.Dispatch,true);
            var p=roster.People[0];Check(p.Capacity==3,"Aoi starts with three slots");
            var raw=new Order("Goal1","raw",0,150);
            var ready=new Order("Goal2","ready",0,150,BakingState.Ready);
            var third=new Order("Goal1","third",0,150,BakingState.Ready);
            orders.AddOrder(raw);orders.AddOrder(ready);orders.AddOrder(third);
            Check(hand.Bag(raw,p) && hand.Bag(ready,p) && hand.Bag(third,p),"reserve three including unbaked order");
            Check(orders.SelectedOrders.SequenceEqual(hand.BagOf(p)),"reserved orders selected for planning");
            Check(!hand.ReadyToDepart(p),"mixed bag is not ready to depart");
            business.RefreshPresentation();
            Check(planner.SelectedPlan!=null,"route can be planned before baking");
            Check(flow.DispatchRejectionReason(planner.SelectedPlan,p).Contains("cooking"),"flow refuses raw pizza");
            var depart=business.GetComponentsInChildren<StickerButton>().First(b=>b.name=="Depart_"+p.Name);
            Check(!depart.Interactable && depart.Text==UiStrings.DepartureWaiting,"departure button waits for baking");
            Check(business.OrderProgress(raw).Contains("予約"),"order shows reservation and kitchen progress");
            Check(business.OrderStage(raw)==0,"reservation does not fake cooking completion");
            Check(business.BeginRouteEdit(p) && clock.IsPlanning,"route edit enables slow time");
            float before=clock.ElapsedSeconds;clock.AdvanceAutomatically(10);
            Check(Mathf.Abs(clock.ElapsedSeconds-before-10*clock.PlanningTimeRate)<.001f,"planning slows the shared simulation clock");
            Check(raw.Deadline==150,"planning does not rewrite deadlines");
            business.ShowZone(ShopZone.Kitchen,true);
            Check(!clock.IsPlanning && !business.IsEditingRoute,"leaving dispatch ends slow time");
            Check(cook.Claim(raw,0) && cook.AddDough(0) && cook.AddSauce(0,1) && cook.AddCheese(0) && cook.AddTopping(0,Topping.Basil) && cook.PutInOven(0,0),"reserved pizza can still be cooked");
            clock.Advance(cook.BakeDuration(raw));Check(cook.TakeOut(0),"reserved pizza leaves oven");
            var job=cook.JobFor(raw);if(!raw.IsReady){cook.Box(job);cook.CloseBox(job);}
            Check(raw.IsReady && hand.ReadyToDepart(p),"baked reservation automatically becomes ready");
            business.ShowZone(ShopZone.Dispatch,true);business.RefreshPresentation();
            depart=business.GetComponentsInChildren<StickerButton>().Last(b=>b.name=="Depart_"+p.Name);
            Check(depart.Interactable,"departure button automatically enables");
            Check(business.SwapDeliveryOrder(p) && hand.BagOf(p).SequenceEqual(new[]{ready,third,raw}),"three-stop lane button rotates all orders");
            Check(hand.Unbag(third) && !hand.IsBagged(third),"reservation can be removed without dispatch");
            Check(hand.Bag(third,p),"released pizza can be reserved again");
            business.SyncRoute();Check(flow.CanDispatch(planner.SelectedPlan,p),"all three ready pizzas can depart");
            business.Toast("通知配置の確認",UITheme.Active.info);business.RefreshPresentation();
            var toastRoot=(RectTransform)new SerializedObject(business).FindProperty("toasts").objectReferenceValue;
            Check(-toastRoot.anchoredPosition.y+toastRoot.rect.height<=250,"notification is above map content");
            business.Depart(p);Check(p.CurrentTrip!=null && p.CurrentTrip.Deliveries.Count==3,"three orders really dispatch");
            Check(hand.BagOf(p).Count==0,"departure releases reservation lane");
            return $"PASS: {count} reservation / planning UX checks.";
        }
        finally{business.EndRouteEdit();clock.AutomaticTick=false;CampaignSaveManager.SessionSavingLocked=true;}
    }
    private static T Find<T>() where T:Object=>Object.FindFirstObjectByType<T>();
    public static string Run()
    {
        if(!Application.isPlaying)throw new InvalidOperationException("Play required.");
        CampaignSaveManager.SessionSavingLocked=true;
        var save=Find<CampaignSaveManager>();using var suppress=save.SuppressSaving();
        using var sample=new SampleMapValidationScope();
        var clock=Find<GameClock>();var spawner=Find<OrderSpawner>();var campaign=Find<CampaignManager>();
        var roster=Find<CourierRoster>();var orders=Find<OrderBook>();var hand=Find<HandoverManager>();var cook=Find<CookingManager>();
        var business=Find<BusinessView>();var map=Find<MapManager>();var crew=Find<KitchenCrew>();
        bool tick=clock.AutomaticTick,spawn=spawner.enabled;int count=0;
        void Check(bool ok,string label){if(!ok)throw new Exception("Shop UX: "+label);count++;}
        try
        {
            campaign.NewGame();clock.AutomaticTick=false;spawner.enabled=false;crew.Restore(Array.Empty<KitchenStaff>());
            for(int i=0;i<6;i++)roster.AddPerson(new DeliveryPerson("Ux"+i,.6f,.6f,2,.5f,.8f,.9f));
            campaign.StartBusiness();business.ShowZone(ShopZone.Dispatch,true);
            var a=new Order("Goal1","one",0,150,BakingState.Ready);var b=new Order("Goal2","two",0,150,BakingState.Ready);
            orders.AddOrder(a);orders.AddOrder(b);var person=roster.People[0];hand.Bag(a,person);hand.Bag(b,person);
            business.RefreshPresentation();Canvas.ForceUpdateCanvases();
            var so=new SerializedObject(business);var lanes=(RectTransform)so.FindProperty("laneRow").objectReferenceValue;
            var scroll=lanes.GetComponentInParent<ScrollRect>();
            Check(scroll.viewport!=null && scroll.viewport.GetComponent<Graphic>().raycastTarget,"lanes receive input over empty background");
            Check(scroll.horizontalScrollbar!=null && lanes.rect.width>scroll.viewport.rect.width,"many couriers have a draggable scrollbar");
            ExecuteEvents.Execute(scroll.gameObject,new PointerEventData(EventSystem.current){scrollDelta=new Vector2(0,-1)},ExecuteEvents.scrollHandler);Canvas.ForceUpdateCanvases();
            Check(scroll.horizontalNormalizedPosition>0,"ordinary mouse wheel moves horizontal lanes");
            Check(business.SwapDeliveryOrder(person) && hand.BagOf(person).SequenceEqual(new[]{b,a}),"lane button reverses two delivery stops");
            Check(business.BeginRouteEdit(person),"route editing starts with selected courier");
            var road=map.Edges.First(e=>e.RoadType==RoadType.Backstreet);
            Vector2 tap=Camera.main.WorldToScreenPoint(RoadGeometry.PointAt(RoadGeometry.Points(map,road),.5f));
            business.Tap(tap);business.SyncRoute();
            Check(Find<DeliveryPlanner>().Route.EdgeIds.Contains(road.Id),"clicking the separate backstreet changes the selected leg");
            business.RefreshPresentation();
            var bagClick=lanes.GetComponentsInChildren<DragSource>().First(s=>s.name=="Bag_"+business.Number(b));bagClick.Clicked();
            Check(!hand.IsBagged(b) && hand.RackOrders.Contains(b),"clicking carried box puts it down");
            business.RefreshPresentation();
            Check(business.PinColor(a)==UITheme.Active.orderReadyPin,"ready pin uses green");
            var raw=new Order("Goal2","raw",0,150);orders.AddOrder(raw);
            Check(business.PinColor(raw)==UITheme.Active.orderWaitingPin,"unstarted pin uses waiting color");
            Check(cook.Claim(raw,0) && cook.AddDough(0),"player can start the waiting ticket");
            Check(business.PinColor(raw)==UITheme.Active.orderMakingPin,"assembly pin uses making color");
            Check(cook.AddSauce(0,1) && cook.AddCheese(0) && cook.AddTopping(0,Topping.Basil) && cook.PutInOven(0,0),"pizza goes into oven");
            Check(business.PinColor(raw)==UITheme.Active.orderBakingPin,"baking pin has a separate color");
            var queued=new Order("Goal1","queue",0,150);orders.AddOrder(queued);
            Check(cook.Claim(queued,0) && cook.AddDough(0) && cook.AddSauce(0,1) && cook.AddCheese(0) && cook.AddTopping(0,Topping.Basil) && cook.PutInOven(0,0),"second pizza joins occupied oven queue");
            business.RefreshPresentation();
            var queue=(RectTransform)new SerializedObject(business).FindProperty("ovenQueueContent").objectReferenceValue;
            Check(queue.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.text=="#"+business.Number(queued)),"queue displays waiting order number");
            Check(queue.GetComponentsInChildren<Image>().Any(i=>i.name=="Dough" && i.enabled),"queue displays waiting pizza");
            Check(cook.WasteCost==0,"discard fee is zero in current scene");
            var detail=business.GetComponentsInChildren<TextMeshProUGUI>().Where(t=>t.name=="Detail").ToArray();
            Check(detail.Length>0 && detail.All(t=>t.overflowMode==TextOverflowModes.Ellipsis) && business.GetComponentsInChildren<TextMeshProUGUI>().Any(t=>t.name=="Progress" && t.text.Length>0),"bake ETA wraps inside order card");
            business.Depart(person);business.RefreshPresentation();
            Check(person.CurrentTrip!=null,"lane departure keeps working after route edit");
            clock.Advance(1);business.RefreshPresentation();
            var lane=lanes.GetComponentsInChildren<UiBar>().Last(bar=>bar.name=="ReturnProgress" && bar.transform.parent.name=="Lane_"+person.Name);
            var trip=person.CurrentTrip;
            Check(trip!=null && Mathf.Abs(lane.Value-Mathf.InverseLerp(trip.StartedAt,trip.ReturnsAt,clock.ElapsedSeconds))<.001f,"reload gauge tracks actual return schedule");
            return $"PASS: {count} shop UX checks.";
        }
        finally
        {
            save.StartNewGame();clock.AutomaticTick=tick;spawner.enabled=spawn;
            CampaignSaveManager.SessionSavingLocked=true;
        }
    }
}
