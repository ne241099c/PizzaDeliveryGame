using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>FIFO automatic cooking on the same GameClock as orders and deliveries.</summary>
[DefaultExecutionOrder(-40)]
public sealed class CookingManager : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private OrderListUI orders;
    [SerializeField,Min(.01f)] private float cookSeconds=12;
    [SerializeField,Min(1)] private int initialOvens=1;
    private sealed class Oven {public Order Active;public float FreeAt;}
    private readonly List<Oven> ovens=new();
    public float CookSeconds=>cookSeconds;
    public int OvenCount=>initialOvens;
    public int MaximumQueueLength {get;private set;}
    public float BusySeconds {get;private set;}
    public float Utilization=>BusySeconds/(OvenCount*clock.BusinessDuration);
    public IReadOnlyList<Order> CookingOrders=>ovens.Where(o=>o.Active!=null).Select(o=>o.Active).ToArray();
    private void Awake()=>ResetDay();
    private void OnEnable(){clock.Advanced+=Tick;clock.OnDayStarted+=ResetDay;orders.OrdersChanged+=Synchronize;}
    private void OnDisable(){clock.Advanced-=Tick;clock.OnDayStarted-=ResetDay;orders.OrdersChanged-=Synchronize;}
    public void ResetDay(){ovens.Clear();for(int i=0;i<OvenCount;i++)ovens.Add(new Oven());MaximumQueueLength=0;BusySeconds=0;}
    private void Synchronize()=>AdvanceTo(clock.ElapsedSeconds);
    private void Tick(float before,float now)=>AdvanceTo(now);
    public float Progress(Order order)=>order.Baking==BakingState.Ready?1:order.Baking==BakingState.Cooking?Mathf.Clamp01((clock.ElapsedSeconds-order.CookStartedAt)/(order.CookFinishesAt-order.CookStartedAt)):0;
    private void Release(Oven oven,float time)
    {
        var order=oven.Active;
        float end=Mathf.Min(time,order.CookFinishesAt,order.Deadline,clock.BusinessDuration);
        BusySeconds+=Mathf.Max(0,end-order.CookStartedAt);
        oven.FreeAt=Mathf.Min(time,order.CookFinishesAt,order.Deadline);oven.Active=null;
    }
    private void AdvanceTo(float now)
    {
        if(ovens.Count==0)return;
        for(int iteration=0;iteration<orders.OrderCount*2+ovens.Count+1;iteration++)
        {
            foreach(var oven in ovens.Where(o=>o.Active!=null))
            {
                var order=oven.Active;
                if(!orders.Contains(order) || now>=order.CookFinishesAt || now>=order.Deadline)
                {
                    if(orders.Contains(order) && order.CookFinishesAt<=now && order.CookFinishesAt<=order.Deadline)order.Baking=BakingState.Ready;
                    Release(oven,now);
                }
            }
            if(clock.Phase!=DayPhase.Open)break;
            var free=ovens.Where(o=>o.Active==null).OrderBy(o=>o.FreeAt).FirstOrDefault();if(free==null)break;
            var next=orders.Orders.Where(o=>o.Baking==BakingState.Waiting && o.SpawnedAt<=now && o.Deadline>Mathf.Max(o.SpawnedAt,free.FreeAt)).OrderBy(o=>o.SpawnedAt).FirstOrDefault();if(next==null)break;
            next.CookStartedAt=Mathf.Max(next.SpawnedAt,free.FreeAt);next.CookFinishesAt=next.CookStartedAt+cookSeconds;next.Baking=BakingState.Cooking;free.Active=next;
        }
        MaximumQueueLength=Mathf.Max(MaximumQueueLength,orders.Orders.Count(o=>o.Baking==BakingState.Waiting && o.Deadline>now));
    }
}
