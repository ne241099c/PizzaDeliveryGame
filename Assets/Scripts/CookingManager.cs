using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>FIFO automatic cooking on the same GameClock as orders and deliveries.</summary>
[DefaultExecutionOrder(-40)]
public sealed class CookingManager : MonoBehaviour
{
    [SerializeField] private GameClock clock;
    [SerializeField] private OrderListUI orders;
    [SerializeField,Min(.01f)] private float cookSeconds=12;
    [SerializeField,Min(1)] private int initialOvens=1;
    [SerializeField,Range(0,1)] private float perfectStart=.85f;
    [SerializeField,Range(0,1)] private float perfectEnd=.95f;
    [SerializeField,Min(0)] private int perfectBakeBonus=15;
    public bool ManualControlEnabled {get;set;}=true;
    public float PerfectStart=>perfectStart;
    public float PerfectEnd=>perfectEnd;
    public bool IsPerfectProgress(float value)=>value>=perfectStart && value<=perfectEnd;
    public int BonusForArrival(Order order,float arrivedAt)=>order.Quality==BakeQuality.Perfect && arrivedAt<=order.Deadline?perfectBakeBonus:0;
    private void Update()
    {
        if(!ManualControlEnabled || Keyboard.current==null || !Keyboard.current.spaceKey.wasPressedThisFrame)return;
        var order=CookingOrders.FirstOrDefault(o=>Progress(o)>=perfectStart);
        if(order!=null)TryTakeOut(order);
    }
    public bool TryTakeOut(Order order)
    {
        if(!ManualControlEnabled || clock.Phase!=DayPhase.Open || order==null || order.Baking!=BakingState.Cooking || clock.ElapsedSeconds>=order.Deadline)return false;
        float progress=Progress(order);if(progress<perfectStart || progress>=1)return false;
        var oven=ovens.FirstOrDefault(o=>o.Active==order);if(oven==null)return false;
        Release(oven,clock.ElapsedSeconds);order.Quality=IsPerfectProgress(progress)?BakeQuality.Perfect:BakeQuality.Standard;order.Baking=BakingState.Ready;order.CookFinishesAt=clock.ElapsedSeconds;
        AdvanceTo(clock.ElapsedSeconds);return true;
    }
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
