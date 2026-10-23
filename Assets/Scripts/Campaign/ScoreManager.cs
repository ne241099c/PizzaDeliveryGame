using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    [SerializeField, Min(1)] private int scorePerDelivery = 100;
    [SerializeField, Min(1)] private int pepperoniPoints=120,seafoodPoints=150;
    public int BasePoints(Order order)=>order.Kind==PizzaKind.Seafood?seafoodPoints:order.Kind==PizzaKind.Pepperoni?pepperoniPoints:scorePerDelivery;
    public int ScoreArrival(Order order,float arrivedAt,float service=0)=>DeliveryScoring.Calculate(arrivedAt,order.Deadline,BasePoints(order),latePenaltyPerSecond,service,tipRatio);
    public int TipForArrival(Order order,float arrivedAt,float service)=>arrivedAt<=order.Deadline?(int)System.Math.Round(BasePoints(order)*tipRatio*Mathf.Clamp01(service),System.MidpointRounding.AwayFromZero):0;
    [SerializeField, Min(0f)] private float latePenaltyPerSecond = 5f;
    [SerializeField, Min(0)] private int closingCancellationPenalty = 0;
    [SerializeField, Range(0,1)] private float tipRatio = 0.2f;
    [SerializeField, Min(0f), Tooltip("目安を過ぎても未出発の注文を受け付ける秒数。得点の減点は目安時刻から始まります。") ] private float lateAcceptanceSeconds = 20f;
    public float LateAcceptanceSeconds => lateAcceptanceSeconds;
    public int Score { get; private set; }
    public int ScorePerDelivery => scorePerDelivery;
    public float LatePenaltyPerSecond => latePenaltyPerSecond;
    public int ClosingCancellationPenalty => closingCancellationPenalty;
    public void ResetForDay() => Score=0;
    public void AddScore(int amount) => Score+=amount;
    public int ScoreArrival(float arrivedAt, float deadline, float service = 0)
        => DeliveryScoring.Calculate(arrivedAt, deadline, scorePerDelivery, latePenaltyPerSecond,service,tipRatio);
    public int TipForArrival(float arrivedAt,float deadline,float service)
        => arrivedAt<=deadline ? (int)System.Math.Round(scorePerDelivery*tipRatio*Mathf.Clamp01(service),System.MidpointRounding.AwayFromZero) : 0;
    public float CancellationAt(Order order) => DeliveryScoring.CancellationAt(order.Deadline, scorePerDelivery, latePenaltyPerSecond, lateAcceptanceSeconds);
}
