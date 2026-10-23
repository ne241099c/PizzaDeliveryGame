using TMPro;
using UnityEngine;

/// <summary>A courier applicant (hub, applicants tab). Layout: Assets/Prefabs/UI/Cards/ApplicantCard.prefab.</summary>
public sealed class ApplicantCardView : MonoBehaviour
{
    [SerializeField] private StaffCardHeader header;
    [SerializeField] private AbilityRowParts[] abilities=new AbilityRowParts[5];
    [SerializeField] private TextMeshProUGUI capacityValue,strengths,wage,hireCost,reason;
    [SerializeField] private StickerButton hire;
    public StickerButton Hire=>hire;
    public void ShowHeader(string name,Color color,(string,Color,Color) role,(string,Color,Color) type)=>header.Show(name,color,role,type);
    public void ShowAbility(int index,string name,float value,float potential)=>abilities[index].Show(name,value,potential);
    public void ShowDetails(string capacity,string strong,string wageText,string cost){capacityValue.text=capacity;strengths.text=strong;wage.text=wageText;hireCost.text=cost;}
    /// <summary>Why hiring is not possible now; empty enables the button.</summary>
    public void ShowRejection(string text){reason.text=text;reason.gameObject.SetActive(text.Length>0);hire.Interactable=text.Length==0;}
}
