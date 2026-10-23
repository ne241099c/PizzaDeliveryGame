using TMPro;
using UnityEngine;

/// <summary>The dashed "仲間を探す" card at the end of the team row. Layout: Assets/Prefabs/UI/Cards/FindTeammateCard.prefab.</summary>
public sealed class FindTeammateCardView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI detail;
    [SerializeField] private StickerButton seeApplicants;
    public StickerButton SeeApplicants=>seeApplicants;
    public void Show(int applicants)=>detail.text=$"人数制限なし\n求人は{applicants}人 来ています";
}
