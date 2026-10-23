using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class OrderSpawner : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private CookingManager cooking;
    [SerializeField] private DayStats stats;
    [SerializeField] private GameClock clock;
    [SerializeField] private MapManager map;
    [SerializeField] private OrderBook orders;
    [SerializeField] private ReputationManager reputation;
    [SerializeField, Min(0.1f)] private float interval = 10f;
    [SerializeField, Range(0f, 0.9f)] private float intervalVariation = 0.3f;
    [SerializeField, Min(0.01f)] private float initialPromiseSeconds = 60f;
    [SerializeField, Min(0.01f)] private float maximumPromiseSeconds = 240f;
    [SerializeField, Min(1f)] private float minimumPromiseMargin = 1.1f;
    [SerializeField, Min(0.01f)] private float referencePromiseSeconds = 60f;
    [SerializeField, Min(0f)] private float demandExponent = 0.7f;
    [SerializeField, Min(0.01f)] private float minimumDemand = 0.4f;
    [SerializeField, Min(0.01f)] private float maximumDemand = 2f;
    [Tooltip("Orders already waiting when the shop opens, so the first minute is not idle.")]
    [SerializeField, Min(0)] private int openingOrders = 3;
    private bool openingPlaced;
    [SerializeField] private bool useFixedSeed;
    [SerializeField] private int randomSeed = 9187;
    private System.Random random;
    public int? SeedOverride { get; set; }
    public void ResetRandom() => random=new System.Random(SeedOverride ?? (useFixedSeed ? randomSeed : System.Environment.TickCount));
    private float progress;
    private float jitter = 1;
    private bool scheduled;
    public event System.Action PolicyChanged;
    public float PromiseSeconds { get; private set; }
    public float MinimumPromiseSeconds { get; private set; }
    public float MaximumPromiseSeconds => Mathf.Max(maximumPromiseSeconds, MinimumPromiseSeconds);
    public float ReputationFactor => reputation != null ? reputation.DemandFactor : 1f;
    public float CityDemandFactor=>map.City==null?1:map.City.blocks.Average(b=>b.template.demand);
    public float ExpectedInterval => interval / (DemandFactor(PromiseSeconds) * ReputationFactor * CityDemandFactor * (facilities!=null?facilities.DemandMultiplier:1));

    private void Awake()
    {
        PromiseSeconds=initialPromiseSeconds;ResetRandom();
        // Policy bounds remain current while only automatic order generation is disabled.
        if(facilities!=null)facilities.Changed+=RefreshMinimumPromise;
    }
    private void OnDestroy()
    {
        if(facilities!=null)facilities.Changed-=RefreshMinimumPromise;
    }
    private void OnEnable()
    {
        clock.OnDayStarted += ScheduleFirst;
        clock.Advanced += Tick;
        map.MapReady += MapReady;
        RefreshMinimumPromise();
    }
    private void OnDisable()
    {
        clock.OnDayStarted -= ScheduleFirst;
        clock.Advanced -= Tick;
        map.MapReady -= MapReady;
    }
    private void Start() { if (map.IsReady) MapReady(); }
    private void MapReady()
    {
        RefreshMinimumPromise();
        if (clock.Phase == DayPhase.Open) ScheduleFirst();
    }
    public void RefreshMinimumPromise()
    {
        if(!map.IsReady)return;
        MinimumPromiseSeconds = map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore)
            .Select(n=>ShortestRoundTripSeconds(n.Id)).DefaultIfEmpty(0).Max() * minimumPromiseMargin + (cooking!=null?cooking.CookSeconds:0);
        SetPromiseSeconds(PromiseSeconds);
    }
    public void ResetForDay() { progress=0; jitter=1; scheduled=false; openingPlaced=false; }
    public void ResetPolicy() => SetPromiseSeconds(initialPromiseSeconds);
    public void SetPromiseSeconds(float seconds)
    {
        float next=Mathf.Clamp(seconds,Mathf.Max(.01f,MinimumPromiseSeconds),MaximumPromiseSeconds);
        if(next==PromiseSeconds)return;PromiseSeconds=next;PolicyChanged?.Invoke();
    }
    public float DemandFactor(float seconds)
        => Mathf.Clamp(Mathf.Pow(referencePromiseSeconds/Mathf.Max(0.01f,seconds),demandExponent),minimumDemand,maximumDemand);
    public float ShortestArrivalSeconds(string destination)
    {
        var perfect = new DeliveryPerson("Reference",1,1,2,1,1,1);
        var path = RoadPathfinder.FindShortestPath(map.Edges,map.StoreNodeId,destination,e=>map.GetTravelTime(e.Id,perfect));
        if (path==null) throw new System.InvalidOperationException("Unreachable delivery address: "+destination);
        return clock.ToRealSeconds(path.Sum(id=>map.GetTravelTime(id,perfect)));
    }
    public float ShortestRoundTripSeconds(string destination)
    {
        var perfect=new DeliveryPerson("Reference",1,1,2,1,1,1);
        var outbound=RoadPathfinder.FindShortestPath(map.Edges,map.StoreNodeId,destination,e=>map.GetTravelTime(e.Id,perfect));
        var inbound=RoadPathfinder.FindShortestPath(map.Edges,destination,map.StoreNodeId,e=>map.GetTravelTime(e.Id,perfect));
        if(outbound==null || inbound==null)throw new System.InvalidOperationException("Unreachable round trip: "+destination);
        return clock.ToRealSeconds(outbound.Concat(inbound).Sum(id=>map.GetTravelTime(id,perfect)));
    }
    public Order CreateOrder(string destination, float spawnedAt)
        {
        if(random==null)ResetRandom();
        int unlocked=facilities!=null?facilities.MenuUpgrades:0;
        var district=map.City?.DistrictFor(destination);var weights=district?.pizzaWeights??Vector3.one;
        float total=weights.x+(unlocked>=1?weights.y:0)+(unlocked>=2?weights.z:0);double roll=random.NextDouble()*Mathf.Max(.01f,total);
        PizzaKind kind=roll<weights.x?PizzaKind.Margherita:unlocked>=2 && roll>=weights.x+weights.y?PizzaKind.Seafood:PizzaKind.Pepperoni;
        if(!System.Enum.IsDefined(typeof(PizzaKind),kind) || (int)kind>unlocked)kind=PizzaKind.Margherita;
        return new Order(destination,"Delivery to "+destination,spawnedAt,PromiseSeconds,kind:kind);
    }
    private string PickDestination(MapNode[] destinations)
    {
        float Weight(MapNode node)=>Mathf.Max(.01f,map.City?.DistrictFor(node.Id)?.demand??1);
        double roll=random.NextDouble()*destinations.Sum(Weight);
        foreach(var node in destinations){roll-=Weight(node);if(roll<=0)return node.Id;}return destinations[destinations.Length-1].Id;
    }
    private void NextJitter()
    {
        if(random==null)ResetRandom();
        jitter=Mathf.Lerp(1f-intervalVariation,1f+intervalVariation,(float)random.NextDouble());
    }
    private void ScheduleFirst()
    {
        if (scheduled || !map.IsReady) return;
        progress=0;NextJitter();scheduled=true;openingPlaced=false;
    }
    private void Tick(float before, float now)
    {
        if (!scheduled || clock.Phase != DayPhase.Open || !map.IsReady) return;
        var destinations = map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore).ToArray();
        if (destinations.Length==0) return;
        if (!openingPlaced)
        {
            openingPlaced=true;
            for (int i=0;i<openingOrders;i++) { orders.AddOrder(CreateOrder(PickDestination(destinations),before)); stats?.RecordSpawn(); }
        }
        float cursor=before;
        // Preserve progress through the current interval when the policy changes.
        while (cursor < now)
        {
            float duration=Mathf.Max(0.1f,ExpectedInterval*jitter);
            float untilSpawn=(1-progress)*duration;
            if (untilSpawn>now-cursor) {progress+=(now-cursor)/duration;break;}
            cursor+=untilSpawn;
            if(cursor>=clock.BusinessDuration)break;
            orders.AddOrder(CreateOrder(PickDestination(destinations),cursor));
            stats?.RecordSpawn();
            progress=0;NextJitter();
        }
    }
}
