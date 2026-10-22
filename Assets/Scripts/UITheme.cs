using TMPro;
using UnityEngine;
[CreateAssetMenu(menuName="Pizza Delivery/UI Theme")]
public sealed class UITheme : ScriptableObject
{
    private static UITheme active;
    public static UITheme Active=>active!=null?active:(active=Resources.Load<UITheme>("UITheme"));
    private static Color Hex(string hex){ColorUtility.TryParseHtmlString(hex,out var value);return value;}
    [SerializeField] public Color background=Hex("#FBF3E4"),ink=Hex("#2B2118"),accent=Hex("#D8452B"),good=Hex("#4C8C4A"),money=Hex("#F2B632"),bad=Hex("#B3261E"),muted=Hex("#8A7A66"),block=Hex("#E6D5B8"),park=Hex("#BFD8A8");
    [SerializeField] public float heading=28,subheading=22,body=18,caption=14;
    [SerializeField] public int smallSpace=8,padding=16,largeSpace=24,cornerRadius=8;
    [SerializeField,Range(0,1)] public float fontFaceDilate=.12f;
    [SerializeField] public TMP_FontAsset font;
    [SerializeField] public Sprite roundedPanel;
    [SerializeField] public Color[] courierColors={Hex("#27658A"),Hex("#835191"),Hex("#956129"),Hex("#3E7770"),Hex("#AC4E72"),Hex("#546536")};
    [SerializeField] public float mainRoadWidth=.18f,backstreetWidth=.10f,routeWidth=.24f,roadBorder=.07f,nodeSize=.4f,worldLabelSize=4.5f;
    [SerializeField] public float congestionWidthScale=.35f,worldIconScale=.35f,routeArrowSize=4f;
    public Color CourierColor(string name){var roster=Object.FindFirstObjectByType<DeliveryPersonSelector>();if(roster!=null){for(int i=0;i<roster.People.Count;i++)if(roster.People[i].Name==name)return courierColors[i%courierColors.Length];}uint hash=2166136261;foreach(char c in name)hash=unchecked((hash^c)*16777619);return courierColors[hash%(uint)courierColors.Length];}
    [SerializeField] public float effectSeconds=.9f,effectRise=.6f,deadlineWarning=.5f,deadlineUrgent=.2f,lowStamina=.25f,closingWarning=.2f,shakeDistance=.04f,shakeFrequency=22;
    public Color DeadlineColor(float fraction)=>fraction<=deadlineUrgent?bad:fraction<=deadlineWarning?money:good;
    public float Size(ThemeTextSize size)=>size switch {ThemeTextSize.Heading=>heading,ThemeTextSize.Subheading=>subheading,ThemeTextSize.Caption=>caption,_=>body};
}
public enum ThemeTextSize {Heading,Subheading,Body,Caption}
public enum ThemeButtonKind {Primary,Secondary,Danger,Disabled,Selected}
