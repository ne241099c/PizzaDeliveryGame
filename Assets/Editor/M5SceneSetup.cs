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
        var label=card.GetComponentInChildren<TextMeshProUGUI>();
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
    public static Button MakeButton(Transform parent,string name,string caption,float x,float y,float w,float h)
    {
        var rect=Rect(parent,name,x,y,w,h);rect.gameObject.AddComponent<Image>().color=new Color(.3f,.75f,.6f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
        var text=Text(rect,"Label",caption,8,0,w-16,h,22);text.alignment=TextAlignmentOptions.Center;text.color=new Color(.05f,.1f,.13f);
        return button;
    }
}
