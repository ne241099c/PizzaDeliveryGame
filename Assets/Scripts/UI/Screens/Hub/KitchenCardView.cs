using TMPro;
using UnityEngine;

/// <summary>Kitchen staff (内勤) on the team or applying (hub). Layout: Assets/Prefabs/UI/Cards/KitchenCard.prefab.</summary>
public sealed class KitchenCardView : MonoBehaviour
{
    [SerializeField] private StaffCardHeader header;
    [SerializeField] private AbilityRowParts speed,care;
    [SerializeField] private TextMeshProUGUI info,wage,hireCost,reason;
    [SerializeField] private StickerButton hire,trainLater,fire;
    public StickerButton Hire=>hire;
    public StickerButton Fire=>fire;
    public void Show(string name,string rank,float speedValue,float careValue,string infoText,string wageText)
    {
        var T=UiKit.T;
        header.Show(name,T.info,(ShopStrings.Kitchen,T.info,Color.white),(rank,T.paper,T.line));
        speed.Show(ShopStrings.Speed,speedValue,-1);care.Show(ShopStrings.Care,careValue,-1);
        info.text=infoText;wage.text=wageText;
    }
    /// <summary>An applicant shows the hiring cost and 雇う; a team member shows 訓練(準備中) and 解雇.</summary>
    public void ShowMode(bool applicant,string cost,string rejection)
    {
        hire.gameObject.SetActive(applicant);
        hireCost.gameObject.SetActive(applicant);
        trainLater.gameObject.SetActive(!applicant);
        fire.gameObject.SetActive(!applicant);
        hireCost.text=cost;reason.text=applicant?rejection:"";reason.gameObject.SetActive(applicant && rejection.Length>0);
        trainLater.Interactable=false;
    }
}
