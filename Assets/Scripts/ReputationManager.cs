using UnityEngine;

public sealed class ReputationManager : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximumReputation = 100f;
    [SerializeField, Min(0f)] private float initialReputation = 50f;
    [SerializeField, Min(0f)] private float onTimeGain = 1f;
    [SerializeField, Min(0f)] private float lateBaseLoss = 1f;
    [SerializeField, Min(0f)] private float lateLossPerSecond = 0.1f;
    [SerializeField, Min(0f)] private float maximumLateLoss = 5f;
    [SerializeField, Range(0,1)] private float serviceProtection = 0.5f;
    [SerializeField, Min(0f)] private float cancellationLoss = 5f;
    [SerializeField, Min(0f)] private float closingCancellationLoss = 0f;
    [SerializeField, Min(0.01f)] private float demandBase = 0.6f;
    [SerializeField, Min(0f)] private float demandScale = 0.8f;
    public float Value { get; private set; }
    public float DemandFactor => demandBase + demandScale * Value / maximumReputation;
    private void Awake() => ResetForDay();
    public void ResetForDay() => Value=Mathf.Clamp(initialReputation,0,maximumReputation);
    public float ArrivalChange(float lateSeconds,float service)
        => lateSeconds<=0 ? onTimeGain : -Mathf.Min(maximumLateLoss,lateBaseLoss+lateSeconds*lateLossPerSecond)
            * (1-serviceProtection*Mathf.Clamp01(service));
    public void RecordArrival(float lateSeconds,float service) => Apply(ArrivalChange(lateSeconds,service));
    public void RecordCancellation(int count, bool closing = false) => Apply(-(closing ? closingCancellationLoss : cancellationLoss)*count);
    private void Apply(float delta) => Value=Mathf.Clamp(Value+delta,0,maximumReputation);
}
