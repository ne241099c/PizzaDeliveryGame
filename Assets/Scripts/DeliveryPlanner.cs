using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>The route being planned for the selected orders and courier: the hand-drawn route, the optional suggestion,
/// the completed plan and the dispatch. Logic only; the dispatch board (BusinessView) draws and edits it.</summary>
[DefaultExecutionOrder(-200)]
public class DeliveryPlanner : MonoBehaviour
{
    [SerializeField] private OrderBook orders;
    [SerializeField] private CourierRoster people;
    [SerializeField] private MapManager map;
    [SerializeField] private GameClock clock;
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private DayStats stats;
    [SerializeField] private ReputationManager reputation;
    private ManualDeliveryRoute route;
    private RouteSuggestion suggestion;
    private float previewStamina=-1;
    private bool subscribed;
    public DeliveryPlan SelectedPlan {get;private set;}
    public ManualDeliveryRoute Route=>route;
    public float? IndividualReferenceTime {get;private set;}
    public RouteSuggestion Suggestion=>suggestion;
    /// <summary>What the player should do next, or why the plan cannot be dispatched (English; UiStrings translates).</summary>
    public string Status {get;private set;}="";

    private void Start()
    {
        orders.SelectionChanged+=OnOrdersChanged;
        orders.OrdersChanged+=RefreshRoute;
        people.SelectionChanged+=OnPersonChanged;
        map.MapReady+=OnMapReady;
        map.EdgeClicked+=AppendRoad;
        clock.OnClosed+=OnClosed;
        subscribed=true;
        if(map.IsReady)OnMapReady();
    }
    private void OnDestroy()
    {
        if(!subscribed)return;
        orders.SelectionChanged-=OnOrdersChanged;
        orders.OrdersChanged-=RefreshRoute;
        people.SelectionChanged-=OnPersonChanged;
        map.MapReady-=OnMapReady;
        map.EdgeClicked-=AppendRoad;
        clock.OnClosed-=OnClosed;
    }
    private void OnMapReady()
    {
        route=new ManualDeliveryRoute(map);
        FitMapCamera();
        UpdateSuggestion();
        RefreshRoute();
    }
    private void OnOrdersChanged()
    {
        route?.Clear();
        UpdateSuggestion();
        RefreshRoute();
    }
    private void OnPersonChanged()
    {
        previewStamina=people.SelectedPerson?.CurrentStamina??-1;
        // Keep the drawn path, and recalculate its time using the new courier's skill.
        UpdateSuggestion();
        RefreshRoute();
    }
    private void UpdateSuggestion()
    {
        suggestion=RoadPathfinder.Suggest(map,orders.SelectedOrders,people.SelectedPerson);
        IndividualReferenceTime=RoadPathfinder.GetIndividualReferenceTime(map,orders.SelectedOrders,people.SelectedPerson);
    }
    private void Update()
    {
        if(people.SelectedPerson!=null && Mathf.Abs(people.SelectedPerson.CurrentStamina-previewStamina)>0.005f)OnPersonChanged();
        clock.IsPlanning=clock.Phase==DayPhase.Open && orders.SelectedOrders.Count>0 && people.SelectedPerson!=null;
    }
    public void PrepareDay()
    {
        route?.Clear();
        UpdateSuggestion();
        RefreshRoute();
    }
    private void OnClosed(){route?.Clear();RefreshRoute();}

    public string ResultSummary=>$"DAY COMPLETE\n\nScore: {stats.TotalScore}\nDelivered: {stats.DeliveredCount}"
        +$"\nLate: {stats.LateCount}\nCancelled: {stats.CancelledCount}\nReputation: {reputation.Value:F1}\n\nCouriers\n"
        +string.Join("\n",people.People.Select(p=>$"{p.Name} / Load {p.Capacity} / Stamina {p.CurrentStamina:P0}"));

    /// <summary>Replace the drawn route (e.g. built by dragging the route on the map).</summary>
    public bool ReplaceRoute(IEnumerable<string> edges)
    {
        if(route==null || clock.Phase!=DayPhase.Open)return false;
        bool ok=route.TryReplace(edges);
        RefreshRoute();
        return ok;
    }
    public void Dispatch()
    {
        UpdateSuggestion();
        RefreshRoute();
        DeliveryPlan plan=SelectedPlan;
        if(!flow.Dispatch(plan,people.SelectedPerson))return;
        Status=$"Dispatched / estimated return in {clock.ToRealSeconds(plan.TotalTime):F1}s.";
    }
    public void DispatchWithSuggestion()
    {
        if(clock.Phase!=DayPhase.Open || suggestion==null)return;
        ApplySuggestion();
        Dispatch();
    }
    public void AppendRoad(string edgeId)
    {
        if(route==null || clock.Phase!=DayPhase.Open || orders.SelectedOrders.Count==0 || people.SelectedPerson==null)
        {
            Status="Select orders and a courier first.";
            return;
        }
        if(!route.TryAppend(edgeId,out string message)){Status=message;return;}
        RefreshRoute();
    }
    /// <summary>Frames the whole town in the map camera.</summary>
    public void FitMapCamera()
    {
        Camera camera=Camera.main;
        if(camera==null || map==null || !map.IsReady || map.Nodes.Count==0)return;
        float minX=map.Nodes.Min(n=>n.Position.x),maxX=map.Nodes.Max(n=>n.Position.x);
        float minY=map.Nodes.Min(n=>n.Position.y),maxY=map.Nodes.Max(n=>n.Position.y);
        camera.transform.position=new Vector3((minX+maxX)/2f,(minY+maxY)/2f-0.3f,-10);
        camera.orthographicSize=Mathf.Max(1f,(maxY-minY+2f)/2f,(maxX-minX+2f)/(2f*camera.aspect));
    }
    public void UndoRoad(){route?.Undo();RefreshRoute();}
    public void ClearRoute(){route?.Clear();RefreshRoute();}
    public void ApplySuggestion()
    {
        if(route==null || suggestion==null || clock.Phase!=DayPhase.Open)return;
        route.TryReplace(suggestion.EdgeIds);
        RefreshRoute();
    }
    private void RefreshRoute()
    {
        SelectedPlan=null;
        if(route==null)return;
        float skill=people.SelectedPerson?.SkillLevel??1f;
        map.PreviewRoute(route.EdgeIds,route.CurrentNodeId,skill);
        SelectedPlan=route.BuildCompletion(orders.SelectedOrders,people.SelectedPerson);
        Status=StatusFor(orders.SelectedOrders.Count(o=>route.HasVisited(o.DestinationNodeId)));
    }
    private string StatusFor(int visited)
    {
        bool canDraw=clock.Phase==DayPhase.Open && orders.SelectedOrders.Count>0 && people.SelectedPerson!=null && people.SelectedPerson.IsAvailable;
        if(clock.Phase!=DayPhase.Open)return "Closed. Couriers will finish their trips before the result is shown.";
        if(orders.OrderCount==0)return "Waiting for orders. New orders appear automatically during opening.";
        if(people.SelectedPerson!=null && orders.SelectedOrders.Count>people.SelectedPerson.Capacity)return flow.DispatchRejectionReason(SelectedPlan,people.SelectedPerson);
        if(!canDraw)return "Select orders and an available courier, then click a road on the map.";
        if(SelectedPlan!=null)return "Route ready! Dispatch, or keep editing. The clock is still running.";
        if(visited<orders.SelectedOrders.Count)
            return "Visit: "+string.Join(", ",orders.SelectedOrders.Where(o=>!route.HasVisited(o.DestinationNodeId)).Select(o=>o.DestinationNodeId))+".";
        return "All destinations visited. Draw the return route to "+map.StoreNodeId+".";
    }
}
