using System;
using UnityEngine;

public enum DayPhase { BeforeOpening, Open, Closed, Ended }

/// <summary>The only source of advancing time for orders, trips, deadlines and UI.</summary>
[DefaultExecutionOrder(-100)]
public sealed class GameClock : MonoBehaviour
{
    [SerializeField, Min(1f)] private float businessDuration = 180f;
    [SerializeField, Min(0.01f)] private float travelTimeMultiplier = 5f;
    [SerializeField, Range(0.01f, 1f), Tooltip("配達指示画面を開いている間の進行倍率。調理・配達・期限を同じ倍率で進めます。") ] private float planningTimeRate = .1f;
    [SerializeField] private bool autoStart = true;
    [SerializeField] private bool automaticTick = true;

    [SerializeField, Min(1f), Tooltip("閉店後の帰還待ちの進行倍率。営業中とメニュー停止には影響しません。") ] private float closedPlaybackSpeed=8f;
    public float ClosedPlaybackSpeed=>Mathf.Max(1f,closedPlaybackSpeed);
    public DayPhase Phase { get; private set; } = DayPhase.BeforeOpening;
    public float ElapsedSeconds { get; private set; }
    public float RemainingSeconds => Mathf.Max(0f, businessDuration - ElapsedSeconds);
    public float BusinessDuration => businessDuration;
    public float TravelTimeMultiplier => travelTimeMultiplier;
    public bool IsPlanning { get; set; }
    public float PlanningTimeRate=>Mathf.Clamp(planningTimeRate,.01f,1f);
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
    private void Update() { if (automaticTick) AdvanceAutomatically(Time.unscaledDeltaTime); }
    public float ToRealSeconds(float mapTime) => mapTime * travelTimeMultiplier;

    public void BeginDay()
    {
        if (Phase != DayPhase.BeforeOpening) return;
        ElapsedSeconds = 0;
        Phase = DayPhase.Open;
        OnDayStarted?.Invoke();
    }

    /// <summary>Advance wall time. Dispatch uses the shared planning rate; only the closed part is accelerated.</summary>
    public void AdvanceAutomatically(float realSeconds)
    {
        if(realSeconds<0 || float.IsNaN(realSeconds) || float.IsInfinity(realSeconds))throw new ArgumentOutOfRangeException(nameof(realSeconds));
        if(Phase==DayPhase.Open)
        {
            float openWall=Mathf.Min(realSeconds,RemainingSeconds/(IsPlanning?PlanningTimeRate:1f));
            Advance(openWall);realSeconds=Mathf.Max(0,realSeconds-openWall);
        }
        if(Phase==DayPhase.Closed && realSeconds>0)Advance(realSeconds*ClosedPlaybackSpeed);
    }

    // Also used by deterministic validation. Split the closing boundary so no event is skipped.
    public void Advance(float realSeconds)
    {
        if (realSeconds < 0 || float.IsNaN(realSeconds) || float.IsInfinity(realSeconds))
            throw new ArgumentOutOfRangeException(nameof(realSeconds));
        float remaining = realSeconds * (Phase==DayPhase.Open && IsPlanning ? PlanningTimeRate : 1f);
        if (Phase == DayPhase.Open)
        {
            float step = Mathf.Min(remaining, RemainingSeconds);
            Tick(step);
            remaining -= step;
            if (RemainingSeconds <= 0f)
            {
                Phase = DayPhase.Closed;
                IsPlanning = false;
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
