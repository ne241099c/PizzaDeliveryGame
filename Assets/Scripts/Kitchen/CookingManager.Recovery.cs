using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>Recovery keeps the original ticket/deadline. Assistance never interrupts a cook's current task.</summary>
public sealed partial class CookingManager
{
    [SerializeField,Min(0)] private float playerIdleSeconds=8;
    [SerializeField,Min(0)] private float wasteCost=0;
    [SerializeField,Range(1,4)] private int redoBelowStar=2;
    [SerializeField,Min(0)] private float redoMinRemainingSec=0;
    private readonly Dictionary<Order,int> redoCounts=new();
    private readonly HashSet<Order> redoPriority=new();
    private readonly Dictionary<Order,float> redoQueuedAt=new();
    public float PlayerIdleSeconds=>playerIdleSeconds;
    public decimal WasteCost=>BusinessEconomy.Money((decimal)wasteCost);
    public int RedoCount(Order order)=>redoCounts.TryGetValue(order,out int n)?n:0;
    public bool IsRedo(Order order)=>RedoCount(order)>0;
    public void Touch(PizzaJob job){if(job!=null && jobs.Contains(job) && job.Worker==PizzaWorker.Player)job.LastTouchedAt=clock.ElapsedSeconds;}
    private float AssistAt(PizzaJob job)=>job.LastTouchedAt+playerIdleSeconds;
    private bool CanAssist(PizzaJob job)=>clock.Phase==DayPhase.Open && job.Worker==PizzaWorker.Player && job.Stage!=PizzaStage.Packed && !job.NeedsDecision && !job.ServeApproved && !job.QueuedForOven;
    private bool Assist(StaffSlot slot,float t)
    {
        var job=jobs.Where(j=>CanAssist(j) && AssistAt(j)<=t).OrderByDescending(j=>j.Stage==PizzaStage.Baking || j.Undercooked).ThenBy(j=>j.LastTouchedAt).FirstOrDefault();
        if(job==null)return false;
        job.Worker=PizzaWorker.Staff;job.Staff=slot.Staff;job.LastTouchedAt=t;
        if(job.Stage==PizzaStage.Baking)
        {
            // A free cook can take an already ready pizza out, even if other ovens have a queue.
            float elapsed=t-job.OvenStartedAt;
            if(elapsed>=BandStartFor(job.Order))
            {
                job.BakePoint=elapsed<=BandEndFor(job.Order);job.Burnt=elapsed>=BurnAtFor(job.Order);FinishOven(job,t);CompleteBake(job,t);
            }
            return true;
        }
        if(job.Undercooked){QueueOven(job,t);return true;}
        if(job.Stage>=PizzaStage.Baked)return true;
        if(job.Stage==PizzaStage.Topping){QueueOven(job,t);return true;}
        slot.FromStage=job.Stage;
        slot.Task=job;slot.Packing=false;slot.Staff.IsWorking=true;
        float remaining=job.Stage switch
        {
            PizzaStage.Ticket=>doughSeconds+sauceSeconds+assemblySeconds,
            PizzaStage.Dough=>sauceSeconds+assemblySeconds,
            PizzaStage.Sauce=>assemblySeconds,
            _=>assemblySeconds
        };
        remaining+=Mathf.Max(0,job.Order.RequiredToppings.Length-job.AddedToppings.Count-1)*extraToppingSeconds;
        job.Stage=PizzaStage.Topping;slot.Until=t+remaining*StaffFactor(slot.Staff);
        return true;
    }
    public string FailureReason(PizzaJob job)
    {
        if(job==null)return "";
        if(job.Undercooked)return ShopStrings.Undercooked;
        if(job.Burnt)return ShopStrings.Burnt;
        var errors=new List<string>();
        if(!job.ToppingPoint)errors.Add(ShopStrings.WrongIngredients);
        if(!job.SaucePoint)errors.Add(ShopStrings.ThinSauce);
        if(!job.BakePoint)errors.Add(ShopStrings.Overbaked);
        return string.Join("・",errors);
    }
    private void CompleteBake(PizzaJob job,float at)
    {
        if(job.ServeApproved)return;
        if(job.Worker==PizzaWorker.Staff)
        {
            if(job.Stars<redoBelowStar && CanAutoRedo(job,at)){DiscardInternal(job,at,true);return;}
            return; // Staff serve when a redo cannot arrive before the original deadline.
        }
        job.NeedsDecision=FailureReason(job).Length>0;
    }
    /// <summary>Production cooking time plus fastest available roster member's outbound route; never uses a duplicate time formula.</summary>
    public float AutoRedoSeconds(PizzaJob job)=>AutoRedoSeconds(job,clock.ElapsedSeconds);
    private float AutoRedoSeconds(PizzaJob job,float at)
    {
        if(job==null || job.Staff==null)return float.PositiveInfinity;
        var map=FindFirstObjectByType<MapManager>();var roster=FindFirstObjectByType<CourierRoster>();
        float delivery=float.PositiveInfinity;
        foreach(var person in roster.People)
        {
            if(person.Status==DeliveryStatus.Training)continue;
            var path=RoadPathfinder.FindShortestPath(map.Edges,"Store",job.Order.DestinationNodeId,e=>map.GetTravelTime(e.Id,person));
            if(path==null)continue;
            float waiting=person.CurrentTrip!=null?Mathf.Max(0,person.CurrentTrip.ReturnsAt-at):0;
            delivery=Mathf.Min(delivery,waiting+clock.ToRealSeconds(path.Sum(id=>map.GetTravelTime(id,person))));
        }
        float ovenWait=ovens.Select(j=>j==null || j==job?0:Mathf.Max(0,j.OvenStartedAt+BakeDuration(j.Order)-at)).DefaultIfEmpty(0).Min();
        float queued=OvenQueue.Where(j=>j!=job).Sum(j=>BakeDuration(j.Order))/Mathf.Max(1,OvenCount);
        float staffWait=staff.Select(s=>s.Task!=null?Mathf.Max(0,s.Until-at):0).DefaultIfEmpty(0).Min();
        return Mathf.Max(redoMinRemainingSec,Mathf.Max(staffWait+StaffWorkSeconds(job.Staff,job.Order),ovenWait+queued)+BakeDuration(job.Order)+StaffPackSeconds(job.Staff)+delivery);
    }
    public bool CanAutoRedo(PizzaJob job,float at)=>job!=null && !job.ServeApproved && job.Order.Deadline-at>=AutoRedoSeconds(job,at);
    public bool CanDiscard(PizzaJob job)=>ManualControlEnabled && clock.Phase==DayPhase.Open && job!=null && jobs.Contains(job) && job.Stage>=PizzaStage.Dough && orders.Contains(job.Order) && job.Order.Status==OrderStatus.Pending && clock.ElapsedSeconds<Expires(job.Order) && !StaffBusyWith(job);
    public bool Discard(PizzaJob job)
    {
        if(!Ready() || !CanDiscard(job))return false;
        DiscardInternal(job,clock.ElapsedSeconds,false);return Done();
    }
    private void DiscardInternal(PizzaJob job,float at,bool automatic)
    {
        var order=job.Order;int oldBench=job.Bench;
        if(handover!=null && handover.IsBagged(order))handover.Unbag(order);
        Release(job,at);order.Baking=BakingState.Waiting;order.Stars=0;
        if(orders.SelectedOrders.Contains(order))
        {
            var remaining=orders.SelectedOrders.Where(o=>o!=order).ToArray();
            if(remaining.Length==0)orders.ClearSelection();
            else orders.SelectForDispatch(remaining);
        }
        redoCounts[order]=RedoCount(order)+1;redoPriority.Add(order);redoQueuedAt[order]=at;
        FindFirstObjectByType<BusinessEconomy>().ChargeWaste(WasteCost);
        if(!automatic)
        {
            int bench=oldBench>=0 && oldBench<benches.Length && benches[oldBench]==null?oldBench:FreeBench();
            if(bench>=0)
            {
                var fresh=new PizzaJob(order,PizzaWorker.Player){Bench=bench,Stage=PizzaStage.Dough,StartedAt=at,LastTouchedAt=at};
                benches[bench]=fresh;jobs.Add(fresh);order.Baking=BakingState.Cooking;
            }
        }
    }
    public bool ServeAsIs(PizzaJob job)
    {
        if(!Ready() || job==null || !jobs.Contains(job) || !job.NeedsDecision || job.Undercooked)return false;
        job.ServeApproved=true;job.NeedsDecision=false;return Done();
    }
    public bool ResumeBaking(PizzaJob job,int oven)
    {
        if(!Ready() || job==null || !jobs.Contains(job) || !job.Undercooked || job.Stage!=PizzaStage.Baked || oven<0 || oven>=ovens.Length)return false;
        Touch(job);QueueOven(job,clock.ElapsedSeconds);return Done();
    }
}
