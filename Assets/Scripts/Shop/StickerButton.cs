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
    private RectTransform face;
    private Image shadow,edge,fill,icon,trailing;
    private TextMeshProUGUI label;
    private Button button;
    private bool hover,pressed,lastInteractable=true;
    private float offset;
    private StickerKind appliedKind=(StickerKind)(-1);
    public Button Button=>button!=null?button:(button=GetComponent<Button>());
    public TextMeshProUGUI Label=>label;
    public StickerKind Kind {get=>kind;set{kind=value;Apply();}}
    public bool Interactable {get=>Button.interactable;set{if(Button.interactable!=value){Button.interactable=value;Apply();}}}
    public string Text {get=>label!=null?label.text:"";set{if(label==null)return;SetText(value);}}
    // The fonts have no ▶ glyph: a trailing ▶ in the caption becomes a play icon after the text.
    private void SetText(string value)
    {
        bool play=value.EndsWith("▶");string text=play?value.TrimEnd('▶',' '):value;if(label.text==text && (trailing!=null)==play)return;label.text=text;
        if(play && trailing==null){trailing=UiKit.Icon(face,"Play","icon_play",0,0,label.fontSize*.8f,label.color);var r=trailing.rectTransform;r.anchorMin=r.anchorMax=r.pivot=new Vector2(.5f,.5f);}
        if(trailing!=null)trailing.gameObject.SetActive(play);
        if(play){float w=label.GetPreferredValues(text).x,i=label.fontSize*.8f,gap=label.fontSize*.3f,total=w+gap+i;
            label.rectTransform.offsetMin=new Vector2(12,0);label.rectTransform.offsetMax=new Vector2(-12,0);label.rectTransform.anchoredPosition=new Vector2(-(gap+i)/2f,0);
            trailing.rectTransform.anchoredPosition=new Vector2(-total/2f+w+gap+i/2f,0);}
        else if(icon==null)label.rectTransform.anchoredPosition=Vector2.zero;
    }

    public void Build(string text,StickerKind kind,float fontSize,FontRole role,string iconName)
    {
        this.kind=kind;var root=(RectTransform)transform;var T=UiKit.T;
        float radius=Mathf.Min(root.sizeDelta.y*.5f,999);
        var s=UiKit.Stretch(root,"Shadow",0,T.stickerShadow,0,-T.stickerShadow);shadow=UiKit.Fill(s,Color.black,radius);
        face=UiKit.Stretch(root,"Face");
        edge=UiKit.Fill(UiKit.Stretch(face,"Edge"),T.line,radius);
        fill=UiKit.Fill(UiKit.Stretch(face,"Fill",T.edge,T.edge,T.edge,T.edge),Color.white,radius-T.edge);
        float iconSize=iconName!=null?fontSize*1.1f:0;
        label=UiKit.Label(face,"Label",text,0,0,root.sizeDelta.x,root.sizeDelta.y,fontSize,T.line,role,TextAlignmentOptions.Center);
        UiKit.Place(label.rectTransform,0,0,root.sizeDelta.x,root.sizeDelta.y);
        label.rectTransform.anchorMin=Vector2.zero;label.rectTransform.anchorMax=Vector2.one;label.rectTransform.offsetMin=new Vector2(12+iconSize,0);label.rectTransform.offsetMax=new Vector2(-12,0);
        if(iconName!=null)
        {
            icon=UiKit.Icon(face,"Icon",iconName,0,0,iconSize,Color.white);
            var r=icon.rectTransform;r.anchorMin=r.anchorMax=new Vector2(.5f,.5f);r.pivot=new Vector2(.5f,.5f);
            float textWidth=label.GetPreferredValues(text).x;r.anchoredPosition=new Vector2(-(textWidth+iconSize*.4f)*.5f,0);
            label.rectTransform.offsetMin=new Vector2(12+iconSize*1.2f,0);label.alignment=TextAlignmentOptions.Center;
        }
        SetText(text);Apply();
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
        if(UIThemeDriver.RenderingSuspended)return;
        if(Button.interactable!=lastInteractable || kind!=appliedKind)Apply();
        float target=!Button.interactable?0:pressed?-4:hover?2:0;
        float speed=Time.unscaledDeltaTime/UiKit.T.buttonFeedbackSeconds;offset=Mathf.MoveTowards(offset,target,Mathf.Max(.5f,speed*6));
        if(face!=null)face.anchoredPosition=new Vector2(0,offset);
    }
    public void OnPointerEnter(PointerEventData e)=>hover=true;
    public void OnPointerExit(PointerEventData e){hover=false;pressed=false;}
    public void OnPointerDown(PointerEventData e)=>pressed=true;
    public void OnPointerUp(PointerEventData e)=>pressed=false;
}
