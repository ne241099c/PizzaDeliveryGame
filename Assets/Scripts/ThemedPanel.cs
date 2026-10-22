using UnityEngine;
using UnityEngine.UI;
[RequireComponent(typeof(Image))]
public sealed class ThemedPanel : MonoBehaviour
{
    [SerializeField] public bool secondary;
    public void Apply(UITheme theme){var image=GetComponent<Image>();image.sprite=theme.roundedPanel;image.type=Image.Type.Sliced;image.color=secondary?Color.Lerp(theme.background,theme.block,.4f):theme.background;}
    private void OnEnable(){if(UITheme.Active!=null)Apply(UITheme.Active);}
}
