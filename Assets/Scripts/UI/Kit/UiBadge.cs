using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A pill badge made by UiKit.Badge: its label and fill, so UiKit.SetBadge needs no lookup by name.</summary>
public sealed class UiBadge : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Image face;
    public TextMeshProUGUI Label=>label;
    public Image Face=>face;
    public void Init(TextMeshProUGUI text,Image fill){label=text;face=fill;}
}
