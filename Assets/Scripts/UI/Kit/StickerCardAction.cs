using UnityEngine;
using UnityEngine.UI;

/// <summary>Interaction feedback for existing cards and empty bag slots without replacing their content or action.</summary>
[RequireComponent(typeof(Button))]
public sealed class StickerCardAction : MonoBehaviour
{
    public void Apply()
    {
        var button=GetComponent<Button>();
        var hit=GetComponent<Image>();
        if(hit==null)hit=gameObject.AddComponent<Image>();
        var face=UiKit.FaceOf((RectTransform)transform);
        hit.color=face!=null?new Color(1,1,1,0):new Color(1,1,1,.12f);
        hit.raycastTarget=true;
        button.targetGraphic=face!=null?face:hit;
        button.transition=Selectable.Transition.ColorTint;
        var colors=button.colors;
        colors.normalColor=Color.white;
        colors.highlightedColor=UiKit.T.cheeseLight;
        colors.pressedColor=UiKit.T.cheese;
        colors.selectedColor=Color.white;
        colors.disabledColor=UiKit.T.disabled;
        colors.fadeDuration=UiKit.T.buttonFeedbackSeconds;
        button.colors=colors;
    }
}
