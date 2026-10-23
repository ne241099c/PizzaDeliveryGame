using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Something the player can pick up (drag) or use with a click.</summary>
public sealed class DragSource : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Func<DragPayload> Payload;
    public Action Clicked;
    public void OnBeginDrag(PointerEventData e){var p=Payload?.Invoke();if(p!=null)ShopHand.Instance?.Begin(p,e);}
    public void OnDrag(PointerEventData e)=>ShopHand.Instance?.Move(e);
    public void OnEndDrag(PointerEventData e)=>ShopHand.Instance?.End(e);
    public void OnPointerClick(PointerEventData e){if(!e.dragging && e.button==PointerEventData.InputButton.Left)Clicked?.Invoke();}
}
