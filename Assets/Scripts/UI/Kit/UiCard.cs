using UnityEngine;
using UnityEngine.UI;

/// <summary>A sticker card made by UiKit.Card/Decorate: its face, so the color can change without a lookup by name.</summary>
public sealed class UiCard : MonoBehaviour
{
    [SerializeField] private Image face,edge;
    public Image Face=>face;
    /// <summary>The dark outline (null when the card has none).</summary>
    public Image Edge=>edge;
    public void Init(Image fill,Image outline){face=fill;edge=outline;}
}
