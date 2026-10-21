using TMPro;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField, Min(1)] private int scorePerDelivery = 100;
    [SerializeField, Min(0f)] private float latePenaltyPerSecond = 5f;
    [SerializeField, Min(0)] private int closingCancellationPenalty = 0;
    [SerializeField, Range(0,1)] private float tipRatio = 0.2f;
    public int Score { get; private set; }
    public int ScorePerDelivery => scorePerDelivery;
    public float LatePenaltyPerSecond => latePenaltyPerSecond;
    public int ClosingCancellationPenalty => closingCancellationPenalty;
    private void Start() => UpdateScoreText();
    public void ResetForDay() { Score=0; UpdateScoreText(); }
    public void AddScore(int amount) { Score += amount; UpdateScoreText(); }
    public int ScoreArrival(float arrivedAt, float deadline, float service = 0)
        => DeliveryScoring.Calculate(arrivedAt, deadline, scorePerDelivery, latePenaltyPerSecond,service,tipRatio);
    public int TipForArrival(float arrivedAt,float deadline,float service)
        => arrivedAt<=deadline ? (int)System.Math.Round(scorePerDelivery*tipRatio*Mathf.Clamp01(service),System.MidpointRounding.AwayFromZero) : 0;
    public float CancellationAt(Order order) => DeliveryScoring.CancellationAt(order.Deadline, scorePerDelivery, latePenaltyPerSecond);
    private void UpdateScoreText() { if (scoreText != null) scoreText.text = $"Score: {Score}"; }
}
