using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public enum StickerKind { Primary, Secondary, Danger, Selected }
public enum FontRole { Body, Bold, Black, Display }

/// <summary>Code-built parts for the redesigned screens: sticker cards (2-3px edge + offset shadow), pill buttons,
/// bars, badges and avatars. Colours, sizes and fonts come from UITheme (v2 tokens).</summary>
public static class UiKit
{
    public static UITheme T=>UITheme.Active;
    private static Sprite round,circle,triangle,mapLabel;
    public static Sprite MapLabel=>mapLabel!=null?mapLabel:(mapLabel=Shape(128,16,false));
    private static readonly Dictionary<string,Sprite> art=new();

    // ---- generated shapes ----
    /// <summary>White rounded square (radius 64px) for 9-slicing; Image.pixelsPerUnitMultiplier picks the radius.</summary>
    public static Sprite Round=>T!=null && T.roundSprite!=null?T.roundSprite:round!=null?round:(round=Shape(128,64,false));
    public static Sprite Circle=>T!=null && T.circleSprite!=null?T.circleSprite:circle!=null?circle:(circle=Shape(128,64,true));
    public static Sprite Triangle
    {
        get
        {
            if(triangle!=null)return triangle;
            var tex=new Texture2D(32,32,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave,wrapMode=TextureWrapMode.Clamp};
            for(int y=0;y<32;y++)for(int x=0;x<32;x++){float half=(y+.5f)/32f*16f;float d=Mathf.Abs(x+.5f-16)-half;tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));}
            tex.Apply();triangle=Sprite.Create(tex,new Rect(0,0,32,32),new Vector2(.5f,.5f),100);triangle.hideFlags=HideFlags.DontSave;return triangle;
        }
    }
    private static Sprite Shape(int size,int radius,bool full)
    {
        var tex=new Texture2D(size,size,TextureFormat.RGBA32,false){hideFlags=HideFlags.DontSave,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
        for(int y=0;y<size;y++)for(int x=0;x<size;x++)
        {
            float px=x+.5f,py=y+.5f;float cx=Mathf.Clamp(px,radius,size-radius),cy=Mathf.Clamp(py,radius,size-radius);
            float d=Mathf.Sqrt((px-cx)*(px-cx)+(py-cy)*(py-cy))-radius;
            tex.SetPixel(x,y,new Color(1,1,1,Mathf.Clamp01(.5f-d)));
        }
        tex.Apply();
        var sprite=Sprite.Create(tex,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,full?Vector4.zero:new Vector4(radius,radius,radius,radius));
        sprite.hideFlags=HideFlags.DontSave;return sprite;
    }
    /// <summary>Original art rendered by Tools/Art/shop_art.py into Resources/ShopArt.</summary>
    public static Sprite Art(string name)
    {
        if(art.TryGetValue(name,out var s) && s!=null)return s;
        s=Resources.Load<Sprite>("ShopArt/"+name);art[name]=s;return s;
    }

    // ---- layout helpers (top-left origin, y down, in 1920x1080 canvas units) ----
    public static RectTransform Box(Transform parent,string name,float x,float y,float w,float h)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);Place(r,x,y,w,h);return r;
    }
    public static void Place(RectTransform r,float x,float y,float w,float h){r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(w,h);}
    public static RectTransform Stretch(Transform parent,string name,float left=0,float top=0,float right=0,float bottom=0)
    {
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.pivot=new Vector2(.5f,.5f);r.offsetMin=new Vector2(left,bottom);r.offsetMax=new Vector2(-right,-top);return r;
    }
    public static CanvasGroup Group(RectTransform r){var g=r.GetComponent<CanvasGroup>();if(g==null)g=r.gameObject.AddComponent<CanvasGroup>();return g;}
    public static void Show(Component c,bool visible){if(c!=null)UIVisibility.Set(c.gameObject,visible);}

    public static Image Fill(RectTransform r,Color color,float radius,bool raycast=false)
    {
        var image=r.GetComponent<Image>();if(image==null)image=r.gameObject.AddComponent<Image>();
        image.color=color;image.raycastTarget=raycast;
        if(radius>0){image.sprite=Round;image.type=Image.Type.Sliced;image.pixelsPerUnitMultiplier=64f/Mathf.Max(1,radius);}
        else image.sprite=null;
        return image;
    }
    /// <summary>Sticker card: offset shadow, dark edge, face. Content added afterwards draws on top.</summary>
    public static RectTransform Card(Transform parent,string name,float x,float y,float w,float h,Color face,float radius=-1,float shadow=-1,float edge=-1,Color? edgeColor=null,Color? shadowColor=null)
    {
        var root=Box(parent,name,x,y,w,h);Decorate(root,face,radius,shadow,edge,edgeColor,shadowColor);return root;
    }
    public static void Decorate(RectTransform root,Color face,float radius=-1,float shadow=-1,float edge=-1,Color? edgeColor=null,Color? shadowColor=null)
    {
        if(radius<0)radius=T.cardRadius;if(shadow<0)shadow=T.stickerShadow;if(edge<0)edge=T.edge;
        if(shadow>0){var s=Stretch(root,"Shadow",0,shadow,0,-shadow);Fill(s,shadowColor??new Color(T.line.r,T.line.g,T.line.b,.22f),radius);}
        if(edge>0){var e=Stretch(root,"Edge");Fill(e,edgeColor??T.line,radius);}
        var f=Stretch(root,"Face",edge,edge,edge,edge);Fill(f,face,Mathf.Max(1,radius-edge));
    }
    public static Image FaceOf(RectTransform card)=>card.Find("Face")?.GetComponent<Image>();
    public static void Dashed(RectTransform root,Color color,float dash=14,float gap=10,float thickness=3,float inset=0)
    {
        var size=root.rect.size;if(size.x<=0)size=root.sizeDelta;
        void Seg(float x,float y,float w,float h){var r=Box(root,"Dash",x,y,w,h);Fill(r,color,0);}
        for(float x=inset+dash*.5f;x<size.x-inset-dash;x+=dash+gap){Seg(x,inset,dash,thickness);Seg(x,size.y-inset-thickness,dash,thickness);}
        for(float y=inset+dash*.5f;y<size.y-inset-dash;y+=dash+gap){Seg(inset,y,thickness,dash);Seg(size.x-inset-thickness,y,thickness,dash);}
    }
    public static void DashedLine(Transform parent,float x,float y,float w,Color color,float dash=8,float gap=6,float thickness=2)
    {for(float d=0;d<w;d+=dash+gap){var r=Box(parent,"Dash",x+d,y,Mathf.Min(dash,w-d),thickness);Fill(r,color,0);}}

    // ---- text ----
    public static TMP_FontAsset Font(FontRole role)=>role switch
    {FontRole.Display=>T.displayFont!=null?T.displayFont:T.font,FontRole.Black=>T.blackFont!=null?T.blackFont:T.font,FontRole.Bold=>T.boldFont!=null?T.boldFont:T.font,_=>T.font};
    public static TextMeshProUGUI Label(Transform parent,string name,string text,float x,float y,float w,float h,float size,Color color,FontRole role=FontRole.Bold,TextAlignmentOptions align=TextAlignmentOptions.MidlineLeft,bool wrap=false)
    {
        var r=Box(parent,name,x,y,w,h);var t=r.gameObject.AddComponent<TextMeshProUGUI>();
        // 16 is the smallest size used (readable at 720p).
        t.font=Font(role);t.fontSize=Mathf.Max(size,T.captionSize);t.color=color;t.alignment=align;t.text=text;t.raycastTarget=false;
        t.textWrappingMode=wrap?TextWrappingModes.Normal:TextWrappingModes.NoWrap;t.overflowMode=TextOverflowModes.Overflow;t.margin=Vector4.zero;
        // Long numbers (more digits) shrink to fit their box instead of spilling over the card behind them.
        if(t.fontSize>T.captionSize){t.enableAutoSizing=true;t.fontSizeMax=t.fontSize;t.fontSizeMin=Mathf.Max(T.captionSize,t.fontSize*.5f);}
        return t;
    }

    // ---- images ----
    public static Image Icon(Transform parent,string name,string sprite,float x,float y,float size,Color tint)
    {var r=Box(parent,name,x,y,size,size);var i=r.gameObject.AddComponent<Image>();i.sprite=Art(sprite);i.color=tint;i.raycastTarget=false;i.preserveAspect=true;return i;}
    public static Image Picture(Transform parent,string name,string sprite,float x,float y,float w,float h,bool raycast=false)
    {var r=Box(parent,name,x,y,w,h);var i=r.gameObject.AddComponent<Image>();i.sprite=Art(sprite);i.raycastTarget=raycast;i.preserveAspect=true;return i;}

    // ---- composite parts ----
    public static StickerButton Button(Transform parent,string name,string label,StickerKind kind,float x,float y,float w,float h,float fontSize,UnityAction onClick,FontRole role=FontRole.Black,string icon=null)
    {
        var root=Box(parent,name,x,y,w,h);var hit=root.gameObject.AddComponent<Image>();hit.color=new Color(1,1,1,0);
        var button=root.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.targetGraphic=hit;
        if(onClick!=null)button.onClick.AddListener(onClick);
        var sticker=root.gameObject.AddComponent<StickerButton>();sticker.Build(label,kind,fontSize,role,icon);return sticker;
    }
    public static RectTransform Badge(Transform parent,string name,string text,float x,float y,Color fill,Color textColor,float height=28,float size=16,float minWidth=0)
    {
        var root=Box(parent,name,x,y,10,height);var bg=Stretch(root,"Edge");Fill(bg,T.line,height/2f);
        var face=Stretch(bg,"Face",2,2,2,2);Fill(face,fill,height/2f-2);
        var label=Label(root,"Text",text,0,0,10,height,Mathf.Max(size,T.captionSize),textColor,FontRole.Black,TextAlignmentOptions.Center);label.verticalAlignment=VerticalAlignmentOptions.Geometry;
        float width=Mathf.Max(minWidth,label.GetPreferredValues(text).x+height*.8f);root.sizeDelta=new Vector2(width,height);label.rectTransform.sizeDelta=new Vector2(width,height);
        return root;
    }
    public static void SetBadge(RectTransform badge,string text,Color fill,Color textColor,float minWidth=0)
    {
        if(badge==null)return;var label=badge.Find("Text").GetComponent<TextMeshProUGUI>();label.text=text;label.color=textColor;
        badge.Find("Edge/Face").GetComponent<Image>().color=fill;float h=badge.sizeDelta.y;float width=Mathf.Max(minWidth,label.GetPreferredValues(text).x+h*.8f);
        badge.sizeDelta=new Vector2(width,h);label.rectTransform.sizeDelta=new Vector2(width,h);
    }
    public static RectTransform Avatar(Transform parent,string name,float x,float y,float size,Color color,string initial)
    {
        var root=Box(parent,name,x,y,size,size);var edge=Stretch(root,"Edge");var e=edge.gameObject.AddComponent<Image>();e.sprite=Circle;e.color=T.line;e.raycastTarget=false;
        float border=Mathf.Max(2,size*.045f);var face=Stretch(root,"Face",border,border,border,border);var f=face.gameObject.AddComponent<Image>();f.sprite=Circle;f.color=color;f.raycastTarget=false;
        var initialText=Label(root,"Initial",initial,0,0,size,size,size*.52f,Color.white,FontRole.Display,TextAlignmentOptions.Center);initialText.verticalAlignment=VerticalAlignmentOptions.Geometry;
        return root;
    }
    public static void SetAvatar(RectTransform avatar,Color color,string initial)
    {if(avatar==null)return;avatar.Find("Face").GetComponent<Image>().color=color;avatar.Find("Initial").GetComponent<TextMeshProUGUI>().text=initial;}
    /// <summary>Sticker-style slider: dark-edged track, coloured fill and a round tomato handle you can drag.</summary>
    public static Slider Slider(Transform parent,string name,float x,float y,float w,float h,Color fill,UnityAction<float> onChange)
    {
        var root=Box(parent,name,x,y,w,h);var slider=root.gameObject.AddComponent<Slider>();
        var track=Stretch(root,"Track");Fill(track,T.line,h*.5f,true);Fill(Stretch(track,"Face",3,3,3,3),T.paperDeep,h*.5f-3);
        var area=Stretch(root,"FillArea",3,3,3,3);var fillRect=Stretch(area,"Fill");Fill(fillRect,fill,h*.5f-3);
        var slide=Stretch(root,"HandleArea",h*.5f,0,h*.5f,0);float size=h*1.5f;
        // Slider stretches the handle over the track height, so the extra size makes a round knob.
        var handle=Box(slide,"Handle",0,0,size,size);handle.anchorMin=new Vector2(0,0);handle.anchorMax=new Vector2(0,1);handle.pivot=new Vector2(.5f,.5f);handle.anchoredPosition=Vector2.zero;handle.sizeDelta=new Vector2(size,size-h);
        var drop=Stretch(handle,"Shadow",0,4,0,-4);var d=drop.gameObject.AddComponent<Image>();d.sprite=Circle;d.color=new Color(T.line.r,T.line.g,T.line.b,.3f);d.raycastTarget=false;
        var edge=Stretch(handle,"Edge");var e=edge.gameObject.AddComponent<Image>();e.sprite=Circle;e.color=T.line;
        var knob=Stretch(handle,"Knob",3,3,3,3);var k=knob.gameObject.AddComponent<Image>();k.sprite=Circle;k.color=T.tomato;k.raycastTarget=false;
        slider.fillRect=fillRect;slider.handleRect=handle;slider.targetGraphic=e;slider.direction=UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.transition=Selectable.Transition.None;slider.navigation=new Navigation{mode=Navigation.Mode.None};slider.wholeNumbers=true;
        // Only the player's drag changes the value: range/value updates from code do not call back.
        if(onChange!=null)slider.onValueChanged.AddListener(v=>{if(!syncing)onChange(v);});
        return slider;
    }
    /// <summary>Keeps a promise slider in step with the spawner (range = physical minimum .. maximum).</summary>
    private static bool syncing;
    public static void SyncPromise(Slider slider,OrderSpawner spawner,GameClock clock)
    {
        if(slider==null || spawner==null)return;syncing=true;try{
        slider.minValue=Mathf.Ceil(spawner.MinimumPromiseSeconds);slider.maxValue=Mathf.Max(slider.minValue+1,Mathf.Floor(spawner.MaximumPromiseSeconds));
        slider.SetValueWithoutNotify(spawner.PromiseSeconds);
        slider.interactable=clock==null || clock.Phase==DayPhase.Open || clock.Phase==DayPhase.BeforeOpening;}finally{syncing=false;}
    }
    public static UiBar Bar(Transform parent,string name,float x,float y,float w,float h,Color fill,bool marker=false)
    {
        var root=Box(parent,name,x,y,w,h);var bar=root.gameObject.AddComponent<UiBar>();bar.Build(fill,marker);return bar;
    }

    /// <summary>Identity of an object for change detection. New-game rosters are copies with the same ids, so ids alone miss them.</summary>
    public static string Ref(object o)=>o==null?"-":System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o).ToString();
    // ---- formatting ----
    public static string Yen(decimal value)=>(value<0?"−":"")+"¥"+Math.Abs(Math.Round(value,0,MidpointRounding.AwayFromZero)).ToString("#,0");
    public static string Signed(decimal value)=>(value>=0?"+":"−")+"¥"+Math.Abs(Math.Round(value,0,MidpointRounding.AwayFromZero)).ToString("#,0");
    public static string Ability(float value)=>value>=1?"1.0":value.ToString(".00");
    public static string Html(Color c)=>"#"+ColorUtility.ToHtmlStringRGB(c);
}
