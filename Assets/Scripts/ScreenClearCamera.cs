using UnityEngine;

/// <summary>Clears the full screen behind the map camera, whose viewport only covers the map frame.
/// Without it, areas outside that viewport keep pixels from the previous screen.</summary>
[RequireComponent(typeof(Camera))]
public sealed class ScreenClearCamera : MonoBehaviour
{
    private void Awake()
    {
        var view=GetComponent<Camera>();
        view.rect=new Rect(0,0,1,1);view.cullingMask=0;view.clearFlags=CameraClearFlags.SolidColor;
        if(UITheme.Active!=null)view.backgroundColor=UITheme.Active.background;
    }
}
