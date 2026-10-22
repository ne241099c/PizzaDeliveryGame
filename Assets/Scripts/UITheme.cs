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
    [SerializeField, Min(1)] public float cityLabelMinimumBlockPixels=150,cityDetailMinimumBlockPixels=70;
    [SerializeField, Min(1)] public float cityCompactPinPixels=18,cityCompactNumberSize=120;
    [SerializeField, Min(.01f)] public float cityExpansionFadeSeconds=.6f;
    [SerializeField, Min(1)] public float deliveryLaneMinWidth=560,deliveryRouteWidthPixels=6,deliveryRouteOffsetPixels=9,deliveryRouteDotSpacingPixels=64,deliveryRouteDotSpeedPixels=28;
    [Header("v2: shop redesign (設計書_画面デザイン 2)")]
    [SerializeField] public Color paper=Hex("#FFF6E5"),paperDeep=Hex("#F3E3C3"),line=Hex("#3A2A20"),inkSub=Hex("#7A6652"),tomato=Hex("#E04B2D"),tomatoShadow=Hex("#A8361F"),basil=Hex("#4C9A52"),basilLight=Hex("#E2F1DF"),cheese=Hex("#F7BE3D"),cheeseLight=Hex("#FFF0C2"),berry=Hex("#B3261E"),berryLight=Hex("#F9DFDA"),info=Hex("#3C79C2"),infoLight=Hex("#E4EEFA"),disabled=Hex("#C9BBA3");
    [SerializeField] public Color wood=Hex("#C98B5B"),woodDark=Hex("#9C6A43"),plaster=Hex("#F1E4CF"),steel=Hex("#C8D0D6"),steelShadow=Hex("#8F9BA5"),floorA=Hex("#E9C99C"),floorB=Hex("#DDB783"),barFill=Hex("#F1A43A");
    [SerializeField] public float titleSize=56,headingSize=32,subheadingSize=24,bodySize=20,captionSize=16;
    [SerializeField] public float edge=3,stickerShadow=6,cardRadius=16,outerMargin=32;
    [SerializeField, Min(.01f)] public float buttonFeedbackSeconds=.08f;
    [SerializeField, Min(1)] public float cancelledOrderNoticeSeconds=8f;
    [SerializeField] public TMP_FontAsset displayFont,boldFont,blackFont;
    [SerializeField] public Color mapGround=Hex("#EFEBE2"),mapBlock=Hex("#E7E1D3"),mapPark=Hex("#C9E3B4"),mapTree=Hex("#9FC98A"),mapWater=Hex("#B9DDF0"),mainRoadEdge=Hex("#D9A93A"),mainRoadFill=Hex("#FDE08A"),backRoadEdge=Hex("#B9AE98"),backRoadFill=Hex("#FFFDF6"),congestion=Hex("#E04B2D");
    [SerializeField, Min(1)] public float mapRoadMaxPixels=24f,mapRoadBorderPixels=3f,mapRouteArrowPixels=22f;
    [SerializeField] public float mapMainRoad=.26f,mapBackRoad=.14f,mapRoute=.22f,mapRoadEdge=.05f,mapPinScale=.012f;
    public Color CourierColor(string name){var roster=Object.FindFirstObjectByType<DeliveryPersonSelector>();if(roster!=null){for(int i=0;i<roster.People.Count;i++)if(roster.People[i].Name==name)return courierColors[i%courierColors.Length];}uint hash=2166136261;foreach(char c in name)hash=unchecked((hash^c)*16777619);return courierColors[hash%(uint)courierColors.Length];}
    [SerializeField] public float effectSeconds=.9f,effectRise=.6f,deadlineWarning=.5f,deadlineUrgent=.2f,lowStamina=.25f,closingWarning=.2f,shakeDistance=.04f,shakeFrequency=22;
    public Color DeadlineColor(float fraction)=>fraction<=deadlineUrgent?bad:fraction<=deadlineWarning?money:good;
    public float Size(ThemeTextSize size)=>size switch {ThemeTextSize.Heading=>heading,ThemeTextSize.Subheading=>subheading,ThemeTextSize.Caption=>caption,_=>body};
}
public enum ThemeTextSize {Heading,Subheading,Body,Caption}
public enum ThemeButtonKind {Primary,Secondary,Danger,Disabled,Selected}
