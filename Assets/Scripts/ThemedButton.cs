using TMPro;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Button))]
public sealed class ThemedButton : MonoBehaviour
{
    [SerializeField] public ThemeButtonKind kind=ThemeButtonKind.Secondary;
    public void Apply(UITheme theme)
    {
        var button=GetComponent<Button>();var image=GetComponent<Image>();
        Color fill=kind==ThemeButtonKind.Primary?theme.accent:kind==ThemeButtonKind.Danger?theme.bad:kind==ThemeButtonKind.Selected?Color.Lerp(theme.background,theme.accent,.12f):theme.background;
        if(image!=null){image.sprite=theme.roundedPanel;image.type=Image.Type.Sliced;image.color=Color.white;}
        var colors=button.colors;colors.normalColor=fill;colors.highlightedColor=Color.Lerp(fill,theme.ink,.12f);colors.pressedColor=Color.Lerp(fill,theme.ink,.24f);colors.selectedColor=colors.highlightedColor;colors.disabledColor=Color.Lerp(theme.background,theme.muted,.3f);button.colors=colors;button.transition=Selectable.Transition.ColorTint;
        var label=GetComponentInChildren<TMP_Text>(true);if(label!=null)label.color=kind==ThemeButtonKind.Primary || kind==ThemeButtonKind.Danger?Color.white:theme.ink;
        var outline=GetComponent<Outline>();if(outline==null)outline=gameObject.AddComponent<Outline>();outline.effectColor=kind==ThemeButtonKind.Selected?theme.accent:theme.muted;outline.effectDistance=Vector2.one;
    }
    private void LateUpdate(){if(UITheme.Active!=null)Apply(UITheme.Active);}
}
