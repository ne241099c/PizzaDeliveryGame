using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Consumes a fresh Play session, exercising the same button events as the player.</summary>
public static class M4PlayValidation
{
    [MenuItem("Tools/Pizza Delivery/Validate M4 Day Flow (consumes Play session)")]
    public static void RunFromMenu() => Debug.Log(Run());

    public static string Run()
    {
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var orders=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();
        var people=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var ui=UnityEngine.Object.FindFirstObjectByType<DeliveryPlannerUI>();
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();
        var spawner=UnityEngine.Object.FindFirstObjectByType<OrderSpawner>();
        var score=UnityEngine.Object.FindFirstObjectByType<ScoreManager>();
        if (!Application.isPlaying || clock.Phase!=DayPhase.Open || clock.ElapsedSeconds>100 || stats.DeliveredCount!=0 || flow.ActiveTrips.Count!=0)
            throw new InvalidOperationException("Run in a fresh Play session, before any dispatch and within 100 seconds.");
        int checks=0;
        void Check(bool value,string name) {if(!value) throw new Exception("M4 flow: "+name);checks++;}
        void Set(UnityEngine.Object target,string name,bool value)
        { var so=new SerializedObject(target);so.FindProperty(name).boolValue=value;so.ApplyModifiedPropertiesWithoutUndo(); }
        Transform Content(UnityEngine.Object target,string field)
            => (Transform)new SerializedObject(target).FindProperty(field).objectReferenceValue;
        void SelectOrder(int index) => Content(orders,"orderListContent").GetComponentsInChildren<Button>().ElementAt(index).onClick.Invoke();
        void SelectPerson(int index) => Content(people,"deliveryPersonListContent").GetComponentsInChildren<Button>().ElementAt(index).onClick.Invoke();
        Set(clock,"automaticTick",false);
        var dynamics=UnityEngine.Object.FindFirstObjectByType<DeliveryDynamics>();
        var dynamicsSo=new SerializedObject(dynamics);
        float accidentProbability=dynamicsSo.FindProperty("baseAccidentProbability").floatValue;
        dynamicsSo.FindProperty("baseAccidentProbability").floatValue=0;dynamicsSo.ApplyModifiedPropertiesWithoutUndo();
        Check(orders.OrderCount>0,"Automatic orders appeared");
        Check(orders.Orders.All(o=>Mathf.Abs(o.TimeLimit-60f)<0.001f),"Spawned deadlines snapshot promise");
        spawner.enabled=false;
        orders.RemoveOrders(orders.Orders.ToArray());
        float start=clock.ElapsedSeconds;
        var a=new Order("Goal1","Test A",start,100);
        var b=new Order("Goal2","Test B late",start,1);
        orders.AddOrder(a);orders.AddOrder(b);SelectOrder(0);SelectOrder(1);SelectPerson(0);
        foreach(string edge in new[]{"Store-N1","N1-N2-Main","N2-Goal1","Goal1-Goal2","N2-Goal2","N1-N2-Main","Store-N1"})
            GameObject.Find("NextRoad_"+edge).GetComponent<Button>().onClick.Invoke();
        Check(ui.SelectedPlan!=null && ui.IndividualReferenceTime>ui.SelectedPlan.TotalTime,"Manual batch beats individual reference");
        Check(!flow.CanDispatch(ui.SelectedPlan,people.SelectedPerson),"Minato overload blocked");
        Check(flow.DispatchRejectionReason(ui.SelectedPlan,people.SelectedPerson).Contains("can carry 1"),"Minato overload reason");
        GameObject.Find("DispatchRoute").GetComponent<Button>().onClick.Invoke();
        Check(flow.ActiveTrips.Count==0 && orders.OrderCount==2,"Rejected dispatch preserves waiting orders");
        SelectPerson(1);
        GameObject.Find("DispatchRoute").GetComponent<Button>().onClick.Invoke();
        Check(flow.ActiveTrips.Count==1 && a.Status==OrderStatus.Dispatched && orders.OrderCount==0,"Manual dispatch transfers orders");
        Check(orders.SelectedOrders.Count==0 && people.SelectedPerson==null && ui.Route.EdgeIds.Count==0,"Dispatch clears selection and next route");
        SelectPerson(1);Check(people.SelectedPerson==null,"Busy courier cannot be selected");
        var c=new Order("Goal1","Test C",start,100);orders.AddOrder(c);SelectOrder(0);SelectPerson(0);
        GameObject.Find("DispatchSuggestion").GetComponent<Button>().onClick.Invoke();
        var d=new Order("Goal2","Test D",start,100);orders.AddOrder(d);SelectOrder(0);SelectPerson(2);
        GameObject.Find("DispatchSuggestion").GetComponent<Button>().onClick.Invoke();
        Check(flow.ActiveTrips.Count==3 && people.People.Take(3).All(p=>!p.IsAvailable),"Three concurrent trips");
        Check(GameObject.Find("CourierMarkers").transform.childCount==3,"Three independent map markers");
        Check(stats.DeliveredCount==0 && score.Score==0,"No score at dispatch");
        var trips=flow.ActiveTrips.ToArray();
        float first=trips.Min(t=>t.Deliveries.Min(x=>x.ArrivesAt));
        clock.Advance(first-clock.ElapsedSeconds+0.01f);
        Check(stats.DeliveredCount>0 && flow.ActiveTrips.Count==3,"Arrival scores before return");
        clock.Advance(trips.Max(t=>t.LastDeliveryAt)-clock.ElapsedSeconds+0.01f);
        int expected=trips.Sum(t=>t.Deliveries.Sum(x=>score.ScoreArrival(x.ArrivesAt,x.Order.Deadline,t.ServiceAtDispatch)));
        Check(stats.DeliveredCount==4 && stats.LateCount==1 && stats.TotalScore==expected && score.Score==expected,"Each arrival scores once with correct late count");
        Check(score.Score>300,"On-time service tips awarded alongside late penalty");
        Check(people.People.Take(3).All(p=>p.Status==DeliveryStatus.Returning),"All returning after last arrivals");
        clock.Advance(trips.Max(t=>t.ReturnsAt)-clock.ElapsedSeconds+0.01f);
        Check(flow.ActiveTrips.Count==0 && people.People.All(p=>p.IsAvailable && p.CurrentTrip==null),"All couriers available at return");
        Check(people.People.Take(3).All(p=>p.CurrentStamina<1),"Completed trips consume stamina");
        float tiredTime=RoadPathfinder.Suggest(UnityEngine.Object.FindFirstObjectByType<MapManager>(),new[]{d},people.People[2]).TotalTime;
        float staminaBeforeRest=people.People[2].CurrentStamina;
        clock.Advance(10);
        Check(people.People[2].CurrentStamina>staminaBeforeRest,"Idle stamina recovers");
        Check(RoadPathfinder.Suggest(UnityEngine.Object.FindFirstObjectByType<MapManager>(),new[]{d},people.People[2]).TotalTime<tiredTime,"Rest speeds up the next trip");
        int total=score.Score;clock.Advance(1);Check(score.Score==total,"No duplicate scoring on later ticks");
        var reputation=UnityEngine.Object.FindFirstObjectByType<ReputationManager>();
        float reputationBeforeCancellation=reputation.Value;
        var expired=new Order("Goal1","Expired",clock.ElapsedSeconds-100,79.9f);
        orders.AddOrder(expired);SelectOrder(0);clock.Advance(0.01f);
        Check(expired.Status==OrderStatus.Cancelled && orders.OrderCount==0 && orders.SelectedOrders.Count==0 && stats.CancelledCount==1,"Pending zero-score order expires and clears selection");
        Check(Mathf.Abs(reputation.Value-(reputationBeforeCancellation-5))<0.001f,"Cancellation lowers actual reputation");
        clock.Advance(clock.BusinessDuration-1-clock.ElapsedSeconds);
        var last=new Order("Goal1","Last dispatch",clock.ElapsedSeconds,100);
        orders.AddOrder(last);SelectOrder(0);SelectPerson(0);ui.DispatchWithSuggestion();
        var tripAfterClose=flow.ActiveTrips.Single();
        var waiting=new Order("Goal2","Waiting at close",clock.ElapsedSeconds,100);orders.AddOrder(waiting);
        clock.Advance(2);
        Check(clock.Phase==DayPhase.Closed && flow.ActiveTrips.Count==1,"Closing waits for travelling courier");
        Check(waiting.Status==OrderStatus.Cancelled && stats.CancelledCount==2 && orders.OrderCount==0,"Closing cancels waiting orders");
        spawner.enabled=true;clock.Advance(1);Check(orders.OrderCount==0,"No orders after closing");
        clock.Advance(tripAfterClose.ReturnsAt-clock.ElapsedSeconds+0.01f);
        Check(clock.Phase==DayPhase.Ended && stats.DeliveredCount==5 && people.People.All(p=>p.IsAvailable),"Day ends only after final return");
        var result=(GameObject)new SerializedObject(ui).FindProperty("resultsPanel").objectReferenceValue;
        Check(result.activeSelf,"Results panel shown");
        Check(score.Score==stats.TotalScore,"Result and displayed score agree");
        var resultLabel=(TMPro.TextMeshProUGUI)new SerializedObject(ui).FindProperty("resultText").objectReferenceValue;
        Check(resultLabel.text.Contains($"Reputation: {reputation.Value:F1}"),"Actual result includes final reputation");
        Check(resultLabel.text.Contains("Stamina"),"Actual result includes remaining stamina");
        dynamicsSo.FindProperty("baseAccidentProbability").floatValue=accidentProbability;dynamicsSo.ApplyModifiedPropertiesWithoutUndo();
        return $"PASS: {checks} integrated Play checks. Delivered {stats.DeliveredCount}, late {stats.LateCount}, cancelled {stats.CancelledCount}, score {stats.TotalScore}. Use Play again to reset.";
    }
    public static string CheckReset()
    {
        var clock=UnityEngine.Object.FindFirstObjectByType<GameClock>();
        var people=UnityEngine.Object.FindFirstObjectByType<DeliveryPersonSelector>();
        var score=UnityEngine.Object.FindFirstObjectByType<ScoreManager>();
        var stats=UnityEngine.Object.FindFirstObjectByType<DayStats>();
        var reputation=UnityEngine.Object.FindFirstObjectByType<ReputationManager>();
        var orders=UnityEngine.Object.FindFirstObjectByType<OrderListUI>();
        var flow=UnityEngine.Object.FindFirstObjectByType<DeliveryFlowController>();
        int checks=0;
        void Check(bool ok,string name){if(!ok)throw new Exception("M4 replay: "+name);checks++;}
        Check(clock.Phase==DayPhase.Open && clock.ElapsedSeconds<10,"clock restarted");
        Check(score.Score==0 && stats.DeliveredCount==0 && stats.CancelledCount==0,"score and statistics reset");
        Check(reputation.Value==50,"reputation reset to fifty");
        Check(people.People.Count==4 && people.People.All(p=>p.IsAvailable && p.CurrentTrip==null && p.CurrentStamina==1),"roster, availability and stamina reset");
        Check(orders.OrderCount==0 && flow.ActiveTrips.Count==0,"orders and trips reset");
        Check(people.People[0].Capacity==1 && people.People[1].Capacity==2,"capacity preserved across replay");
        return $"PASS: {checks} replay reset checks.";
    }

}
