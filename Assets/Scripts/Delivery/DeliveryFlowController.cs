using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeliveryFlowController : MonoBehaviour
{
    [SerializeField] private CookingManager cooking;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private OrderBook orderListUI;
    [SerializeField] private CourierRoster deliveryPersonSelector;
    [SerializeField] private GameClock clock;
    [SerializeField] private DayStats stats;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private DeliveryDynamics dynamics;
    private readonly List<DeliveryTrip> activeTrips = new();
    public IReadOnlyList<DeliveryTrip> ActiveTrips => activeTrips;
    public event Action<DeliveryTrip> Dispatched;
    public event Action<DeliveryTrip> Returned;
    public event Action<int> RevenueEarned;
    public event Action<int> QualityBonusEarned;
    public event Action<string> Message;
    public event Action TripsCleared;
    public void ResetForNewGame()
    {
        foreach(var trip in activeTrips){trip.Person.CurrentTrip=null;trip.Person.Status=DeliveryStatus.Idle;}
        activeTrips.Clear();TripsCleared?.Invoke();
    }

    private void OnEnable() { clock.Advanced += Tick; clock.OnClosed += Close; }
    private void OnDisable() { clock.Advanced -= Tick; clock.OnClosed -= Close; }

    public string DispatchRejectionReason(DeliveryPlan plan, DeliveryPerson person)
    {
        if (clock.Phase != DayPhase.Open) return "The store is closed.";
        if (person == null) return "Select a courier.";
        if(person.Status==DeliveryStatus.Training)return "This courier is training.";
        if (!person.IsAvailable) return "This courier is travelling.";
        int count = plan?.Orders.Count ?? orderListUI.SelectedOrders.Count;
        if (count > person.Capacity) return $"{person.Name} can carry {person.Capacity} order(s); {count} selected.";
        if (plan == null || plan.Orders.Count == 0) return "Visit all selected destinations and return to Store.";
        if(plan.Orders.Any(o=>!o.IsReady))return "These orders are still cooking.";
        if(plan.Orders.Any(o=>clock.ElapsedSeconds>=scoreManager.CancellationAt(o)))return "These orders have expired.";
        if (!deliveryPersonSelector.People.Contains(person)) return "Courier is not in the roster.";
        if (plan.Orders.Count != orderListUI.SelectedOrders.Count
            || !plan.Orders.All(o=>orderListUI.SelectedOrders.Contains(o) && orderListUI.Contains(o)))
            return "The selected orders changed. Review the route.";
        return "";
    }
    public bool CanDispatch(DeliveryPlan plan, DeliveryPerson person)
        => DispatchRejectionReason(plan,person).Length == 0;

    public bool Dispatch(DeliveryPlan plan, DeliveryPerson person)
    {
        string rejection = DispatchRejectionReason(plan,person);
        if (rejection.Length > 0) { Message?.Invoke(rejection); return false; }
        // Validate and freeze all arrival times before mutating orders or availability.
        var trip = new DeliveryTrip(FindFirstObjectByType<MapManager>(), plan, person,
            clock.ElapsedSeconds, clock.TravelTimeMultiplier,dynamics);
        foreach (Order order in plan.Orders) order.Status = OrderStatus.Dispatched;
        person.Status = DeliveryStatus.Delivering;
        person.CurrentTrip = trip;
        activeTrips.Add(trip);
        orderListUI.RemoveOrders(plan.Orders);
        deliveryPersonSelector.ClearSelection();
        Dispatched?.Invoke(trip);
        Message?.Invoke($"{person.Name} dispatched / returns in {trip.ReturnsAt - trip.StartedAt:F1}s");
        trip.AdvanceAccidents(clock.ElapsedSeconds,(road,delay)=>ReportAccident(trip,road,delay));
        return true;
    }
    private void ReportAccident(DeliveryTrip trip,string road,float delay)
    {
        stats.RecordAccident();
        Message?.Invoke($"ACCIDENT: {trip.Person.Name} / {road} / stopped {delay:F0}s; arrival delayed.");
    }

    private void Tick(float before, float now)
    {
        // Arrival timestamps, rather than frame time, determine scores even on slow frames.
        foreach (DeliveryTrip trip in activeTrips.ToArray())
        {
            trip.AdvanceAccidents(now,(road,delay)=>ReportAccident(trip,road,delay));
            foreach (ScheduledDelivery delivery in trip.Deliveries)
            {
                if (delivery.Resolved || now < delivery.ArrivesAt) continue;
                delivery.Resolved = true;
                delivery.Order.Status = OrderStatus.Delivered;
                float late = Mathf.Max(0, delivery.ArrivesAt - delivery.Order.Deadline);
                int bonus=cooking!=null?cooking.BonusForArrival(delivery.Order,delivery.ArrivesAt):0;
                int points = scoreManager.ScoreArrival(delivery.Order, delivery.ArrivesAt, trip.ServiceAtDispatch)+bonus;
                scoreManager.AddScore(points);
                RevenueEarned?.Invoke(points);
                if(bonus>0)QualityBonusEarned?.Invoke(bonus);
                stats.RecordDelivery(points, late);
                stats.RecordQuality(delivery.Order.Stars,late<=0);
                reputation.RecordArrival(late,trip.ServiceAtDispatch);
                if(cooking!=null)reputation.AddBonus(cooking.ReputationForArrival(delivery.Order,delivery.ArrivesAt));
                int tip=scoreManager.TipForArrival(delivery.Order,delivery.ArrivesAt,trip.ServiceAtDispatch);
                Message?.Invoke($"{trip.Person.Name}: {delivery.Order.DestinationNodeId} +{points}"
                    + (late > 0 ? $" ({late:F1}s late)" : $" (on time / tip +{tip})"));
            }
            trip.Person.Status = trip.StatusAt(now);
            if (now < trip.ReturnsAt) continue;
            trip.Returned = true;
            stats.RecordReturn(trip,clock.BusinessDuration);
            dynamics.ApplyTripFatigue(trip.Person,trip.StartingStamina,trip.EnduranceAtDispatch,trip.ReturnsAt-trip.StartedAt);
            trip.Person.LastReturnedAt=trip.ReturnsAt;
            trip.Person.CurrentTrip = null;
            trip.Person.Status = DeliveryStatus.Idle;
            activeTrips.Remove(trip);
            Returned?.Invoke(trip);
        }
        foreach(var person in deliveryPersonSelector.People.Where(p=>p.IsAvailable))
            dynamics.RecoverStamina(person,now-Mathf.Max(before,person.LastReturnedAt));
        CancelPending(now, false);
        TryFinishDay();
    }

    private void CancelPending(float now, bool closing)
    {
        var cancelled = orderListUI.Orders.Where(o => closing || now >= scoreManager.CancellationAt(o)).ToArray();
        foreach (Order order in cancelled) order.Status = OrderStatus.Cancelled;
        if (cancelled.Length == 0) return;
        orderListUI.RemoveOrders(cancelled);
        int penalty = closing ? scoreManager.ClosingCancellationPenalty : 0;
        scoreManager.AddScore(-cancelled.Length * penalty);
        stats.RecordCancellation(cancelled.Length, penalty, closing);
        reputation.RecordCancellation(cancelled.Length, closing);
        Message?.Invoke($"{cancelled.Length} waiting order(s) cancelled" + (closing ? " at closing." : " after expiry."));
    }

    private void Close()
    {
        CancelPending(clock.ElapsedSeconds, true);
        orderListUI.ClearSelection();
        deliveryPersonSelector.ClearSelection();
        TryFinishDay();
    }
    private void TryFinishDay() { if (clock.Phase == DayPhase.Closed && activeTrips.Count == 0) clock.FinishDay(); }
}
