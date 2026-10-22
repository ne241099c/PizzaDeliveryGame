using TMPro;
using UnityEngine;
[RequireComponent(typeof(TMP_Text))]
public sealed class ThemedText : MonoBehaviour
{
    [SerializeField] public ThemeTextSize size=ThemeTextSize.Body;
    [SerializeField] public bool numeric;
    public void Apply(UITheme theme)
    {
        var text=GetComponent<TMP_Text>();text.font=theme.font;text.fontSize=theme.Size(size);text.fontSizeMin=theme.caption;text.fontSizeMax=theme.Size(size);text.color=theme.ink;
        text.fontStyle=numeric?FontStyles.Bold:FontStyles.Normal;
        text.margin=Vector4.zero;
    }
    private void OnEnable(){if(UITheme.Active!=null)Apply(UITheme.Active);}
}
