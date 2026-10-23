using UnityEngine;

public class EdgeView : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private EdgeCollider2D edgeCollider;
    private MapEdge road;
    private Vector2[] worldPoints;
    private LineRenderer border;
    private bool styledUsed,styledAvailable,styledLast;
    private float styledPixelScale;
    private bool directionalView,residentialRoad;
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
        residentialRoad=mapManager.City!=null && mapManager.City.version>=6 && (edge.Id.Contains("_Local") || edge.Id.Contains("_House") || edge.Id.Contains("_Pair"));
        gameObject.name = "Edge_" + edge.Id;
        worldPoints=RoadGeometry.Points(mapManager,edge);
        Vector2 start=worldPoints[0];
        transform.position=start;
        var localPoints=new Vector2[worldPoints.Length];
        lineRenderer.positionCount=worldPoints.Length;
        lineRenderer.useWorldSpace=false;
        for(int i=0;i<worldPoints.Length;i++)
        {
            localPoints[i]=worldPoints[i]-start;
            lineRenderer.SetPosition(i,localPoints[i]);
        }
        edgeCollider.points=localPoints;
        edgeCollider.enabled = true;
        edgeCollider.edgeRadius = 0.12f;
        var borderObject=new GameObject("RoadBorder");
        borderObject.transform.SetParent(transform,false);
        border=borderObject.AddComponent<LineRenderer>();
        border.sharedMaterial=lineRenderer.sharedMaterial;
        border.useWorldSpace=false;
        border.positionCount=localPoints.Length;
        for(int i=0;i<localPoints.Length;i++)border.SetPosition(i,localPoints[i]);
        border.sortingOrder=-1;
        if(!edge.Bidirectional)
        {
            var go=new GameObject("OneWayArrow");
            go.transform.SetParent(transform,false);
            go.transform.localPosition=RoadGeometry.PointAt(localPoints,.5f);
            Vector2 direction=RoadGeometry.PointAt(localPoints,.55f)-RoadGeometry.PointAt(localPoints,.45f);
            go.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(direction.y,direction.x)*Mathf.Rad2Deg);
            var text=go.AddComponent<TMPro.TextMeshPro>();
            text.font=UITheme.Active.font;
            text.text=">";
            text.fontSize=UITheme.Active.routeArrowSize;
            text.color=UITheme.Active.ink;
            text.alignment=TMPro.TextAlignmentOptions.Center;
            text.rectTransform.sizeDelta=Vector2.one;
            go.GetComponent<MeshRenderer>().sortingOrder=8;
        }
        SetRouteStyle(false, false, false);
    }

    public float DistanceToScreenPoint(Camera camera, Vector2 point)
    {
        return RoadGeometry.DistanceToScreenPoint(camera,worldPoints,point);
    }

    // Map look (設計書_画面デザイン 3-3): main roads wide and yellow, backstreets narrow and white, the chosen route
    // in tomato with a dark edge, and roads you can take next outlined in tomato. Congestion is drawn by MapVisuals as dots.
    public void SetRouteStyle(bool used, bool available, bool last)
    {
        directionalView=BusinessView.HideMapArrows;styledUsed=used;styledAvailable=available;styledLast=last;
        var cam=Camera.main;styledPixelScale=cam!=null?cam.orthographicSize*2/Mathf.Max(1,cam.pixelHeight):1;
        var theme=UITheme.Active;bool main=road.RoadType==RoadType.Main;bool route=used || last;
        float width=Mathf.Min(main?theme.mapMainRoad:theme.mapBackRoad,theme.mapRoadMaxPixels*styledPixelScale);
        if(residentialRoad)width*=theme.mapResidentialWidthScale;
        // Zoomed out, keep roads wide enough to see and to click.
        width=Mathf.Max(width,theme.mapRoadMinPixels*(main && !residentialRoad?1:.65f)*styledPixelScale);
        Color fill=route && !directionalView?theme.tomato:residentialRoad?theme.mapResidentialFill:main?theme.mainRoadFill:theme.backRoadFill;
        Color edge=route?theme.line:available && !directionalView?theme.tomato:main?theme.mainRoadEdge:theme.backRoadEdge;
        lineRenderer.numCapVertices=6;lineRenderer.numCornerVertices=6;lineRenderer.startWidth=lineRenderer.endWidth=width;lineRenderer.startColor=lineRenderer.endColor=fill;
        if(border!=null)
        {
            border.numCapVertices=6;border.numCornerVertices=6;
            border.startWidth=border.endWidth=width+Mathf.Min(theme.mapRoadEdge,theme.mapRoadBorderPixels*styledPixelScale)*(available && !route?1.6f:1);
            border.startColor=border.endColor=edge;
        }
    }
}
