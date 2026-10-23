using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>One facility to buy (hub, facilities tab). Advertising shows three tier buttons instead of the price and 購入.
/// Layout: Assets/Prefabs/UI/Cards/FacilityCard.prefab.</summary>
public sealed class FacilityCardView : MonoBehaviour
{
    [SerializeField] private Image picture;
    [SerializeField] private TextMeshProUGUI title,effect,price,maintenance,reason;
    [SerializeField] private RectTransform level;
    [SerializeField] private StickerButton buy;
    [SerializeField] private StickerButton[] tiers=new StickerButton[3];
    public StickerButton Buy=>buy;
    public StickerButton Tier(int tier)=>tiers[tier-1];
    public void Show(Sprite art,string name,string levelText,string effectText)
    {
        picture.sprite=art;title.text=name;effect.text=effectText;
        UiKit.SetBadge(level,levelText,UiKit.T.cheeseLight,UiKit.T.line,120);
        float w=((RectTransform)transform).sizeDelta.x;level.anchoredPosition=new Vector2((w-level.sizeDelta.x)/2f,level.anchoredPosition.y);
    }
    public void ShowPurchase(string priceText,string maintenanceText,string rejection,bool interactable)
    {
        foreach(var t in tiers)t.gameObject.SetActive(false);
        buy.gameObject.SetActive(true);price.gameObject.SetActive(true);maintenance.gameObject.SetActive(true);
        price.text=priceText;maintenance.text=maintenanceText;buy.Interactable=interactable;
        reason.text=rejection;reason.gameObject.SetActive(rejection.Length>0);
    }
    public void ShowTiers(int count,System.Func<int,string> caption,System.Func<int,bool> interactable)
    {
        buy.gameObject.SetActive(false);price.gameObject.SetActive(false);maintenance.gameObject.SetActive(false);reason.gameObject.SetActive(false);
        for(int i=0;i<tiers.Length;i++){bool on=i<count;tiers[i].gameObject.SetActive(on);if(on){tiers[i].Text=caption(i+1);tiers[i].Interactable=interactable(i+1);}}
    }
}
