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
