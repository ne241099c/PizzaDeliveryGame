using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>A round avatar made by UiKit.Avatar: its colored face and initial.</summary>
public sealed class UiAvatar : MonoBehaviour
{
    [SerializeField] private Image face;
    [SerializeField] private TextMeshProUGUI initial;
    public void Init(Image color,TextMeshProUGUI letter){face=color;initial=letter;}
    public void Set(Color color,string letter){face.color=color;initial.text=letter;}
}
