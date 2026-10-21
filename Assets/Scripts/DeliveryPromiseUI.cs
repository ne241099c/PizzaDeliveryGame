using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DeliveryPromiseUI : MonoBehaviour
{
    [SerializeField] private OrderSpawner spawner;
    [SerializeField] private MapManager map;
    [SerializeField] private GameClock clock;
    [SerializeField] private Slider promiseSlider;
    [SerializeField] private TextMeshProUGUI promiseText;
    [SerializeField] private TextMeshProUGUI demandText;
    [SerializeField] private TextMeshProUGUI referenceText;
    [SerializeField] private ReputationManager reputation;
    [SerializeField] private TextMeshProUGUI reputationText;
    private bool ready;
    private void Start()
    {
        map.MapReady+=Configure;
        promiseSlider.onValueChanged.AddListener(SetPromise);
        if(map.IsReady)Configure();
    }
    private void OnDestroy() {map.MapReady-=Configure;promiseSlider.onValueChanged.RemoveListener(SetPromise);}
    private void Configure()
    {
        promiseSlider.minValue=spawner.MinimumPromiseSeconds;
        promiseSlider.maxValue=spawner.MaximumPromiseSeconds;
        promiseSlider.SetValueWithoutNotify(spawner.PromiseSeconds);
        referenceText.text="Fastest arrival (reference): "+string.Join(" / ",map.Nodes.Where(n=>n.NodeType==NodeType.DeliveryAddress && !n.IsStore)
            .Select(n=>$"{n.Id} {spawner.ShortestArrivalSeconds(n.Id):F1}s"));
        ready=true;
    }
    private void SetPromise(float seconds) => spawner.SetPromiseSeconds(seconds);
    private void Update()
    {
        if(!ready)return;
        promiseSlider.interactable=clock.Phase==DayPhase.Open || clock.Phase==DayPhase.BeforeOpening;
        promiseSlider.SetValueWithoutNotify(spawner.PromiseSeconds);
        promiseText.text=$"Delivery promise: {spawner.PromiseSeconds:F1}s (min {spawner.MinimumPromiseSeconds:F1}s)";
        reputationText.text=$"Reputation: {reputation.Value:F1} / Orders x{reputation.DemandFactor:F2}";
        demandText.text=$"Demand x{spawner.DemandFactor(spawner.PromiseSeconds):F2} / about {spawner.ExpectedInterval:F1}s per order";
    }
}
