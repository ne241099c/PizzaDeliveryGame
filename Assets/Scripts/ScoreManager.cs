using TMPro;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField, Min(1)] private int scorePerDelivery = 100;
    [SerializeField, Min(0f)] private float latePenaltyPerSecond = 5f;
    [SerializeField, Min(0)] private int closingCancellationPenalty = 0;
    public int Score { get; private set; }
    public int ScorePerDelivery => scorePerDelivery;
    public float LatePenaltyPerSecond => latePenaltyPerSecond;
    public int ClosingCancellationPenalty => closingCancellationPenalty;
    private void Start() => UpdateScoreText();
    public void AddScore(int amount) { Score += amount; UpdateScoreText(); }
    public int ScoreArrival(float arrivedAt, float deadline) => DeliveryScoring.Calculate(arrivedAt, deadline, scorePerDelivery, latePenaltyPerSecond);
    public float CancellationAt(Order order) => DeliveryScoring.CancellationAt(order.Deadline, scorePerDelivery, latePenaltyPerSecond);
    private void UpdateScoreText() { if (scoreText != null) scoreText.text = $"Score: {Score}"; }
}
