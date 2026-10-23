using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A place that can receive what the hand holds; it glows while a matching item is held.</summary>
public sealed class DropZone : MonoBehaviour
{
    public Func<DragPayload,bool> Accepts;
    public Action<DragPayload> Dropped;
    public Action<DragPayload,Vector2> Hovering;
    public Image Glow;
    public void SetGlow(bool on,bool hot){if(Glow==null)return;Glow.enabled=on;var c=UiKit.T.cheese;Glow.color=new Color(c.r,c.g,c.b,hot?.95f:.55f);}
}
