using TMPro;
using UnityEngine;

/// <summary>Parts shared by the people cards on the hub: the round avatar with an initial, the name and up to two badges
/// placed one after the other.</summary>
[System.Serializable]
public sealed class StaffCardHeader
{
    [SerializeField] private RectTransform avatar;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private RectTransform firstBadge,secondBadge;
    public void Show(string name,Color color,(string text,Color fill,Color ink) first,(string text,Color fill,Color ink) second)
    {
        UiKit.SetAvatar(avatar,color,string.IsNullOrEmpty(name)?"?":name.Substring(0,1).ToUpperInvariant());
        nameText.text=name;
        UiKit.SetBadge(firstBadge,first.text,first.fill,first.ink);
        UiKit.SetBadge(secondBadge,second.text,second.fill,second.ink);
        var p=firstBadge.anchoredPosition;secondBadge.anchoredPosition=new Vector2(p.x+firstBadge.sizeDelta.x+8,p.y);
    }
}

/// <summary>One ability row: name, a bar with the potential marker, and the value.</summary>
[System.Serializable]
public sealed class AbilityRowParts
{
    [SerializeField] private TextMeshProUGUI label,value;
    [SerializeField] private UiBar bar;
    public void Show(string name,float amount,float potential,float scale=1)
    {
        label.text=name;value.text=UiKit.Ability(amount);bar.Set(amount/scale,potential<0?-1:potential/scale);
    }
}
