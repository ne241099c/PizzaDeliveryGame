using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Styles and translates existing and pooled views without changing their actions or data.</summary>
[DefaultExecutionOrder(1000)]
public sealed class UIThemeDriver : MonoBehaviour
{
    [SerializeField] private Canvas canvas;
    [SerializeField] private int implementationStage=1;
    public int ImplementationStage=>implementationStage;
    private static readonly string[] primary={"StartBusiness","DispatchRoute","DispatchSuggestion","ReplayDay","Continue","AdvancePage"};
    private static readonly string[] danger={"NewGame","RetryCampaign","Fire","ConfirmTitle"};
    public static bool RenderingSuspended {get;set;}
    private void LateUpdate(){if(!RenderingSuspended)Refresh();}
    public void Refresh()
    {
        var theme=UITheme.Active;if(theme==null || canvas==null)return;
        var shop=canvas.GetComponentInChildren<ShopUiRoot>(true);var skip=shop!=null?shop.transform:null;
        var optOut=canvas.GetComponentsInChildren<ThemeOptOut>(true);
        bool Skipped(Transform t){if(skip!=null && t.IsChildOf(skip))return true;foreach(var o in optOut)if(t.IsChildOf(o.transform))return true;return false;}
        foreach(var text in canvas.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if(!text.gameObject.activeInHierarchy || text.canvasRenderer.GetInheritedAlpha()<=0 || text.text==null || Skipped(text.transform))continue;
            var component=text.GetComponent<ThemedText>();
            if(component==null){component=text.gameObject.AddComponent<ThemedText>();component.size=SizeFor(text);component.numeric=text.name.Contains("Cash") || text.name.Contains("Reputation");}
            if(text.GetComponentInParent<Button>()!=null || text.text=="PIZZA DELIVERY" || text.text=="ピザ配達ゲーム")component.size=SizeFor(text);
            component.Apply(theme);text.text=UiStrings.Japanese(text.text);
            text.text=System.Text.RegularExpressions.Regex.Replace(text.text,@"<size=\d+>","<size="+theme.caption+">");
        }
        foreach(var image in canvas.GetComponentsInChildren<Image>(true))
        {
            if(!image.gameObject.activeInHierarchy || image.canvasRenderer.GetInheritedAlpha()<=0 || Skipped(image.transform))continue;
            if(image.GetComponent<Button>()!=null)continue;
            string name=image.name;
            if(name.Contains("Fill") || name.Contains("Limit") || name.Contains("Band") || name.Contains("Handle"))
            {
                image.color=name.Contains("Handle")?theme.accent:name.Contains("Experience")?theme.money:name.Contains("Limit")?theme.ink:name.Contains("Perfect")?theme.money:theme.good;continue;
            }
            var panel=image.GetComponent<ThemedPanel>();if(panel==null){panel=image.gameObject.AddComponent<ThemedPanel>();panel.secondary=name.Contains("Card") || name.Contains("Viewport") || name.Contains("Bar");}panel.Apply(theme);
        }
        var list=FindFirstObjectByType<OrderListUI>();var roster=FindFirstObjectByType<DeliveryPersonSelector>();var hub=FindFirstObjectByType<HubScreenUI>();
        foreach(var button in canvas.GetComponentsInChildren<Button>(true))
        {
            if(!button.gameObject.activeInHierarchy || Skipped(button.transform))continue;
            var renderer=button.GetComponent<CanvasRenderer>();if(renderer==null || renderer.GetInheritedAlpha()<=0)continue;
            var styled=button.GetComponent<ThemedButton>();if(styled==null)styled=button.gameObject.AddComponent<ThemedButton>();
            styled.kind=primary.Contains(button.name)?ThemeButtonKind.Primary:danger.Contains(button.name)?ThemeButtonKind.Danger:ThemeButtonKind.Secondary;
            var label=button.GetComponentInChildren<TMP_Text>(true);if(label!=null && label.text=="解雇")styled.kind=ThemeButtonKind.Danger;
            var card=button.GetComponent<CookingCardUI>();if(card!=null && button.GetComponent<OrderDeadlineView>()==null)button.gameObject.AddComponent<OrderDeadlineView>();if(card!=null && card.BoundOrder!=null && list.SelectedOrders.Contains(card.BoundOrder))styled.kind=ThemeButtonKind.Selected;
            if(button.transform.parent!=null && button.transform.parent.name=="DeliveryPersonListContent" && roster.SelectedPerson!=null && label!=null && label.text.StartsWith(roster.SelectedPerson.Name+" /"))styled.kind=ThemeButtonKind.Selected;
            var tabNames=new[]{"StaffButton","ApplicantsButton","ManagementButton","FacilitiesButton"};if(hub.SelectedTab>=0 && hub.SelectedTab<tabNames.Length && button.name==tabNames[hub.SelectedTab])styled.kind=ThemeButtonKind.Selected;
            styled.Apply(theme);
        }
    }
    public static ThemeTextSize SizeFor(TMP_Text text)
    {
        if(text.text=="PIZZA DELIVERY" || text.text=="ピザ配達ゲーム")return ThemeTextSize.Heading;
        // Compact rows inside cards (abilities, bake quality) must win over the button rule.
        if(text.name.StartsWith("Ability") || text.name.Contains("Profile") || text.name=="BakeQuality")return ThemeTextSize.Caption;
        // Narrow buttons and the two-line training/suggestion buttons use caption; tabs and other buttons stay readable at 720p.
        var button=text.GetComponentInParent<Button>();if(button!=null)return button.name=="UseSuggestedRoute" || button.name.StartsWith("Train") || button.GetComponent<RectTransform>().rect.width<150?ThemeTextSize.Caption:ThemeTextSize.Body;
        if(text.name.Contains("Title") || text.name.Contains("Heading"))return ThemeTextSize.Heading;
        if(text.name.Contains("Ability") || text.name.Contains("Profile") || text.fontSize<18)return ThemeTextSize.Caption;
        if(text.fontSize>=22)return ThemeTextSize.Subheading;return ThemeTextSize.Body;
    }
}
