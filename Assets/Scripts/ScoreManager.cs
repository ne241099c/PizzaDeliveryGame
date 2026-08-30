using TMPro;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private int scorePerDelivery = 100;

    private int score;

    private void Start()
    {
        UpdateScoreText();
    }

    public void AddScore(int amount)
    {
        score += amount;
        UpdateScoreText();
    }

    public void OnDeliveryCompleted()
    {
        AddScore(scorePerDelivery);
    }

    private void UpdateScoreText()
    {
        if (scoreText == null)
        {
            return;
        }

        scoreText.text = $"Score: {score}";
    }
}
