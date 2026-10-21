using System;
using UnityEngine;

public enum DayPhase { BeforeOpening, Open, Closed, Ended }

/// <summary>The only source of advancing time for orders, trips, deadlines and UI.</summary>
[DefaultExecutionOrder(-100)]
public sealed class GameClock : MonoBehaviour
{
    [SerializeField, Min(1f)] private float businessDuration = 300f;
    [SerializeField, Min(0.01f)] private float travelTimeMultiplier = 5f;
    [SerializeField, Range(0.05f, 1f)] private float planningTimeRate = 1f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool automaticTick = true;

    public DayPhase Phase { get; private set; } = DayPhase.BeforeOpening;
    public float ElapsedSeconds { get; private set; }
    public float RemainingSeconds => Mathf.Max(0f, businessDuration - ElapsedSeconds);
    public float BusinessDuration => businessDuration;
    public float TravelTimeMultiplier => travelTimeMultiplier;
    public bool IsPlanning { get; set; }
    public bool AutomaticTick { get => automaticTick; set => automaticTick = value; }
    public void PrepareDay()
    {
        Phase = DayPhase.BeforeOpening; ElapsedSeconds = 0; IsPlanning = false;
    }
    public event Action OnDayStarted;
    public event Action OnClosed;
    public event Action OnDayEnded;
    public event Action<float, float> Advanced;

    private void Start() { if (autoStart) BeginDay(); }
    private void Update() { if (automaticTick) Advance(Time.unscaledDeltaTime); }
    public float ToRealSeconds(float mapTime) => mapTime * travelTimeMultiplier;

    public void BeginDay()
    {
        if (Phase != DayPhase.BeforeOpening) return;
        ElapsedSeconds = 0;
        Phase = DayPhase.Open;
        OnDayStarted?.Invoke();
    }

    // Also used by deterministic validation. Split the closing boundary so no event is skipped.
    public void Advance(float realSeconds)
    {
        if (realSeconds < 0 || float.IsNaN(realSeconds) || float.IsInfinity(realSeconds))
            throw new ArgumentOutOfRangeException(nameof(realSeconds));
        float remaining = realSeconds * (IsPlanning ? planningTimeRate : 1f);
        if (Phase == DayPhase.Open)
        {
            float step = Mathf.Min(remaining, RemainingSeconds);
            Tick(step);
            remaining -= step;
            if (RemainingSeconds <= 0f)
            {
                Phase = DayPhase.Closed;
                OnClosed?.Invoke();
            }
        }
        if (Phase == DayPhase.Closed && remaining > 0) Tick(remaining);
    }

    private void Tick(float seconds)
    {
        if (seconds <= 0) return;
        float before = ElapsedSeconds;
        ElapsedSeconds += seconds;
        Advanced?.Invoke(before, ElapsedSeconds);
    }

    public void FinishDay()
    {
        if (Phase != DayPhase.Closed) return;
        Phase = DayPhase.Ended;
        OnDayEnded?.Invoke();
    }
}
