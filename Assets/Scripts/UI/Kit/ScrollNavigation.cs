using UnityEngine;
using UnityEngine.UI;

/// <summary>Shared input setup for the hub cards and dispatch lanes. Does not rebuild their contents.</summary>
public static class ScrollNavigation
{
    /// <summary>Wheel scrolling, dragging the background, and an optional visible horizontal handle.
    /// Unity's ScrollRect maps an ordinary vertical wheel to x when horizontal is its only enabled axis.</summary>
    public static void Configure(ScrollRect scroll,bool createScrollbar=false)
    {
        if(scroll==null)return;
        if(scroll.viewport==null)scroll.viewport=(RectTransform)scroll.transform;
        var viewport=scroll.viewport;
        var surface=viewport.GetComponent<Graphic>();
        if(surface==null)
        {
            var image=viewport.gameObject.AddComponent<Image>();
            image.color=Color.clear;
            surface=image;
        }
        surface.raycastTarget=true;
        scroll.movementType=ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity=UiKit.T.largeSpace*6;
        if(createScrollbar && scroll.horizontal && !scroll.vertical && scroll.horizontalScrollbar==null)
            AddHorizontalScrollbar(scroll);
    }

    private static void AddHorizontalScrollbar(ScrollRect scroll)
    {
        var theme=UiKit.T;
        var track=UiKit.Stretch((RectTransform)scroll.transform,"ScrollHandle");
        track.anchorMin=new Vector2(0,0);
        track.anchorMax=new Vector2(1,0);
        track.pivot=new Vector2(.5f,0);
        track.offsetMin=new Vector2(theme.smallSpace,0);
        track.offsetMax=new Vector2(-theme.smallSpace,theme.padding);
        UiKit.Fill(track,theme.paperDeep,theme.cornerRadius,true);
        var area=UiKit.Stretch(track,"SlidingArea",theme.edge,theme.edge,theme.edge,theme.edge);
        var handle=UiKit.Stretch(area,"Handle");
        var image=UiKit.Fill(handle,theme.info,theme.cornerRadius,true);
        var bar=track.gameObject.AddComponent<Scrollbar>();
        bar.handleRect=handle;
        bar.targetGraphic=image;
        bar.direction=Scrollbar.Direction.LeftToRight;
        bar.navigation=new Navigation{mode=Navigation.Mode.None};
        scroll.horizontalScrollbar=bar;
        scroll.horizontalScrollbarVisibility=ScrollRect.ScrollbarVisibility.AutoHide;
        // Explicit viewport avoids treating this sibling handle as scrollable content.
        scroll.horizontalScrollbarSpacing=theme.smallSpace;
    }
}
