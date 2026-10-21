using TMPro;
using UnityEngine;

public sealed class DeliveryMarkerView : MonoBehaviour
{
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private GameClock clock;
    [SerializeField] private Transform markersParent;
    private readonly System.Collections.Generic.Dictionary<DeliveryTrip, GameObject> markers = new();
    private void OnEnable() { flow.Dispatched += Add; flow.Returned += Remove; }
    private void OnDisable() { flow.Dispatched -= Add; flow.Returned -= Remove; }
    private void Add(DeliveryTrip trip)
    {
        var go = new GameObject("Courier_" + trip.Person.Name);
        go.transform.SetParent(markersParent, false);
        var text = go.AddComponent<TextMeshPro>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = "<b>[" + trip.Person.Name.Substring(0, 1) + "]</b>";
        text.fontSize = 5.5f;
        text.color = trip.Person.SkillLevel > 0.8f ? Color.cyan : trip.Person.SkillLevel > 0.5f ? Color.yellow : new Color(1, 0.5f, 0.3f);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(1.5f, 0.7f);
        go.GetComponent<MeshRenderer>().sortingOrder = 30;
        markers.Add(trip, go);
        go.transform.position = (Vector3)trip.PositionAt(clock.ElapsedSeconds) + Vector3.up * 0.2f;
    }
    private void Remove(DeliveryTrip trip)
    {
        if (!markers.TryGetValue(trip, out GameObject marker)) return;
        Destroy(marker);
        markers.Remove(trip);
    }
    private void Update()
    {
        foreach (var pair in markers)
            pair.Value.transform.position = (Vector3)pair.Key.PositionAt(clock.ElapsedSeconds) + Vector3.up * 0.2f;
    }
}
