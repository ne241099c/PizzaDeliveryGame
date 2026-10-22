using UnityEngine;

public class EdgeView : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private EdgeCollider2D edgeCollider;
    private MapEdge road;
    private LineRenderer border;
    private bool styledUsed,styledAvailable,styledLast;
    private float styledPixelScale;
    private bool directionalView;
    private void LateUpdate()
    {
        var cam=Camera.main;if(road==null || cam==null)return;
        float scale=cam.orthographicSize*2/Mathf.Max(1,cam.pixelHeight);
        if(!Mathf.Approximately(scale,styledPixelScale) || directionalView!=BusinessView.HideMapArrows)SetRouteStyle(styledUsed,styledAvailable,styledLast);
    }
    public string EdgeId { get; private set; }

    public void Initialize(MapEdge edge, MapNode nodeA, MapNode nodeB, MapManager mapManager, bool isActive)
    {
        road=edge;EdgeId = edge.Id;
        gameObject.name = "Edge_" + edge.Id;
        Vector3 start = nodeA.Position;
        Vector3 end = nodeB.Position;
        if (edge.RoadType == RoadType.Backstreet)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 offset = new Vector3(-direction.y, direction.x, 0) * 0.35f;
            start += offset;
            end += offset;
        }
        transform.position = start;
        Vector3 localEnd = end - start;
        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, localEnd);
        edgeCollider.points = new Vector2[] { Vector2.zero, localEnd };
        edgeCollider.enabled = true;
        edgeCollider.edgeRadius = 0.12f;
        var borderObject=new GameObject("RoadBorder");borderObject.transform.SetParent(transform,false);border=borderObject.AddComponent<LineRenderer>();border.sharedMaterial=lineRenderer.sharedMaterial;border.useWorldSpace=false;border.positionCount=2;border.SetPosition(0,Vector3.zero);border.SetPosition(1,localEnd);border.sortingOrder=-1;
        if(!edge.Bidirectional)
        {
            var go=new GameObject("OneWayArrow");go.transform.SetParent(transform,false);go.transform.localPosition=localEnd*.5f;go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(localEnd.y,localEnd.x)*Mathf.Rad2Deg);var text=go.AddComponent<TMPro.TextMeshPro>();text.font=UITheme.Active.font;text.text=">";text.fontSize=UITheme.Active.routeArrowSize;text.color=UITheme.Active.ink;text.alignment=TMPro.TextAlignmentOptions.Center;text.rectTransform.sizeDelta=Vector2.one;go.GetComponent<MeshRenderer>().sortingOrder=8;
        }
        SetRouteStyle(false, false, false);
    }

    public float DistanceToScreenPoint(Camera camera, Vector2 point)
    {
        Vector2 a = camera.WorldToScreenPoint(transform.TransformPoint(lineRenderer.GetPosition(0)));
        Vector2 b = camera.WorldToScreenPoint(transform.TransformPoint(lineRenderer.GetPosition(1)));
        Vector2 segment = b - a;
        float t = segment.sqrMagnitude > 0 ? Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude) : 0;
        return Vector2.Distance(point, a + t * segment);
    }

    // Map look (設計書_画面デザイン 3-3): main roads wide and yellow, backstreets narrow and white, the chosen route
    // in tomato with a dark edge, and roads you can take next outlined in tomato. Congestion is drawn by MapVisuals as dots.
    public void SetRouteStyle(bool used, bool available, bool last)
    {
        directionalView=BusinessView.HideMapArrows;styledUsed=used;styledAvailable=available;styledLast=last;
        var cam=Camera.main;styledPixelScale=cam!=null?cam.orthographicSize*2/Mathf.Max(1,cam.pixelHeight):1;
        var theme=UITheme.Active;bool main=road.RoadType==RoadType.Main;bool route=used || last;
        float width=Mathf.Min(main?theme.mapMainRoad:theme.mapBackRoad,theme.mapRoadMaxPixels*styledPixelScale);
        Color fill=route && !directionalView?theme.tomato:main?theme.mainRoadFill:theme.backRoadFill;
        Color edge=route?theme.line:available?theme.tomato:main?theme.mainRoadEdge:theme.backRoadEdge;
        lineRenderer.numCapVertices=6;lineRenderer.startWidth=lineRenderer.endWidth=width;lineRenderer.startColor=lineRenderer.endColor=fill;
        if(border!=null){border.numCapVertices=6;border.startWidth=border.endWidth=width+Mathf.Min(theme.mapRoadEdge,theme.mapRoadBorderPixels*styledPixelScale)*(available && !route?1.6f:1);border.startColor=border.endColor=edge;}
    }
}
