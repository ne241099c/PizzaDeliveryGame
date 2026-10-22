using TMPro;
using UnityEngine;
using UnityEngine.UI;
public sealed class FacilityScreenUI : MonoBehaviour
{
    [SerializeField] private FacilityManager facilities;
    [SerializeField] private TextMeshProUGUI[] descriptions;
    [SerializeField] private TextMeshProUGUI message;
    [SerializeField] private Button oven,bike,shop,small,medium,large;
    private void Start()
    {
        oven.onClick.AddListener(()=>facilities.Purchase(FacilityType.Oven));bike.onClick.AddListener(()=>facilities.Purchase(FacilityType.Bike));shop.onClick.AddListener(()=>facilities.Purchase(FacilityType.Expansion));
        small.onClick.AddListener(()=>facilities.Purchase(FacilityType.Advertising,1));medium.onClick.AddListener(()=>facilities.Purchase(FacilityType.Advertising,2));large.onClick.AddListener(()=>facilities.Purchase(FacilityType.Advertising,3));
    }
    private void Update()
    {
        descriptions[0].text=facilities.Description(FacilityType.Oven);
        descriptions[1].text=facilities.Description(FacilityType.Bike);
        descriptions[2].text=facilities.Description(FacilityType.Advertising);
        descriptions[3].text=facilities.Description(FacilityType.Expansion);
        oven.interactable=facilities.Rejection(FacilityType.Oven).Length==0;bike.interactable=facilities.Rejection(FacilityType.Bike).Length==0;shop.interactable=facilities.Rejection(FacilityType.Expansion).Length==0;
        small.interactable=facilities.Rejection(FacilityType.Advertising,1).Length==0;medium.interactable=facilities.Rejection(FacilityType.Advertising,2).Length==0;large.interactable=facilities.Rejection(FacilityType.Advertising,3).Length==0;message.text=facilities.LastMessage;
    }
}
