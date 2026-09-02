using UnityEngine;

public class EdgeView : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private EdgeCollider2D edgeCollider;

    private MapEdge edge;
    private MapManager mapManager;

    public string EdgeId => edge.Id;

    public void Initialize(
        MapEdge edge,
        MapNode nodeA,
        MapNode nodeB,
        MapManager mapManager,
        bool isActive)
    {
        this.edge = edge;
        this.mapManager = mapManager;

        Vector3 start = nodeA.Position;
        Vector3 end = nodeB.Position;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.useWorldSpace = true;

        edgeCollider.points = new Vector2[]
        {
            nodeA.Position,
            nodeB.Position
        };

        edgeCollider.enabled = edge.RoadType == RoadType.Backstreet;

        SetActive(isActive);
    }

    public void SetActive(bool isActive)
    {
        if (edge.RoadType == RoadType.Main)
        {
            lineRenderer.startWidth = 0.18f;
            lineRenderer.endWidth = 0.18f;
            lineRenderer.startColor = isActive ? Color.green : Color.gray;
            lineRenderer.endColor = isActive ? Color.green : Color.gray;
        }
        else
        {
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.startColor = isActive ? Color.green : Color.yellow;
            lineRenderer.endColor = isActive ? Color.green : Color.yellow;
        }
    }

    private void OnMouseDown()
    {
        if (edge.RoadType != RoadType.Backstreet)
        {
            return;
        }

        mapManager.ToggleEdge(edge.Id);
    }
}