using UnityEngine;

/// <summary>Aligns the world camera to its scene-authored UI slot at any Game view size.</summary>
[ExecuteAlways, DefaultExecutionOrder(-250)]
public sealed class MapViewportUI : MonoBehaviour
{
    [SerializeField] private Camera mapCamera;
    [SerializeField] private RectTransform viewport;
    private readonly Vector3[] corners = new Vector3[4];
    private void OnEnable() => ApplyViewport();
    private void Start() => ApplyViewport();
    private void Update() => ApplyViewport();
    public void ApplyViewport()
    {
        if(mapCamera==null || viewport==null)return;
        var canvas=viewport.GetComponentInParent<Canvas>();
        if(canvas==null)return;
        Rect screen=canvas.rootCanvas.pixelRect;
        if(screen.width<=0 || screen.height<=0)return;
        viewport.GetWorldCorners(corners);
        mapCamera.rect=new Rect((corners[0].x-screen.x)/screen.width,(corners[0].y-screen.y)/screen.height,
            (corners[2].x-corners[0].x)/screen.width,(corners[2].y-corners[0].y)/screen.height);
    }
}
