using UnityEngine;
using UnityEngine.InputSystem;

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

        if (edge.RoadType == RoadType.Backstreet)
        {
            Vector3 direction = (end - start).normalized;
            Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0f);
            Vector3 offset = perpendicular * 0.25f;

            start += offset;
            end += offset;
        }

        transform.position = start;
        Vector3 localEnd = end - start;

        lineRenderer.positionCount = 2;
        lineRenderer.useWorldSpace = false;
        lineRenderer.SetPosition(0, Vector3.zero);
        lineRenderer.SetPosition(1, localEnd);

        edgeCollider.points = new Vector2[]
        {
            Vector2.zero,
            localEnd
        };

        edgeCollider.enabled = edge.RoadType == RoadType.Backstreet;
        edgeCollider.edgeRadius = edge.RoadType == RoadType.Backstreet ? 0.35f : 0f;

        SetActive(isActive);
    }

    private void Update()
    {
        if (edge == null || edge.RoadType != RoadType.Backstreet)
        {
            return;
        }

        if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 screenPosition = Mouse.current.position.ReadValue();
        Vector2 worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);

        if (edgeCollider.OverlapPoint(worldPosition))
        {
            mapManager.ToggleEdge(edge.Id);
        }
    }

    public void SetActive(bool isActive)
    {
        if (edge.RoadType == RoadType.Main)
        {
            lineRenderer.startWidth = 0.12f;
            lineRenderer.endWidth = 0.12f;
            lineRenderer.startColor = isActive ? Color.green : Color.gray;
            lineRenderer.endColor = isActive ? Color.green : Color.gray;
        }
        else
        {
            lineRenderer.startWidth = 0.08f;
            lineRenderer.endWidth = 0.08f;
            lineRenderer.startColor = isActive ? Color.green : Color.yellow;
            lineRenderer.endColor = isActive ? Color.green : Color.yellow;
        }
    }
}
