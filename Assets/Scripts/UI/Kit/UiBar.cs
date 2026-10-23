using UnityEngine;
using UnityEngine.UI;

/// <summary>Rounded bar with a dark edge. Optional ▼ marker shows a ceiling (potential) the value cannot pass.</summary>
public sealed class UiBar : MonoBehaviour
{
    // Serialized so a bar built into a prefab keeps its parts.
    [SerializeField] private RectTransform fillRect,marker;
    [SerializeField] private Image fill;
    public float Value {get;private set;}
    public void Build(Color color,bool withMarker)
    {
        var root=(RectTransform)transform;var T=UiKit.T;float h=root.sizeDelta.y;
        UiKit.Fill(UiKit.Stretch(root,"Edge"),T.line,h*.5f);
        var track=UiKit.Stretch(root,"Track",2,2,2,2);UiKit.Fill(track,T.paperDeep,h*.5f-2);
        fillRect=UiKit.Stretch(track,"Fill");fill=UiKit.Fill(fillRect,color,h*.5f-2);fillRect.anchorMax=new Vector2(0,1);
        if(withMarker)
        {
            // ▼ just above the bar at the ceiling (potential)
            marker=UiKit.Box(root,"Marker",0,0,14,11);
            var m=marker.gameObject.AddComponent<Image>();
            m.sprite=UiKit.Triangle;
            m.color=T.line;
            m.raycastTarget=false;
            marker.pivot=new Vector2(.5f,0);
        }
    }
    public void Set(float value,float ceiling=-1,Color? color=null)
    {
        Value=Mathf.Clamp01(value);
        if(fillRect!=null)
        {
            fillRect.anchorMax=new Vector2(Value,1);
            fillRect.offsetMax=Vector2.zero;
            fillRect.offsetMin=Vector2.zero;
        }
        if(color.HasValue && fill!=null)fill.color=color.Value;
        if(marker!=null)
        {
            bool show=ceiling>=0;marker.gameObject.SetActive(show);
            if(show){var w=((RectTransform)transform).sizeDelta.x;marker.anchoredPosition=new Vector2(Mathf.Clamp01(ceiling)*w,1);}
        }
    }
}
