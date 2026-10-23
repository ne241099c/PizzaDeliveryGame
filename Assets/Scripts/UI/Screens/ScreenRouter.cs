using System;
using System.Collections;
using UnityEngine;

/// <summary>The screens of the Game scene. The title is its own scene (GameScenes).</summary>
public enum ScreenId { Hub, Business, Settlement, GameOver }

/// <summary>Observes campaign events and owns presentation only.</summary>
[DefaultExecutionOrder(100)]
public sealed class ScreenRouter : MonoBehaviour
{
    [SerializeField] private CampaignManager campaign;
    [SerializeField] private GameClock clock;
    [SerializeField] private CanvasGroup[] screens;
    [SerializeField] private CanvasGroup sharedHeader;
    [SerializeField, Min(0)] private float fadeSeconds = .3f;
    [SerializeField] private int implementationStage = 1;
    public int ImplementationStage => implementationStage;
    public ScreenId Current { get; private set; }
    public event Action<ScreenId> Changed;
    private Coroutine fade;
    private void OnEnable()
    {
        campaign.PreparationEntered += OnPreparation;
        clock.OnDayStarted += OnBusiness;
        clock.OnDayEnded += OnSettlement;
    }
    private void OnDisable()
    {
        campaign.PreparationEntered -= OnPreparation;
        clock.OnDayStarted -= OnBusiness;
        clock.OnDayEnded -= OnSettlement;
    }
    private void Start() => Show(Current, true); // the campaign has already entered preparation (or restored a save)
    private void OnPreparation() => Show(ScreenId.Hub);
    private void OnBusiness() => Show(ScreenId.Business);
    private void OnSettlement() => Show(ScreenId.Settlement);
    public CanvasGroup Root(ScreenId id) => screens[(int)id];
    public void Show(ScreenId id, bool immediate = false)
    {
        if (fade != null) StopCoroutine(fade);
        Current = id;
        foreach (var group in screens) UIVisibility.Set(group.gameObject, false);
        UIVisibility.Set(sharedHeader.gameObject, id == ScreenId.Hub || id == ScreenId.Business || id == ScreenId.Settlement);
        var target = Root(id);
        UIVisibility.Set(target.gameObject, true);
        if (!immediate && fadeSeconds > 0 && Application.isPlaying) fade = StartCoroutine(Fade(target));
        Changed?.Invoke(id);
    }
    private IEnumerator Fade(CanvasGroup group)
    {
        group.alpha = 0;
        for (float elapsed = 0; elapsed < fadeSeconds; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Clamp01(elapsed / fadeSeconds);
            yield return null;
        }
        group.alpha = 1; fade = null;
    }
}
