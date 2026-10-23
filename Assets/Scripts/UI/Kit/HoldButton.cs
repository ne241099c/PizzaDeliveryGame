using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>True while the pointer holds this element down (and has not left it). For press-and-hold buttons.</summary>
public sealed class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    public bool Held { get; private set; }
    public void OnPointerDown(PointerEventData data)=>Held=data.button==PointerEventData.InputButton.Left;
    public void OnPointerUp(PointerEventData data)=>Held=false;
    public void OnPointerExit(PointerEventData data)=>Held=false;
    private void OnDisable()=>Held=false;
}
