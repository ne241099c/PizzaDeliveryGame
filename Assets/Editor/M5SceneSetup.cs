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
    public static Button MakeButton(Transform parent,string name,string caption,float x,float y,float w,float h)
    {
        var rect=Rect(parent,name,x,y,w,h);rect.gameObject.AddComponent<Image>().color=new Color(.3f,.75f,.6f);
        var button=rect.gameObject.AddComponent<Button>();button.targetGraphic=rect.GetComponent<Image>();
        var text=Text(rect,"Label",caption,8,0,w-16,h,22);text.alignment=TextAlignmentOptions.Center;text.color=new Color(.05f,.1f,.13f);
        return button;
    }
}
