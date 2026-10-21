using UnityEngine;
using TMPro;

/// <summary>Only confirmed arrival revenue credits cash; daily scores may also contain penalties.</summary>
public sealed class BusinessEconomy : MonoBehaviour
{
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private TextMeshProUGUI cashText;
    [SerializeField, Min(0)] private float initialCash = 300f;
    public decimal Cash { get; private set; }
    public decimal DailyRevenue { get; private set; }
    public decimal OpeningCash { get; private set; }
    private void Awake() => NewGame();
    private void OnEnable() => flow.RevenueEarned += CreditArrival;
    private void OnDisable() => flow.RevenueEarned -= CreditArrival;
    private void Update() => cashText.text=$"Cash: {Cash:F2} / Sales: {DailyRevenue:F2}";
    private void CreditArrival(int points) { Cash+=points; DailyRevenue+=points; }
    public void NewGame() { Cash=(decimal)initialCash; PrepareDay(); }
    public void PrepareDay() { DailyRevenue=0; OpeningCash=Cash; }
}
