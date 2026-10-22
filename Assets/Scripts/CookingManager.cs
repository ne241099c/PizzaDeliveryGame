using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PizzaStage { Ticket, Dough, Sauce, Cheese, Topping, Baking, Baked, Boxed, Packed }
public enum PizzaWorker { Player, Staff }

/// <summary>One pizza being made for one order. Points: sauce coverage, correct topping, bake timing.</summary>
public sealed class PizzaJob
{
    public Order Order {get;}
    public PizzaWorker Worker {get;}
    public KitchenStaff Staff {get;internal set;}
    public PizzaStage Stage {get;internal set;}
    public int Bench {get;internal set;}=-1;
    public int Oven {get;internal set;}=-1;
    public float StartedAt {get;internal set;}
    public float AssembledAt {get;internal set;}
    public bool QueuedForOven {get;internal set;}
    public long QueueSequence {get;internal set;}
    public float OvenStartedAt {get;internal set;}
    public float TakenOutAt {get;internal set;}
    public float PackingStartedAt {get;internal set;}
    public float SauceCoverage {get;internal set;}
    public Topping? AddedTopping {get;internal set;}
    public readonly List<Topping> AddedToppings=new();
    public bool SaucePoint {get;internal set;}
    public bool ToppingPoint {get;internal set;}
    public bool BakePoint {get;internal set;}
    public bool Burnt {get;internal set;}
    public int Points=>(SaucePoint?1:0)+(ToppingPoint?1:0)+(BakePoint?1:0);
    /// <summary>0 points = 1 star, 1-2 = 2 stars, 3 = 3 stars. Burnt pizzas are always 1 star.</summary>
    public int Stars=>Burnt?1:Points==0?1:Points<3?2:3;
    public PizzaJob(Order order,PizzaWorker worker){Order=order;Worker=worker;}
}

/// <summary>The shop kitchen: workbenches, ovens, a warming shelf, packing and the box rack.
/// Kitchen staff work automatically on exact clock timestamps; the player can claim tickets and cook by hand.</summary>
[DefaultExecutionOrder(-40)]
public sealed class CookingManager : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private GameClock clock;
    [SerializeField] private OrderListUI orders;
    [SerializeField] private KitchenCrew crew;
    [SerializeField] private HandoverManager handover;
    [Tooltip("Standard total for one pizza by staff with speed 0.5. Used for the delivery promise minimum.")]
    [SerializeField,Min(1)] private int initialOvens=1;
    [SerializeField,Min(1)] private int initialBenches=2;
    [SerializeField,Min(.01f)] private float doughSeconds=2,sauceSeconds=4,assemblySeconds=6,bakeSeconds=6;
    [SerializeField,Min(0)] private float seafoodExtraBakeSeconds=1,extraToppingSeconds=2;
    public float BakeDuration(Order order)=>bakeSeconds+(order.Kind==PizzaKind.Seafood?seafoodExtraBakeSeconds:0);
    public float BandStartFor(Order order)=>BakeDuration(order)-bakeBandHalfWidth;
    public float BandEndFor(Order order)=>BakeDuration(order)+bakeBandHalfWidth;
    public float BurnAtFor(Order order)=>BandEndFor(order)+burnAfterBand;
    [Tooltip("Boxing time after baking. 0 = packed the moment it leaves the oven (2026-10-23 request: ignore packing time).")]
    [SerializeField,Min(0)] private float packSeconds=0;
    [SerializeField,Min(0)] private float bakeBandHalfWidth=.4f,burnAfterBand=5;
    [SerializeField] private float staffTimeBase=1.3f,staffTimeScale=.6f;
    [SerializeField,Range(0,1)] private float sauceThreshold=.7f;
    [SerializeField] private float staffSuccessBase=.3f,staffSuccessScale=.5f;
    [SerializeField,Min(0)] private int twoStarBonus=8,threeStarBonus=20;
    [SerializeField,Min(0)] private float threeStarReputation=.5f;
    [SerializeField,Min(1)] private int rackCapacity=6,packingSlots=2;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int randomSeed=5113;

    private sealed class StaffSlot {public KitchenStaff Staff;public PizzaJob Task;public bool Packing;public float Until,FreeSince;}
    private readonly List<PizzaJob> jobs=new();
    private readonly List<StaffSlot> staff=new();
    private PizzaJob[] benches=Array.Empty<PizzaJob>();
    private PizzaJob[] ovens=Array.Empty<PizzaJob>();
    private float[] benchFreeSince=Array.Empty<float>(),ovenFreeSince=Array.Empty<float>();
    private readonly HashSet<Order> expired=new();
    private float processed;
    private long queueSequence;
    private ScoreManager score;
    private float Expires(Order order) { if(score==null)score=FindFirstObjectByType<ScoreManager>(); return score.CancellationAt(order); }
    public float PackSeconds=>packSeconds;
    private System.Random random;

#if UNITY_EDITOR
    /// <summary>Autoplay-only cook. Not in the roster, never paid or saved, absent from player builds.</summary>
    public KitchenStaff EditorSubstitute {get;set;}
#endif
    public bool ManualControlEnabled {get;set;}=true;
    public int? SeedOverride {get;set;}
    /// <summary>Standard time for one pizza by staff with speed 0.5 (assembly + oven + packing). Used for the delivery promise minimum.</summary>
    public float CookSeconds=>(doughSeconds+sauceSeconds+assemblySeconds+(facilities!=null && facilities.IsUnlocked(PizzaKind.Seafood)?extraToppingSeconds:0))*(staffTimeBase-staffTimeScale*.5f)+bakeSeconds+(facilities!=null && facilities.IsUnlocked(PizzaKind.Seafood)?seafoodExtraBakeSeconds:0)+packSeconds;
    public float BakeSeconds=>bakeSeconds;
    public float BandStart=>bakeSeconds-bakeBandHalfWidth;
    public float BandEnd=>bakeSeconds+bakeBandHalfWidth;
    public float BurnAt=>BandEnd+burnAfterBand;
    public float SauceThreshold=>sauceThreshold;
    public int RackCapacity=>rackCapacity;
    public int PackingSlots=>packingSlots;
    public int OvenCount=>facilities!=null?facilities.Ovens:initialOvens;
    public int BenchCount=>facilities!=null?facilities.Workbenches:initialBenches;
    public int MaximumQueueLength {get;private set;}
    /// <summary>Pizzas packed today by the player and by each kitchen staff member.</summary>
    public int PackedByPlayer {get;private set;}
    private readonly Dictionary<string,int> packedByStaff=new();
    public IReadOnlyDictionary<string,int> PackedByStaff=>packedByStaff;
    public float BusySeconds {get;private set;}
    public int OperatingOvenCount {get;private set;}=1;
    public float Utilization=>BusySeconds/(OperatingOvenCount*clock.BusinessDuration);
    public IReadOnlyList<PizzaJob> Jobs=>jobs;
    public IReadOnlyList<Order> CookingOrders=>ovens.Where(o=>o!=null).Select(o=>o.Order).ToArray();
    public PizzaJob BenchJob(int index)=>index>=0 && index<benches.Length?benches[index]:null;
    public IEnumerable<PizzaJob> OvenQueue=>jobs.Where(j=>j.QueuedForOven).OrderBy(j=>j.AssembledAt).ThenBy(j=>j.QueueSequence);
    public PizzaJob OvenJob(int index)=>index>=0 && index<ovens.Length?ovens[index]:null;
    public IEnumerable<PizzaJob> Shelf=>jobs.Where(j=>j.Stage==PizzaStage.Baked).OrderBy(j=>j.TakenOutAt);
    public IEnumerable<PizzaJob> PackingTable=>jobs.Where(j=>j.Stage==PizzaStage.Boxed && j.Worker==PizzaWorker.Player);
    public PizzaJob JobFor(Order order)=>jobs.FirstOrDefault(j=>j.Order==order);
    public bool IsStaffBusy(KitchenStaff member)=>staff.Any(s=>s.Staff==member && s.Task!=null);
    public PizzaJob StaffTask(KitchenStaff member)=>staff.FirstOrDefault(s=>s.Staff==member)?.Task;
    /// <summary>Packed boxes waiting at the counter (not yet in a courier's bag).</summary>
    public int RackCount=>orders.Orders.Count(o=>o.IsReady && (handover==null || !handover.IsBagged(o)))+jobs.Count(j=>j.Stage==PizzaStage.Boxed);
    public float StaffWorkSeconds(KitchenStaff member)=>(doughSeconds+sauceSeconds+assemblySeconds)*StaffFactor(member);
    public float StaffWorkSeconds(KitchenStaff member,Order order)=>StaffWorkSeconds(member)+Mathf.Max(0,order.RequiredToppings.Length-1)*extraToppingSeconds*StaffFactor(member);
    public float StaffPackSeconds(KitchenStaff member)=>packSeconds*StaffFactor(member);
    private float StaffFactor(KitchenStaff member)=>Mathf.Max(.05f,staffTimeBase-staffTimeScale*member.Speed);
    public float StaffSuccessChance(KitchenStaff member)=>Mathf.Clamp01(staffSuccessBase+staffSuccessScale*member.Care);
    public int BonusForArrival(Order order,float arrivedAt)=>arrivedAt>order.Deadline?0:order.Stars>=3?threeStarBonus:order.Stars==2?twoStarBonus:0;
    public float ReputationForArrival(Order order,float arrivedAt)=>arrivedAt<=order.Deadline && order.Stars>=3?threeStarReputation:0;
    public int TwoStarBonus=>twoStarBonus;
    public int ThreeStarBonus=>threeStarBonus;

    private void Awake(){ResetRandom();ResetDay();}
    private void OnEnable(){clock.Advanced+=Tick;clock.OnDayStarted+=ResetDay;orders.OrdersChanged+=Synchronize;}
    private void OnDisable(){clock.Advanced-=Tick;clock.OnDayStarted-=ResetDay;orders.OrdersChanged-=Synchronize;}
    public void ResetRandom()=>random=new System.Random(SeedOverride ?? (useFixedSeed?randomSeed:Environment.TickCount));
    public void ResetDay()
    {
        OperatingOvenCount=OvenCount;jobs.Clear();expired.Clear();staff.Clear();
        ovens=new PizzaJob[OvenCount];benches=new PizzaJob[BenchCount];ovenFreeSince=new float[OvenCount];benchFreeSince=new float[BenchCount];
        if(crew!=null){crew.ResetDailyState();foreach(var member in crew.People)staff.Add(new StaffSlot{Staff=member});}
#if UNITY_EDITOR
        if(staff.Count==0 && EditorSubstitute!=null)staff.Add(new StaffSlot{Staff=EditorSubstitute});
#endif
        MaximumQueueLength=0;queueSequence=0;BusySeconds=0;PackedByPlayer=0;packedByStaff.Clear();processed=clock!=null?clock.ElapsedSeconds:0;
    }
    private void Synchronize()=>AdvanceTo(clock.ElapsedSeconds);
    private void Tick(float before,float now)=>AdvanceTo(now);

    /// <summary>Overall progress for display: 0 waiting ticket, 1 packed.</summary>
    public float Progress(Order order)
    {
        if(order.IsReady)return 1;var job=JobFor(order);if(job==null)return 0;
        float now=clock.ElapsedSeconds;
        return job.Stage switch
        {
            PizzaStage.Ticket=>.05f,PizzaStage.Dough=>.15f,PizzaStage.Sauce=>.3f,PizzaStage.Cheese=>.4f,
            PizzaStage.Topping=>job.Worker==PizzaWorker.Staff && StaffBusyWith(job)?Mathf.Lerp(.05f,.5f,StaffFraction(job,now)):.5f,
            PizzaStage.Baking=>Mathf.Lerp(.5f,.8f,Mathf.Clamp01((now-job.OvenStartedAt)/BakeDuration(order))),
            PizzaStage.Baked=>.82f,PizzaStage.Boxed=>job.Worker==PizzaWorker.Staff?Mathf.Lerp(.85f,.98f,StaffFraction(job,now)):.9f,_=>1
        };
    }
    private bool StaffBusyWith(PizzaJob job)=>staff.Any(s=>s.Task==job);
    private float StaffFraction(PizzaJob job,float now)
    {
        var slot=staff.FirstOrDefault(s=>s.Task==job);if(slot==null)return 1;
        float length=slot.Packing?StaffPackSeconds(slot.Staff):StaffWorkSeconds(slot.Staff,job.Order);
        return Mathf.Clamp01(1-(slot.Until-now)/Mathf.Max(.01f,length));
    }
    public string StageCaption(Order order)
    {
        if(order.IsReady)return "梱包済み";var job=JobFor(order);
        if(job==null)return "順番待ち";if(job.QueuedForOven)return "窯待ち "+(OvenQueue.ToList().IndexOf(job)+1)+"番";
        return job.Stage switch
        {
            PizzaStage.Baking=>"焼いている",PizzaStage.Baked=>"梱包待ち",PizzaStage.Boxed=>"梱包中",
            _=>job.Worker==PizzaWorker.Player?"あなたが調理中":(job.Staff!=null?job.Staff.Name+" 調理中":"調理中")
        };
    }

    /// <summary>Display estimate of the seconds until the order is packed (queue, staff, oven). Null while the player is
    /// making it by hand or nobody in the kitchen can take it.</summary>
    public float? ReadyIn(Order order)
    {
        if(order==null || clock==null)return null;if(order.IsReady)return 0;
        float now=clock.ElapsedSeconds;
        float ovenFree=float.PositiveInfinity;foreach(var o in ovens)ovenFree=Mathf.Min(ovenFree,o==null?0:Mathf.Max(0,o.OvenStartedAt+BakeDuration(o.Order)-now));
        if(float.IsInfinity(ovenFree))ovenFree=0;
        var job=JobFor(order);
        if(job!=null)
        {
            if(job.Stage==PizzaStage.Baking)return Mathf.Max(0,job.OvenStartedAt+BakeDuration(job.Order)-now)+packSeconds;
            if(job.Stage>=PizzaStage.Baked)return packSeconds;
            if(job.QueuedForOven)return ovenFree+OvenQueue.TakeWhile(j=>j!=job).Sum(j=>BakeDuration(j.Order))/Mathf.Max(1,OvenCount)+BakeDuration(order)+packSeconds;
            if(job.Worker!=PizzaWorker.Staff)return null;
            var slot=staff.FirstOrDefault(s=>s.Task==job);
            return Mathf.Max(slot!=null?Mathf.Max(0,slot.Until-now):0,ovenFree)+BakeDuration(order)+packSeconds;
        }
        if(staff.Count==0 || order.Baking!=BakingState.Waiting)return null;
        var waiting=orders.Orders.Where(o=>o.Baking==BakingState.Waiting && JobFor(o)==null && !expired.Contains(o)).OrderBy(o=>o.SpawnedAt).ToList();
        int ahead=waiting.IndexOf(order);if(ahead<0)return null;
        float work=staff.Average(s=>StaffWorkSeconds(s.Staff,order));
        float firstFree=staff.Min(s=>s.Task==null?0:Mathf.Max(0,s.Until-now));
        float cycle=Mathf.Max(work/Mathf.Max(1,Mathf.Min(staff.Count,BenchCount)),bakeSeconds/Mathf.Max(1,ovens.Length));
        return Mathf.Max(firstFree+ahead*cycle+work,ovenFree)+BakeDuration(order)+packSeconds;
    }

    // ---- automatic staff simulation on exact timestamps ----
    private void AdvanceTo(float now)
    {
        if(now<processed)now=processed;
        DropRemovedOrders();
        for(int guard=0;guard<100000;guard++)
        {
            Assign(processed);
            float next=NextEvent(processed);
            if(next>now)break;
            processed=next;Process(processed);
        }
        processed=now;Assign(now);
        if(clock.Phase==DayPhase.Open || clock.Phase==DayPhase.BeforeOpening)
            MaximumQueueLength=Mathf.Max(MaximumQueueLength,orders.Orders.Count(o=>o.Baking==BakingState.Waiting && Expires(o)>now && !expired.Contains(o)));
    }
    private void DropRemovedOrders()
    {
        foreach(var job in jobs.Where(j=>!orders.Contains(j.Order) && j.Stage!=PizzaStage.Packed).ToArray())Release(job,Mathf.Min(clock.ElapsedSeconds,Expires(job.Order)));
        jobs.RemoveAll(j=>j.Stage==PizzaStage.Packed && !orders.Contains(j.Order));
    }
    // Assignments may be backdated to the exact moment a ticket arrived, so pending events can lie in the past;
    // those are processed at once (Process uses each event's own timestamp).
    private float NextEvent(float after)
    {
        float next=float.PositiveInfinity;
        foreach(var slot in staff)if(slot.Task!=null)next=Mathf.Min(next,slot.Until);
        foreach(var job in ovens)if(job!=null && job.Worker==PizzaWorker.Staff)next=Mathf.Min(next,job.OvenStartedAt+BakeDuration(job.Order));
        foreach(var job in jobs)
        {
            if(job.Stage!=PizzaStage.Packed)next=Mathf.Min(next,Expires(job.Order));
            if(job.Worker==PizzaWorker.Player && job.Stage==PizzaStage.Boxed)next=Mathf.Min(next,job.PackingStartedAt+packSeconds);
        }
        next=Mathf.Max(next,after);
        if(clock.Phase==DayPhase.Open && staff.Any(s=>s.Task==null))
            foreach(var order in orders.Orders)if(order.Baking==BakingState.Waiting && order.SpawnedAt>after && Expires(order)>order.SpawnedAt)next=Mathf.Min(next,order.SpawnedAt);
        return next;
    }
    private void Process(float t)
    {
        foreach(var job in jobs.Where(j=>j.Stage!=PizzaStage.Packed && Expires(j.Order)<=t).ToArray()){expired.Add(job.Order);Release(job,Expires(job.Order));}
        foreach(var job in jobs.Where(j=>j.Worker==PizzaWorker.Player && j.Stage==PizzaStage.Boxed && j.PackingStartedAt+packSeconds<=t).ToArray())Pack(job);
        foreach(var slot in staff.Where(s=>s.Task!=null && s.Until<=t).OrderBy(s=>s.Until).ToArray())
        {
            var job=slot.Task;float at=slot.Until;
            if(slot.Packing)Pack(job);
            else
            {
                bool success(){return random.NextDouble()<StaffSuccessChance(slot.Staff);}
                job.SauceCoverage=1;job.SaucePoint=success();job.AddedTopping=job.Order.RequiredTopping;job.AddedToppings.AddRange(job.Order.RequiredToppings);job.ToppingPoint=success();job.Stage=PizzaStage.Topping;
                QueueOven(job,at);
            }
            slot.Task=null;slot.Packing=false;slot.Staff.IsWorking=false;slot.FreeSince=at;
        }
        for(int i=0;i<ovens.Length;i++)
        {
            var job=ovens[i];if(job==null || job.Worker!=PizzaWorker.Staff || job.OvenStartedAt+BakeDuration(job.Order)>t)continue;
            float at=job.OvenStartedAt+BakeDuration(job.Order);FinishOven(job,at);job.BakePoint=true;
            if(packSeconds<=0 && RackCount<rackCapacity)Pack(job); // no packing time: boxed as it leaves the oven

        }
    }
    private void Assign(float t)
    {
        if(clock.Phase!=DayPhase.Open)return;
        // Without packing time, staff pizzas waiting for rack space are boxed as soon as a slot frees; nobody has to stop work for it.
        if(packSeconds<=0)foreach(var job in jobs.Where(j=>j.Worker==PizzaWorker.Staff && j.Stage==PizzaStage.Baked).OrderBy(j=>j.TakenOutAt).ToArray()){if(RackCount>=rackCapacity)break;Pack(job);}
        foreach(var job in jobs.Where(j=>j.Worker==PizzaWorker.Player && j.Stage==PizzaStage.Baked).OrderBy(j=>j.TakenOutAt))
        {
            if(RackCount>=rackCapacity || PackingTable.Count()>=packingSlots)break;
            job.Stage=PizzaStage.Boxed;job.PackingStartedAt=t;
        }
        for(int i=0;i<ovens.Length;i++)
        {
            if(ovens[i]!=null)continue;
            var waiting=OvenQueue.FirstOrDefault();
            if(waiting!=null)StartOven(waiting,i,Mathf.Min(t,Mathf.Max(waiting.AssembledAt,ovenFreeSince[i])));
        }
        foreach(var slot in staff.Where(s=>s.Task==null))
        {
            var baked=jobs.Where(j=>j.Worker==PizzaWorker.Staff && j.Stage==PizzaStage.Baked).OrderBy(j=>j.TakenOutAt).FirstOrDefault();
            if(baked!=null && RackCount<rackCapacity)
            {
                float start=Mathf.Min(t,Mathf.Max(slot.FreeSince,baked.TakenOutAt));
                baked.Stage=PizzaStage.Boxed;slot.Task=baked;slot.Packing=true;slot.Until=start+StaffPackSeconds(slot.Staff);slot.Staff.IsWorking=true;continue;
            }
            int bench=FreeBench();if(bench<0)continue;
            var order=orders.Orders.Where(o=>o.Baking==BakingState.Waiting && o.SpawnedAt<=t && Expires(o)>t && !expired.Contains(o) && JobFor(o)==null).OrderBy(o=>o.SpawnedAt).FirstOrDefault();
            if(order==null)continue;
            float begin=Mathf.Min(t,Mathf.Max(order.SpawnedAt,Mathf.Max(slot.FreeSince,benchFreeSince[bench])));
            var job=new PizzaJob(order,PizzaWorker.Staff){Staff=slot.Staff,Bench=bench,Stage=PizzaStage.Topping,StartedAt=begin};
            jobs.Add(job);benches[bench]=job;order.Baking=BakingState.Cooking;
            slot.Task=job;slot.Packing=false;slot.Until=begin+StaffWorkSeconds(slot.Staff,job.Order);slot.Staff.IsWorking=true;
        }
    }
    private int FreeOven(){for(int i=0;i<ovens.Length;i++)if(ovens[i]==null)return i;return -1;}
    private int FreeBench(){for(int i=0;i<benches.Length;i++)if(benches[i]==null)return i;return -1;}
    private void QueueOven(PizzaJob job,float at)
    {
        job.AssembledAt=at;job.QueueSequence=++queueSequence;job.QueuedForOven=true;
        if(job.Bench>=0 && benches[job.Bench]==job){benches[job.Bench]=null;benchFreeSince[job.Bench]=at;}
        job.Bench=-1;
    }
    private void StartOven(PizzaJob job,int oven,float at)
    {
        job.QueuedForOven=false;
        if(job.Bench>=0 && job.Bench<benches.Length && benches[job.Bench]==job){benches[job.Bench]=null;benchFreeSince[job.Bench]=at;}
        job.Bench=-1;job.Oven=oven;ovens[oven]=job;job.OvenStartedAt=at;job.Stage=PizzaStage.Baking;
    }
    private void FinishOven(PizzaJob job,float at)
    {
        BusySeconds+=Mathf.Max(0,Mathf.Min(at,clock.BusinessDuration)-job.OvenStartedAt);
        if(job.Oven>=0 && job.Oven<ovens.Length && ovens[job.Oven]==job){ovens[job.Oven]=null;ovenFreeSince[job.Oven]=at;}
        job.Oven=-1;job.Stage=PizzaStage.Baked;job.TakenOutAt=at;
    }
    private void Pack(PizzaJob job)
    {
        job.Stage=PizzaStage.Packed;job.Order.Stars=job.Stars;job.Order.Baking=BakingState.Ready;
        if(job.Worker==PizzaWorker.Player)PackedByPlayer++;else{string name=job.Staff!=null?job.Staff.Name:"Staff";packedByStaff[name]=packedByStaff.TryGetValue(name,out int n)?n+1:1;}
    }
    private void Release(PizzaJob job,float at)
    {
        if(job.Stage==PizzaStage.Baking)BusySeconds+=Mathf.Max(0,Mathf.Min(at,clock.BusinessDuration)-job.OvenStartedAt);
        if(job.Bench>=0 && job.Bench<benches.Length && benches[job.Bench]==job){benches[job.Bench]=null;benchFreeSince[job.Bench]=at;}
        if(job.Oven>=0 && job.Oven<ovens.Length && ovens[job.Oven]==job){ovens[job.Oven]=null;ovenFreeSince[job.Oven]=at;}
        foreach(var slot in staff.Where(s=>s.Task==job)){slot.Task=null;slot.Packing=false;slot.Staff.IsWorking=false;slot.FreeSince=at;}
        jobs.Remove(job);
    }

    // ---- the player's hands ----
    private PizzaJob PlayerBench(int bench)=>BenchJob(bench) is PizzaJob job && job.Worker==PizzaWorker.Player?job:null;
    private bool Ready(){if(!ManualControlEnabled || clock.Phase!=DayPhase.Open)return false;AdvanceTo(clock.ElapsedSeconds);return true;}
    private bool Done(){AdvanceTo(clock.ElapsedSeconds);orders.RefreshCookingUI();return true;}
    public string ClaimRejection(Order order,int bench)
    {
        if(!ManualControlEnabled)return "いまは自分で調理できません。";
        if(clock.Phase!=DayPhase.Open)return "営業中だけ調理できます。";
        if(order==null || !orders.Contains(order) || order.Baking!=BakingState.Waiting || JobFor(order)!=null)return "この伝票はもう作り始めています。";
        if(clock.ElapsedSeconds>=Expires(order))return "期限が切れています。";
        if(bench<0 || bench>=benches.Length)return "作業台がありません。";
        if(benches[bench]!=null)return "この作業台は使用中です。";
        return "";
    }
    public bool Claim(Order order,int bench)
    {
        if(!Ready() || ClaimRejection(order,bench).Length>0)return false;
        var job=new PizzaJob(order,PizzaWorker.Player){Bench=bench,Stage=PizzaStage.Ticket,StartedAt=clock.ElapsedSeconds};
        jobs.Add(job);benches[bench]=job;order.Baking=BakingState.Cooking;return Done();
    }
    private bool Step(int bench,PizzaStage from,Action<PizzaJob> apply)
    {
        if(!Ready())return false;var job=PlayerBench(bench);if(job==null || job.Stage!=from)return false;apply(job);return Done();
    }
    public bool AddDough(int bench)=>Step(bench,PizzaStage.Ticket,j=>j.Stage=PizzaStage.Dough);
    public bool AddSauce(int bench,float coverage)=>Step(bench,PizzaStage.Dough,j=>{j.SauceCoverage=Mathf.Clamp01(coverage);j.SaucePoint=j.SauceCoverage>=sauceThreshold;j.Stage=PizzaStage.Sauce;});
    public bool AddCheese(int bench)=>Step(bench,PizzaStage.Sauce,j=>j.Stage=PizzaStage.Cheese);
    public bool AddTopping(int bench,Topping topping)=>Step(bench,PizzaStage.Cheese,j=>
    {
        j.AddedToppings.Add(topping);j.AddedTopping=j.AddedToppings[0];
        if(j.AddedToppings.Count>=j.Order.RequiredToppings.Length){j.ToppingPoint=j.AddedToppings.OrderBy(t=>t).SequenceEqual(j.Order.RequiredToppings.OrderBy(t=>t));j.Stage=PizzaStage.Topping;}
    });
    public bool PutInOven(int bench,int oven)
    {
        if(!Ready())return false;var job=PlayerBench(bench);
        if(job==null || job.Stage!=PizzaStage.Topping || oven<0 || oven>=ovens.Length)return false;
        QueueOven(job,clock.ElapsedSeconds);return Done();
    }
    public float OvenElapsed(int oven)=>OvenJob(oven) is PizzaJob job?clock.ElapsedSeconds-job.OvenStartedAt:0;
    public bool CanTakeOut(int oven)=>ManualControlEnabled && clock.Phase==DayPhase.Open && OvenJob(oven) is PizzaJob job && job.Worker==PizzaWorker.Player && OvenElapsed(oven)>=BandStartFor(job.Order);
    public bool TakeOut(int oven)
    {
        if(!Ready() || !CanTakeOut(oven))return false;
        var job=ovens[oven];float elapsed=OvenElapsed(oven);
        job.BakePoint=elapsed<=BandEndFor(job.Order);job.Burnt=elapsed>=BurnAtFor(job.Order);FinishOven(job,clock.ElapsedSeconds);return Done();
    }
    public bool Box(PizzaJob job)
    {
        if(!Ready() || job==null || job.Worker!=PizzaWorker.Player || job.Stage!=PizzaStage.Baked || RackCount>=rackCapacity || PackingTable.Count()>=packingSlots)return false;
        job.Stage=PizzaStage.Boxed;job.PackingStartedAt=clock.ElapsedSeconds;return Done();
    }
    public string CloseRejection(PizzaJob job)
    {
        if(job==null || job.Stage!=PizzaStage.Boxed || job.Worker!=PizzaWorker.Player)return "箱に入ったピザがありません。";
        if(clock.ElapsedSeconds<job.PackingStartedAt+packSeconds)return "自動で梱包中です。";
        return "";
    }
    public bool CloseBox(PizzaJob job)
    {
        if(!Ready() || CloseRejection(job).Length>0)return false;
        Pack(job);return Done();
    }
}
