using TMPro;
using UnityEngine;

public class EstimatedTimeText : MonoBehaviour
{
    [SerializeField] private MapManager mapManager;
    [SerializeField] private TextMeshProUGUI estimatedTimeText;

    private void Start()
    {
        mapManager.OnRouteChanged += UpdateText;
        UpdateText(mapManager.CurrentRouteState);
    }

    private void OnDestroy()
    {
        if (mapManager != null)
        {
            mapManager.OnRouteChanged -= UpdateText;
        }
    }

    private void UpdateText(RouteState routeState)
    {
        estimatedTimeText.text = $"Estimated Time: {routeState.EstimatedTime:F2}s";
    }
}