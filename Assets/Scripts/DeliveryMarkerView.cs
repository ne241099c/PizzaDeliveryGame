using TMPro;
using UnityEngine;

public sealed class DeliveryMarkerView : MonoBehaviour
{
    [SerializeField] private DeliveryFlowController flow;
    [SerializeField] private GameClock clock;
    [SerializeField] private Transform markersParent;
    [SerializeField] private float markerOffset = 0.2f;
    [SerializeField] private float markerFontSize = 5.5f;
    private readonly System.Collections.Generic.Dictionary<DeliveryTrip, TextMeshPro> markers = new();
    private void OnEnable() { flow.Dispatched += Add; flow.Returned += Remove; flow.TripsCleared += Clear; }
    private void OnDisable() { flow.Dispatched -= Add; flow.Returned -= Remove; flow.TripsCleared -= Clear; }
    private void Clear()
    {
        foreach(var marker in markers.Values)if(marker!=null)Destroy(marker.gameObject);
        markers.Clear();
    }
    private void Add(DeliveryTrip trip)
    {
        var go = new GameObject("Courier_" + trip.Person.Name);
        go.transform.SetParent(markersParent, false);
        var text = go.AddComponent<TextMeshPro>();
        text.font = UITheme.Active.font;
        var dot=new GameObject("CourierDot");dot.transform.SetParent(go.transform,false);dot.transform.localScale=Vector3.one*UITheme.Active.worldIconScale;var sprite=dot.AddComponent<SpriteRenderer>();sprite.sprite=MapVisuals.Shape(true);sprite.color=UITheme.Active.CourierColor(trip.Person.Name);sprite.sortingOrder=29;
        text.text = MarkerCaption(trip,clock.ElapsedSeconds);
        var headingObject=new GameObject("Heading");headingObject.transform.SetParent(go.transform,false);var heading=headingObject.AddComponent<TextMeshPro>();heading.font=UITheme.Active.font;heading.text=">";heading.fontSize=UITheme.Active.routeArrowSize;heading.color=UITheme.Active.CourierColor(trip.Person.Name);heading.alignment=TextAlignmentOptions.Center;heading.rectTransform.sizeDelta=Vector2.one;headingObject.GetComponent<MeshRenderer>().sortingOrder=31;
        text.fontSize = UITheme.Active.worldLabelSize;
        text.color = ColorFor(trip,clock.ElapsedSeconds);
        text.alignment = TextAlignmentOptions.Center;
        text.rectTransform.sizeDelta = new Vector2(2.2f, 1.2f);
        go.GetComponent<MeshRenderer>().sortingOrder = 30;
        markers.Add(trip, text);
        go.transform.position = (Vector3)trip.PositionAt(clock.ElapsedSeconds) + Vector3.up * markerOffset;
    }
    private void Remove(DeliveryTrip trip)
    {
        if (!markers.TryGetValue(trip, out TextMeshPro marker)) return;
        Destroy(marker.gameObject);
        markers.Remove(trip);
    }
    private void Update()
    {
        foreach (var pair in markers)
        {
            pair.Value.transform.position = (Vector3)pair.Key.PositionAt(clock.ElapsedSeconds) + Vector3.up * markerOffset;
            pair.Value.text=MarkerCaption(pair.Key,clock.ElapsedSeconds);
            pair.Value.color=ColorFor(pair.Key,clock.ElapsedSeconds);
            Vector2 direction=pair.Key.PositionAt(clock.ElapsedSeconds+.1f)-pair.Key.PositionAt(clock.ElapsedSeconds);var heading=pair.Value.transform.Find("Heading");
            if(direction.sqrMagnitude>0){heading.localPosition=direction.normalized*UITheme.Active.worldIconScale;heading.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);}
        }
    }
    public static string MarkerCaption(DeliveryTrip trip,float now)
    {
        string initial=string.IsNullOrWhiteSpace(trip.Person.Name)?"?":trip.Person.Name.Substring(0,1);
        string caption="<b>["+initial+"]</b>";
        return trip.IsStoppedAt(now)?caption+$"\nSTOP {trip.StoppedUntil(now)-now:F0}s":caption;
    }
    private static Color ColorFor(DeliveryTrip trip,float now)
        => trip.IsStoppedAt(now)?UITheme.Active.bad:UITheme.Active.background;
}
