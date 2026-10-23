using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Pill button with a dark edge and an offset shadow. Hover lifts the face, press sinks it (0.08s).
/// Kinds follow the style guide: primary (tomato), secondary (paper), danger (berry), selected (cheese).</summary>
[RequireComponent(typeof(Button))]
public sealed class StickerButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] private StickerKind kind;
    // Serialized so a button built into a prefab keeps its parts.
    [SerializeField] private RectTransform face;
    [SerializeField] private Image shadow,edge,fill,icon,trailing;
    [SerializeField] private TextMeshProUGUI label;
    /// <summary>Moves the caption up (positive) to make room for a second line under it.</summary>
    [SerializeField] private float labelOffsetY;
    private Button button;
    private bool hover,pressed,lastInteractable=true;
    private float offset;
    private StickerKind appliedKind=(StickerKind)(-1);
    public Button Button=>button!=null?button:(button=GetComponent<Button>());
    public TextMeshProUGUI Label=>label;
    public StickerKind Kind {get=>kind;set{kind=value;Apply();}}
    public RectTransform Face=>face;
    public float LabelOffsetY {get=>labelOffsetY;set{labelOffsetY=value;laidOutText=null;}}
    public bool Interactable {get=>Button.interactable;set{if(Button.interactable!=value){Button.interactable=value;Apply();}}}
    public string Text {get=>label!=null?label.text:"";set{if(label==null)return;SetText(value);}}
    // The fonts have no ▶ glyph: a trailing ▶ in the caption becomes a play icon after the text.
    private void SetText(string value)
    {
        bool play=value.EndsWith("▶");string text=play?value.TrimEnd('▶',' '):value;
        if(play && trailing==null)
        {
            trailing=UiKit.Icon(face,"Play","icon_play",0,0,label.fontSize*.8f,label.color);
            var r=trailing.rectTransform;
            r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        }
        if(trailing!=null)trailing.gameObject.SetActive(play);
        label.text=text;laidOutText=null;
    }
    private string laidOutText;private float laidOutSize;
    // Icon, text and trailing ▶ are centred together on the face; text is centred on its glyph shapes
    // (the Japanese fonts' ascent/descent otherwise push it visibly low).
    private void LayoutContent()
    {
        laidOutText=label.text;laidOutSize=label.fontSize;
        label.verticalAlignment=VerticalAlignmentOptions.Geometry;
        bool lead=icon!=null && icon.gameObject.activeSelf,trail=trailing!=null && trailing.gameObject.activeSelf;
        if(!lead && !trail){label.rectTransform.anchoredPosition=new Vector2(0,labelOffsetY);return;}
        label.horizontalAlignment=HorizontalAlignmentOptions.Center;
        label.rectTransform.offsetMin=new Vector2(8,0);label.rectTransform.offsetMax=new Vector2(-8,0);
        float w=0;
        if(label.text.Length>0)
        {
            label.ForceMeshUpdate();
            w=label.textBounds.size.x;
            if(w<=0 || float.IsNaN(w))w=label.GetPreferredValues(label.text).x;
        }
        float li=lead?icon.rectTransform.sizeDelta.x:0,ti=0;
        if(trail){ti=label.fontSize*.8f;trailing.rectTransform.sizeDelta=new Vector2(ti,ti);}
        float gap=label.fontSize*.3f,gl=lead && w>0?gap:0,gt=trail && w>0?gap:0,total=li+gl+w+gt+ti,left=-total/2f;
        if(lead)icon.rectTransform.anchoredPosition=new Vector2(left+li/2f,0);
        label.rectTransform.anchoredPosition=new Vector2(left+li+gl+w/2f,labelOffsetY);
        if(trail)trailing.rectTransform.anchoredPosition=new Vector2(left+li+gl+w+gt+ti/2f,0);
    }

    public void Build(string text,StickerKind kind,float fontSize,FontRole role,string iconName)
    {
        this.kind=kind;var root=(RectTransform)transform;var T=UiKit.T;
        float radius=Mathf.Min(root.sizeDelta.y*.5f,999);
        var s=UiKit.Stretch(root,"Shadow",0,T.stickerShadow,0,-T.stickerShadow);shadow=UiKit.Fill(s,Color.black,radius);
        face=UiKit.Stretch(root,"Face");
        edge=UiKit.Fill(UiKit.Stretch(face,"Edge"),T.line,radius);
        fill=UiKit.Fill(UiKit.Stretch(face,"Fill",T.edge,T.edge,T.edge,T.edge),Color.white,radius-T.edge);
        label=UiKit.Label(face,"Label",text,0,0,root.sizeDelta.x,root.sizeDelta.y,fontSize,T.line,role,TextAlignmentOptions.Center);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.pivot=new Vector2(.5f,.5f);
        label.rectTransform.offsetMin=new Vector2(12,0);label.rectTransform.offsetMax=new Vector2(-12,0);
        if(iconName!=null)
        {
            float iconSize=Mathf.Min(fontSize*1.1f,root.sizeDelta.y*.6f);
            icon=UiKit.Icon(face,"Icon",iconName,0,0,iconSize,Color.white);
            var r=icon.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);
        }
        SetText(text);Apply();LayoutContent();
    }
    public void Apply()
    {
        if(fill==null)return;var T=UiKit.T;bool on=Button.interactable;lastInteractable=on;appliedKind=kind;
        Color face,line,text,drop;
        switch(kind)
        {
            case StickerKind.Primary:face=T.tomato;line=T.line;text=Color.white;drop=T.tomatoShadow;break;
            case StickerKind.Danger:face=T.paper;line=T.berry;text=T.berry;drop=new Color(T.berry.r,T.berry.g,T.berry.b,.35f);break;
            case StickerKind.Selected:face=T.cheeseLight;line=T.line;text=T.line;drop=new Color(T.line.r,T.line.g,T.line.b,.3f);break;
            default:face=T.paper;line=T.line;text=T.line;drop=new Color(T.line.r,T.line.g,T.line.b,.3f);break;
        }
        if(!on)
        {
            if(kind==StickerKind.Primary){face=T.disabled;line=Color.Lerp(T.disabled,T.line,.25f);text=Color.white;}
            else{face=T.paper;line=T.disabled;text=T.disabled;}
            drop=new Color(T.disabled.r,T.disabled.g,T.disabled.b,.45f);
        }
        fill.color=face;edge.color=line;label.color=text;shadow.color=drop;if(icon!=null)icon.color=text;if(trailing!=null)trailing.color=text;
    }
    private void LateUpdate()
    {
        if(UiRendering.Suspended)return;
        if(Button.interactable!=lastInteractable || kind!=appliedKind)Apply();
        if(label!=null && (label.text!=laidOutText || !Mathf.Approximately(label.fontSize,laidOutSize)))LayoutContent();
        float target=!Button.interactable?0:pressed?-4:hover?2:0;
        float speed=Time.unscaledDeltaTime/UiKit.T.buttonFeedbackSeconds;offset=Mathf.MoveTowards(offset,target,Mathf.Max(.5f,speed*6));
        if(face!=null)face.anchoredPosition=new Vector2(0,offset);
    }
    public void OnPointerEnter(PointerEventData e)=>hover=true;
    public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
    public void OnPointerDown(PointerEventData e)=>pressed=true;
    public void OnPointerUp(PointerEventData e)=>pressed=false;
}
