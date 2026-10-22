using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
public sealed class ThemedPanel : MonoBehaviour
{
    [SerializeField] public bool secondary;
    public void Apply(UITheme theme){var image=GetComponent<Image>();
        if(image.rectTransform.rect.height<=8){image.sprite=null;image.type=Image.Type.Simple;image.color=image.name=="Value"?theme.good:image.name=="Potential"?theme.ink:image.name=="Progress"?theme.money:Color.Lerp(theme.background,theme.muted,.4f);return;}
        image.sprite=theme.roundedPanel;image.type=Image.Type.Sliced;image.color=secondary?Color.Lerp(theme.background,theme.block,.4f):theme.background;}
    private void OnEnable(){if(UITheme.Active!=null)Apply(UITheme.Active);}
}
