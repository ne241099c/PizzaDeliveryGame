using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-50)]
public sealed class OrderSpawner : MonoBehaviour
{
    [SerializeField] private DayStats stats;
    [SerializeField] private GameClock clock;
    [SerializeField] private MapManager map;
    [SerializeField] private OrderListUI orders;
    [SerializeField] private ReputationManager reputation;
    [SerializeField, Min(0.1f)] private float interval = 25f;
    [SerializeField, Range(0f, 0.9f)] private float intervalVariation = 0.3f;
    [SerializeField, Min(0.01f)] private float initialPromiseSeconds = 60f;
    [SerializeField, Min(0.01f)] private float maximumPromiseSeconds = 240f;
    [SerializeField, Min(1f)] private float minimumPromiseMargin = 1.1f;
    [SerializeField, Min(0.01f)] private float referencePromiseSeconds = 60f;
    [SerializeField, Min(0f)] private float demandExponent = 0.7f;
    [SerializeField, Min(0.01f)] private float minimumDemand = 0.4f;
    [SerializeField, Min(0.01f)] private float maximumDemand = 2f;
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
    public float ExpectedInterval => interval / (DemandFactor(PromiseSeconds) * ReputationFactor);

    private void Awake() { PromiseSeconds=initialPromiseSeconds;ResetRandom(); }
    private void OnEnable()
    {
        clock.OnDayStarted += ScheduleFirst;
        clock.Advanced += Tick;
        map.MapReady += MapReady;
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
        MinimumPromiseSeconds = map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore)
            .Select(n=>ShortestArrivalSeconds(n.Id)).DefaultIfEmpty(0).Max() * minimumPromiseMargin;
        SetPromiseSeconds(PromiseSeconds);
        if (clock.Phase == DayPhase.Open) ScheduleFirst();
    }
    public void ResetForDay() { progress=0; jitter=1; scheduled=false; }
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
    public Order CreateOrder(string destination, float spawnedAt)
        => new Order(destination,"Delivery to "+destination,spawnedAt,PromiseSeconds);
    private void NextJitter() { if(random==null)ResetRandom();jitter=Mathf.Lerp(1f-intervalVariation,1f+intervalVariation,(float)random.NextDouble()); }
    private void ScheduleFirst()
    {
        if (scheduled || !map.IsReady) return;
        progress=0;NextJitter();scheduled=true;
    }
    private void Tick(float before, float now)
    {
        if (!scheduled || clock.Phase != DayPhase.Open || !map.IsReady) return;
        var destinations = map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore).ToArray();
        if (destinations.Length==0) return;
        float cursor=before;
        // Preserve progress through the current interval when the policy changes.
        while (cursor < now)
        {
            float duration=Mathf.Max(0.1f,ExpectedInterval*jitter);
            float untilSpawn=(1-progress)*duration;
            if (untilSpawn>now-cursor) {progress+=(now-cursor)/duration;break;}
            cursor+=untilSpawn;
            if(cursor>=clock.BusinessDuration)break;
            orders.AddOrder(CreateOrder(destinations[random.Next(destinations.Length)].Id,cursor));
            stats?.RecordSpawn();
            progress=0;NextJitter();
        }
    }
}
