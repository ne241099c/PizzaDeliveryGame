using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public enum DragKind { Ticket, Dough, Ladle, Cheese, Topping, Pizza, Box, Bagged }

/// <summary>What the hand holds.</summary>
public sealed class DragPayload
{
    public DragKind Kind;public Order Order;public PizzaJob Job;public Topping Topping;public string Sprite;public Vector2 Size=new(140,140);
}

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

/// <summary>A place that can receive what the hand holds; it glows while a matching item is held.</summary>
public sealed class DropZone : MonoBehaviour
{
    public Func<DragPayload,bool> Accepts;
    public Action<DragPayload> Dropped;
    public Action<DragPayload,Vector2> Hovering;
    public Image Glow;
    public void SetGlow(bool on,bool hot){if(Glow==null)return;Glow.enabled=on;var c=UiKit.T.cheese;Glow.color=new Color(c.r,c.g,c.b,hot?.95f:.55f);}
}

/// <summary>The player's hand: one held item that follows the pointer with a shadow; right click returns it.</summary>
public sealed class ShopHand : MonoBehaviour
{
    public static ShopHand Instance {get;private set;}
    private RectTransform layer,ghost;private Image ghostImage,ghostShadow;
    private readonly List<DropZone> zones=new();
    private readonly List<RaycastResult> hits=new();
    public DragPayload Held {get;private set;}
    private int cancelledFrame=-1;
    public bool CancelledThisFrame=>cancelledFrame==Time.frameCount;
    public event Action<DragPayload> Released;
    public void Init(RectTransform dragLayer)
    {
        Instance=this;layer=dragLayer;
        ghost=UiKit.Box(layer,"Held",0,0,140,140);ghost.pivot=new Vector2(.5f,.5f);
        var shadow=UiKit.Stretch(ghost,"Shadow",8,14,-8,-14);ghostShadow=shadow.gameObject.AddComponent<Image>();ghostShadow.color=new Color(0,0,0,.22f);ghostShadow.raycastTarget=false;ghostShadow.preserveAspect=true;
        ghostImage=UiKit.Stretch(ghost,"Item").gameObject.AddComponent<Image>();ghostImage.raycastTarget=false;ghostImage.preserveAspect=true;
        ghost.gameObject.SetActive(false);
        // BuildKitchen/BuildDispatch create fixed zones before the hand exists. Later station zones register themselves.
        foreach(var zone in layer.parent.GetComponentsInChildren<DropZone>(true))Register(zone);
    }
    private void OnDestroy(){if(Instance==this)Instance=null;}
    public void Register(DropZone zone){if(!zones.Contains(zone))zones.Add(zone);}
    public void Unregister(DropZone zone)=>zones.Remove(zone);
    public void Begin(DragPayload payload,PointerEventData e)=>BeginAt(payload,e.position,e.pressEventCamera);
    public void Move(PointerEventData e)=>MoveAt(e.position,e.pressEventCamera);
    public void End(PointerEventData e)=>EndAt(e.position);
    /// <summary>Screen-position versions, used for things picked up from the world map (pins).</summary>
    public void BeginAt(DragPayload payload,Vector2 screen,Camera eventCamera=null)
    {
        if(payload.Job!=null)UnityEngine.Object.FindFirstObjectByType<CookingManager>()?.Touch(payload.Job);
        Held=payload;ghost.gameObject.SetActive(true);ghost.SetAsLastSibling();ghost.sizeDelta=payload.Size;
        var sprite=UiKit.Art(payload.Sprite);ghostImage.sprite=sprite;ghostShadow.sprite=sprite;ghostImage.color=payload.Job!=null && payload.Job.Burnt?UiKit.T.line:Color.white;MoveAt(screen,eventCamera);
    }
    public void MoveAt(Vector2 screen,Camera eventCamera=null)
    {
        if(Held==null)return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(layer,screen,eventCamera,out var local);
        ghost.localPosition=local;
        var over=ZoneAt(screen);
        foreach(var zone in zones){if(zone==null)continue;bool ok=zone.isActiveAndEnabled && zone.Accepts!=null && zone.Accepts(Held);zone.SetGlow(ok,ok && zone==over);}
        if(over!=null && over.Accepts!=null && over.Accepts(Held))over.Hovering?.Invoke(Held,screen);
    }
    public void EndAt(Vector2 screen)
    {
        if(Held==null)return;var payload=Held;var over=ZoneAt(screen);Cancel();
        if(over!=null && over.Accepts!=null && over.Accepts(payload))over.Dropped?.Invoke(payload);
        Released?.Invoke(payload);
    }
    public void Cancel(){if(Held!=null)cancelledFrame=Time.frameCount;Held=null;if(ghost!=null)ghost.gameObject.SetActive(false);foreach(var zone in zones)if(zone!=null)zone.SetGlow(false,false);}
    private DropZone ZoneAt(Vector2 screen)
    {
        if(EventSystem.current==null)return null;hits.Clear();
        EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current){position=screen},hits);
        foreach(var hit in hits){var zone=hit.gameObject.GetComponentInParent<DropZone>();if(zone!=null)return zone;}
        return null;
    }
    private void Update()
    {
        if(Held?.Job!=null)UnityEngine.Object.FindFirstObjectByType<CookingManager>()?.Touch(Held.Job);
        if(Held!=null && Keyboard.current!=null && Keyboard.current.escapeKey.wasPressedThisFrame)Cancel();
        if(Held!=null && Mouse.current!=null && Mouse.current.rightButton.wasPressedThisFrame)Cancel();
    }
}
