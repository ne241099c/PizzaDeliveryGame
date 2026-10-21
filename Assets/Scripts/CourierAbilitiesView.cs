using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Scene/prefab-authored rows: colored current ability, white ceiling marker, gold XP progress.</summary>
public sealed class CourierAbilitiesView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI[] labels;
    [SerializeField] private RectTransform[] fills;
    [SerializeField] private RectTransform[] limits;
    [SerializeField] private RectTransform[] experienceFills;
    private CourierProgression progression;
    private static readonly string[] Names={"Drive","Know","Service","Endurance","Safety","Load"};
    public void Show(DeliveryPerson person)
    {
        if(progression==null)progression=FindFirstObjectByType<CourierProgression>();
        float threshold=progression!=null?progression.ExperiencePerLevel:1;
        for(int i=0;i<6;i++)
        {
            var ability=(CourierAbility)i;float value=person.Ability(ability),max=person.Potential(ability),xp=person.Experience(ability);
            float scale=ability==CourierAbility.Capacity?2:1;
            labels[i].text=$"{Names[i]} {value:F2}/{max:F2}"+(i<5?$"  XP {xp:F0}/{threshold:F0}":"");
            fills[i].anchorMax=new Vector2(value/scale,1);
            limits[i].anchorMin=limits[i].anchorMax=new Vector2(max/scale,.5f);
            experienceFills[i].anchorMax=new Vector2(Mathf.Clamp01(xp/threshold),1);
        }
    }
}
