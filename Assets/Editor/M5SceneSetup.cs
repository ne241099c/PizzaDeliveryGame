using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static M4StageSceneSetup;

public static class M5SceneSetup
{
    public static void AddCampaign()
    {
        if(Application.isPlaying)throw new System.InvalidOperationException("Stop Play first.");
        var manager=GameObject.Find("GameManager");var canvas=Object.FindFirstObjectByType<Canvas>();
        var campaign=manager.GetComponent<CampaignManager>()??Undo.AddComponent<CampaignManager>(manager);
        var so=new SerializedObject(manager.GetComponent<GameClock>());so.FindProperty("autoStart").boolValue=false;so.ApplyModifiedProperties();
        var panel=canvas.transform.Find("Preparation")?.GetComponent<RectTransform>()??Rect(canvas.transform,"Preparation",0,170,1280,670);
        if(panel.GetComponent<Image>()==null)panel.gameObject.AddComponent<Image>().color=new Color(.06f,.1f,.16f,.98f);
        var button=panel.Find("StartBusiness")?.GetComponent<Button>()??MakeButton(panel,"StartBusiness","Start business",490,500,300,56);
        var title=canvas.transform.Find("CampaignDay")?.GetComponent<TextMeshProUGUI>()??Text(canvas.transform,"CampaignDay","Day 1 / Preparation",760,52,470,40,22);
        Bind(campaign,"clock",manager.GetComponent<GameClock>(),"orders",manager.GetComponent<OrderListUI>(),"roster",manager.GetComponent<DeliveryPersonSelector>(),"flow",manager.GetComponent<DeliveryFlowController>(),"dynamics",manager.GetComponent<DeliveryDynamics>(),"spawner",manager.GetComponent<OrderSpawner>(),"reputation",manager.GetComponent<ReputationManager>(),"score",manager.GetComponent<ScoreManager>(),"stats",manager.GetComponent<DayStats>(),"planner",manager.GetComponent<DeliveryPlannerUI>(),"preparationPanel",panel.gameObject,"openButton",button,"dayText",title);
        var replay=canvas.transform.Find("DayResults/ResultCard/ReplayDay").GetComponentInChildren<TextMeshProUGUI>();Undo.RecordObject(replay,"Next day button");replay.text="Next day";
        canvas.transform.Find("DayResults").SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
    public static void AddEconomy()
    {
        var manager=GameObject.Find("GameManager");var canvas=Object.FindFirstObjectByType<Canvas>();
        var economy=manager.GetComponent<BusinessEconomy>()??Undo.AddComponent<BusinessEconomy>(manager);
        var label=canvas.transform.Find("Cash")?.GetComponent<TextMeshProUGUI>()??Text(canvas.transform,"Cash","Cash: 300.00",760,92,480,40,22);
        Bind(economy,"flow",manager.GetComponent<DeliveryFlowController>(),"roster",manager.GetComponent<DeliveryPersonSelector>(),"cashText",label);
        Bind(manager.GetComponent<CampaignManager>(),"economy",economy);
        canvas.transform.Find("DayResults").SetAsLastSibling();
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
    public static void AddSettlement()
    {
        var manager=GameObject.Find("GameManager");var selector=manager.GetComponent<DeliveryPersonSelector>();
        var so=new SerializedObject(selector);var entries=so.FindProperty("deliveryPeople");
        for(int i=entries.arraySize-1;i>=0;i--)
        {
            string name=entries.GetArrayElementAtIndex(i).FindPropertyRelative("Name").stringValue;
            if(name=="Minato" || name=="Sora")entries.DeleteArrayElementAtIndex(i);
        }
        so.ApplyModifiedProperties();
        var card=Object.FindFirstObjectByType<Canvas>().transform.Find("DayResults/ResultCard").GetComponent<RectTransform>();
        Undo.RecordObject(card,"Room for financial breakdown");card.anchoredPosition=new Vector2(190,-85);card.sizeDelta=new Vector2(900,700);
        var label=card.GetComponentsInChildren<TextMeshProUGUI>(true).First(t=>t.name=="ResultText");
        var viewport=card.Find("LedgerViewport")?.GetComponent<RectTransform>()??Rect(card,"LedgerViewport",28,20,844,570);
        if(viewport.GetComponent<Image>()==null)viewport.gameObject.AddComponent<Image>().color=new Color(0,0,0,.01f);
        if(viewport.GetComponent<RectMask2D>()==null)viewport.gameObject.AddComponent<RectMask2D>();
        if(label.transform.parent!=viewport)Undo.SetTransformParent(label.transform,viewport,"Scrollable settlement");
        Undo.RecordObject(label,"Settlement text");Undo.RecordObject(label.rectTransform,"Settlement content");
        label.fontSize=23;label.alignment=TextAlignmentOptions.TopLeft;
        var rt=label.rectTransform;rt.anchorMin=new Vector2(0,1);rt.anchorMax=new Vector2(1,1);rt.pivot=new Vector2(.5f,1);rt.anchoredPosition=Vector2.zero;rt.sizeDelta=new Vector2(0,570);
        var fitter=label.GetComponent<ContentSizeFitter>()??label.gameObject.AddComponent<ContentSizeFitter>();fitter.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.GetComponent<ScrollRect>()??viewport.gameObject.AddComponent<ScrollRect>();scroll.content=rt;scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
        var button=card.Find("ReplayDay").GetComponent<RectTransform>();Undo.RecordObject(button,"Settlement action");button.anchoredPosition=new Vector2(300,-620);
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
    public static void AddPreparation()
    {
        var manager=GameObject.Find("GameManager");var canvas=Object.FindFirstObjectByType<Canvas>();
        var panel=canvas.transform.Find("Preparation");
        var recruitment=manager.GetComponent<RecruitmentManager>()??Undo.AddComponent<RecruitmentManager>(manager);
        Bind(recruitment,"clock",manager.GetComponent<GameClock>(),"roster",manager.GetComponent<DeliveryPersonSelector>(),"economy",manager.GetComponent<BusinessEconomy>());
        Bind(manager.GetComponent<CampaignManager>(),"recruitment",recruitment);
        ConfigurePreparationLayout(panel,canvas);
        if(panel.Find("PreparationOverview")!=null){EditorSceneManager.MarkSceneDirty(manager.scene);return;}
        Text(panel,"PreparationTitle","PREPARATION / Build your team",28,8,1180,42,28);
        var overview=Text(panel,"PreparationOverview","Day 1 / Cash 300 / Reputation 50",28,55,1180,38,23);
        Text(panel,"TeamTitle","YOUR TEAM / scroll",28,100,580,32,21);
        Text(panel,"ApplicantsTitle","TODAY'S APPLICANTS / scroll",652,100,580,32,21);
        var team=MakeList(panel,"Team",28,138,598,366);var applicants=MakeList(panel,"Applicants",652,138,598,366);
        var message=Text(panel,"EmploymentMessage","Choose your team, then start business.",28,515,1180,58,21);
        var start=panel.Find("StartBusiness").GetComponent<RectTransform>();Undo.RecordObject(start,"Preparation start position");start.anchoredPosition=new Vector2(490,-592);
        var template=Rect(panel,"EmploymentRowTemplate",0,0,598,120);template.gameObject.AddComponent<Image>().color=new Color(.14f,.21f,.28f);
        var layout=template.gameObject.AddComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=120;
        Text(template,"Profile","Courier / Wage\nDrive / Know / Load\nService / Endurance / Safety\nHiring or severance",12,4,462,112,17);
        MakeButton(template,"Action","Hire",486,38,98,44);template.gameObject.SetActive(false);
        var ui=panel.gameObject.AddComponent<PreparationUI>();
        Bind(ui,"recruitment",recruitment,"roster",manager.GetComponent<DeliveryPersonSelector>(),"economy",manager.GetComponent<BusinessEconomy>(),"reputation",manager.GetComponent<ReputationManager>(),"campaign",manager.GetComponent<CampaignManager>(),"overview",overview,"message",message,"teamContent",team,"applicantContent",applicants,"rowTemplate",template.gameObject);
        ConfigurePreparationLayout(panel,canvas);
        canvas.transform.Find("DayResults").SetAsLastSibling();EditorSceneManager.MarkSceneDirty(manager.scene);
    }
    private static void ConfigurePreparationLayout(Transform panel,Canvas canvas)
    {
        var panelRect=panel.GetComponent<RectTransform>();Undo.RecordObject(panelRect,"Fit preparation below policy");
        panelRect.anchoredPosition=new Vector2(0,-202);panelRect.sizeDelta=new Vector2(1280,638);
        var image=panel.GetComponent<Image>();Undo.RecordObject(image,"Opaque preparation background");image.color=new Color(.06f,.1f,.16f,1);
        var cash=canvas.transform.Find("Cash").GetComponent<TextMeshProUGUI>();Undo.RecordObject(cash.rectTransform,"Cash in header");Undo.RecordObject(cash,"Cash font");
        cash.rectTransform.anchoredPosition=new Vector2(24,-49);cash.rectTransform.sizeDelta=new Vector2(680,27);cash.fontSize=21;
        var subtitle=canvas.transform.Find("Header/Subtitle");Undo.RecordObject(subtitle.gameObject,"Replace old subtitle with cash");subtitle.gameObject.SetActive(false);
        var day=canvas.transform.Find("CampaignDay").GetComponent<RectTransform>();Undo.RecordObject(day,"Day in header");day.anchoredPosition=new Vector2(760,-49);day.sizeDelta=new Vector2(480,27);
        var start=panel.Find("StartBusiness").GetComponent<RectTransform>();Undo.RecordObject(start,"Keep start button inside preparation");start.anchoredPosition=new Vector2(490,-574);start.sizeDelta=new Vector2(300,50);
        foreach(string name in new[]{"TeamViewport","ApplicantsViewport"})
        {
            var child=panel.Find(name)?.GetComponent<RectTransform>();if(child!=null){Undo.RecordObject(child,"Preparation list height");child.sizeDelta=new Vector2(598,350);}
        }
        var message=panel.Find("EmploymentMessage")?.GetComponent<RectTransform>();if(message!=null){Undo.RecordObject(message,"Preparation message");message.anchoredPosition=new Vector2(28,-501);}
    }
    private static RectTransform MakeList(Transform parent,string name,float x,float y,float width,float height)
    {
        var viewport=Rect(parent,name+"Viewport",x,y,width,height);viewport.gameObject.AddComponent<Image>().color=new Color(.03f,.06f,.1f);viewport.gameObject.AddComponent<RectMask2D>();
        var content=Rect(viewport,name+"Content",0,0,width,height);content.anchorMax=new Vector2(1,1);content.sizeDelta=new Vector2(0,height);
        var layout=content.gameObject.AddComponent<VerticalLayoutGroup>();layout.spacing=8;layout.childControlWidth=true;layout.childControlHeight=true;layout.childForceExpandHeight=false;
        content.gameObject.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
        var scroll=viewport.gameObject.AddComponent<ScrollRect>();scroll.content=content;scroll.viewport=viewport;scroll.horizontal=false;scroll.vertical=true;scroll.movementType=ScrollRect.MovementType.Clamped;scroll.scrollSensitivity=32;
        return content;
    }
    public static Button MakeButton(Transform parent,string name,string caption,float x,float y,float w,float h)
    {
        var rect=Rect(parent,name,x,y,w,h);rect.gameObject.AddComponent<Image>().color=new Color(.3f,.75f,.6f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
        var text=Text(rect,"Label",caption,8,0,w-16,h,22);text.alignment=TextAlignmentOptions.Center;text.color=new Color(.05f,.1f,.13f);
        return button;
    }
}
