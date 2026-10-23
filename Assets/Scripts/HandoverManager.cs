using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Couriers' bags at the counter. A packed box goes into an idle courier's bag (up to their load);
/// opening the dispatch board for that courier selects exactly the bagged orders. Delivery rules stay in DeliveryFlowController.</summary>
public sealed class HandoverManager : MonoBehaviour
{
    [SerializeField] private OrderBook orders;
    [SerializeField] private CourierRoster roster;
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private GameClock clock;
    private readonly Dictionary<DeliveryPerson,List<Order>> bags=new();
    public event Action Changed;
    public string LastMessage {get;private set;}="";
    private void OnEnable(){orders.OrdersChanged+=Prune;flow.Dispatched+=Dispatched;flow.TripsCleared+=Clear;clock.OnDayStarted+=Clear;clock.OnClosed+=Clear;}
    private void OnDisable(){orders.OrdersChanged-=Prune;flow.Dispatched-=Dispatched;flow.TripsCleared-=Clear;clock.OnDayStarted-=Clear;clock.OnClosed-=Clear;}
    public IReadOnlyList<Order> BagOf(DeliveryPerson person)=>person!=null && bags.TryGetValue(person,out var bag)?bag:Array.Empty<Order>();
    public bool IsBagged(Order order)=>bags.Values.Any(b=>b.Contains(order));
    public DeliveryPerson Holder(Order order)=>bags.FirstOrDefault(p=>p.Value.Contains(order)).Key;
    public IEnumerable<Order> RackOrders=>orders.Orders.Where(o=>o.IsReady && !IsBagged(o)).OrderBy(o=>o.Deadline);
    public string BagRejection(Order order,DeliveryPerson person)
    {
        if(clock.Phase!=DayPhase.Open)return "営業中だけ渡せます。";
        if(person==null || !roster.People.Contains(person))return "配達員を選んでください。";
        if(person.Status==DeliveryStatus.Training)return person.Name+" は訓練中です。";
        if(!person.IsAvailable)return person.Name+" は店にいません。";
        if(order==null || !orders.Contains(order) || !order.IsReady)return "梱包済みの箱だけ渡せます。";
        if(clock.ElapsedSeconds>=FindFirstObjectByType<ScoreManager>().CancellationAt(order))return "期限が切れています。";
        if(IsBagged(order))return "この箱はもう誰かのバッグに入っています。";
        if(BagOf(person).Count>=person.Capacity)return $"{person.Name} のバッグはいっぱいです（{person.Capacity}箱まで）。";
        return "";
    }
    public bool Bag(Order order,DeliveryPerson person)
    {
        LastMessage=BagRejection(order,person);if(LastMessage.Length>0){Changed?.Invoke();return false;}
        if(!bags.TryGetValue(person,out var bag))bags[person]=bag=new List<Order>();
        bag.Add(order);Select(person);Changed?.Invoke();return true;
    }
    public bool Unbag(Order order)
    {
        var holder=Holder(order);if(holder==null)return false;
        bags[holder].Remove(order);if(roster.SelectedPerson==holder)Select(holder);Changed?.Invoke();return true;
    }
    /// <summary>Change the visit order inside a bag, or move a bagged box to another courier's bag at an index.</summary>
    public bool Move(Order order,DeliveryPerson person,int index)
    {
        var holder=Holder(order);LastMessage="";
        if(holder==null || person==null){LastMessage="この箱はバッグに入っていません。";return false;}
        if(holder!=person)
        {
            if(clock.Phase!=DayPhase.Open)LastMessage="営業中だけ渡せます。";
            else if(!person.IsAvailable)LastMessage=person.Name+" は店にいません。";
            else if(BagOf(person).Count>=person.Capacity)LastMessage=$"{person.Name} のバッグはいっぱいです（{person.Capacity}箱まで）。";
            if(LastMessage.Length>0){Changed?.Invoke();return false;}
            bags[holder].Remove(order);if(!bags.TryGetValue(person,out var target))bags[person]=target=new List<Order>();
            target.Insert(Mathf.Clamp(index<0?target.Count:index,0,target.Count),order);
        }
        else
        {
            var bag=bags[person];bag.Remove(order);bag.Insert(Mathf.Clamp(index<0?bag.Count:index,0,bag.Count),order);
        }
        Select(person);Changed?.Invoke();return true;
    }
    /// <summary>Make the planner work on this courier's bag (selection drives route and dispatch).</summary>
    public bool Select(DeliveryPerson person)
    {
        if(person==null || !person.IsAvailable || clock.Phase!=DayPhase.Open)return false;
        var bag=BagOf(person);
        if(roster.SelectedPerson!=person)roster.SelectForDispatch(person);
        if(bag.Count==0)orders.ClearSelection();
        else if(!orders.SelectedOrders.SequenceEqual(bag))orders.SelectForDispatch(bag);
        return true;
    }
    private void Dispatched(DeliveryTrip trip){bags.Remove(trip.Person);Changed?.Invoke();}
    private void Prune()
    {
        bool changed=false;
        foreach(var bag in bags.Values)changed|=bag.RemoveAll(o=>!orders.Contains(o))>0;
        if(changed)Changed?.Invoke();
    }
    public void Clear(){if(bags.Count==0)return;bags.Clear();Changed?.Invoke();}
}
