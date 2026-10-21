using UnityEngine;

public class EdgeView : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private EdgeCollider2D edgeCollider;
    public string EdgeId { get; private set; }

    public void Initialize(MapEdge edge, MapNode nodeA, MapNode nodeB, MapManager mapManager, bool isActive)
    {
        EdgeId = edge.Id;
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

    public void SetRouteStyle(bool used, bool available, bool last)
    {
        Color color = last ? new Color(1f, 0.78f, 0.25f)
            : available ? new Color(0.24f, 0.78f, 1f)
            : used ? new Color(0.32f, 0.88f, 0.69f) : new Color(0.4f, 0.46f, 0.56f);
        float width = last ? 0.18f : available ? 0.14f : 0.09f;
        lineRenderer.startWidth = lineRenderer.endWidth = width;
        lineRenderer.startColor = lineRenderer.endColor = color;
    }
}
