using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Draggable items use the hand; empty slots and click-only cards drag their enclosing list.</summary>
public sealed class DragSource : MonoBehaviour, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Func<DragPayload> Payload;
    public Action Clicked;
    private ScrollRect scrolling;
    private bool draggingItem;

    public void OnInitializePotentialDrag(PointerEventData e)
    {
        scrolling=GetComponentInParent<ScrollRect>();
        if(scrolling!=null)scrolling.OnInitializePotentialDrag(e);
    }
    public void OnBeginDrag(PointerEventData e)
    {
        var payload=Payload?.Invoke();
        draggingItem=payload!=null && ShopHand.Instance!=null;
        if(draggingItem)ShopHand.Instance.Begin(payload,e);
        else
        {
            if(scrolling==null)scrolling=GetComponentInParent<ScrollRect>();
            if(scrolling!=null)scrolling.OnBeginDrag(e);
        }
    }
    public void OnDrag(PointerEventData e)
    {
        if(draggingItem)ShopHand.Instance?.Move(e);
        else if(scrolling!=null)scrolling.OnDrag(e);
    }
    public void OnEndDrag(PointerEventData e)
    {
        if(draggingItem)ShopHand.Instance?.End(e);
        else if(scrolling!=null)scrolling.OnEndDrag(e);
        draggingItem=false;
        scrolling=null;
    }
    public void OnPointerClick(PointerEventData e)
    {
        if(!e.dragging && e.button==PointerEventData.InputButton.Left)Clicked?.Invoke();
    }
}
