using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeliveryFlowController : MonoBehaviour
{
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private OrderListUI orderListUI;
    [SerializeField] private DeliveryPersonSelector deliveryPersonSelector;
    [SerializeField] private GameClock clock;
    [SerializeField] private DayStats stats;
    private readonly List<DeliveryTrip> activeTrips = new();
    public IReadOnlyList<DeliveryTrip> ActiveTrips => activeTrips;
    public event Action<DeliveryTrip> Dispatched;
    public event Action<DeliveryTrip> Returned;
    public event Action<string> Message;

    private void OnEnable() { clock.Advanced += Tick; clock.OnClosed += Close; }
    private void OnDisable() { clock.Advanced -= Tick; clock.OnClosed -= Close; }

    public bool CanDispatch(DeliveryPlan plan, DeliveryPerson person)
        => clock.Phase == DayPhase.Open && plan != null && person != null && person.IsAvailable
            && deliveryPersonSelector.People.Contains(person) && plan.Orders.Count > 0
            && plan.Orders.Count == orderListUI.SelectedOrders.Count
            && plan.Orders.All(o => orderListUI.SelectedOrders.Contains(o) && orderListUI.Contains(o));

    public bool Dispatch(DeliveryPlan plan, DeliveryPerson person)
    {
        if (!CanDispatch(plan, person)) return false;
        // Validate and freeze all arrival times before mutating orders or availability.
        var trip = new DeliveryTrip(FindFirstObjectByType<MapManager>(), plan, person,
            clock.ElapsedSeconds, clock.TravelTimeMultiplier);
        foreach (Order order in plan.Orders) order.Status = OrderStatus.Dispatched;
        person.Status = DeliveryStatus.Delivering;
        person.CurrentTrip = trip;
        activeTrips.Add(trip);
        orderListUI.RemoveOrders(plan.Orders);
        deliveryPersonSelector.ClearSelection();
        Dispatched?.Invoke(trip);
        Message?.Invoke($"{person.Name} dispatched / returns in {trip.ReturnsAt - trip.StartedAt:F1}s");
        return true;
    }

    private void Tick(float before, float now)
    {
        // Arrival timestamps, rather than frame time, determine scores even on slow frames.
        foreach (DeliveryTrip trip in activeTrips.ToArray())
        {
            foreach (ScheduledDelivery delivery in trip.Deliveries)
            {
                if (delivery.Resolved || now < delivery.ArrivesAt) continue;
                delivery.Resolved = true;
                delivery.Order.Status = OrderStatus.Delivered;
                float late = Mathf.Max(0, delivery.ArrivesAt - delivery.Order.Deadline);
                int points = scoreManager.ScoreArrival(delivery.ArrivesAt, delivery.Order.Deadline);
                scoreManager.AddScore(points);
                stats.RecordDelivery(points, late);
                Message?.Invoke($"{trip.Person.Name}: {delivery.Order.DestinationNodeId} +{points}"
                    + (late > 0 ? $" ({late:F1}s late)" : " (on time)"));
            }
            trip.Person.Status = trip.StatusAt(now);
            if (now < trip.ReturnsAt) continue;
            trip.Returned = true;
            trip.Person.CurrentTrip = null;
            trip.Person.Status = DeliveryStatus.Idle;
            activeTrips.Remove(trip);
            Returned?.Invoke(trip);
        }
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
        stats.RecordCancellation(cancelled.Length, penalty);
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
