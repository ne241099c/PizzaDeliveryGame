using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using static M4StageSceneSetup;

public static class ScreensSceneSetup
{
    private static Canvas Canvas => UnityEngine.Object.FindFirstObjectByType<Canvas>();
    private static GameObject Manager => GameObject.Find("GameManager");
    public static RectTransform Full(Transform parent, string name)
    {
        var found = parent.Find(name);
        var r = found != null ? (RectTransform)found : new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        if (found == null) { Undo.RegisterCreatedObjectUndo(r.gameObject,"Screen root"); r.SetParent(parent,false); }
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;
        return r;
    }
    public static void Bind(UnityEngine.Object target, string property, UnityEngine.Object value)
    { var so=new SerializedObject(target);so.FindProperty(property).objectReferenceValue=value;so.ApplyModifiedProperties(); }
    public static void Place(Transform t, float x, float y, float width, float height)
    {
        var r=(RectTransform)t;Undo.RecordObject(r,"Screen layout");r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);
    }
    public static Transform Find(string name)=>UIVisibility.Find(Canvas.transform,name);
    private static void Move(Transform t, Transform parent) => Undo.SetTransformParent(t,parent,"Move existing UI");
    public static void Shell()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var canvas=Canvas;Undo.RecordObject(canvas,"Screen shell");
        var roots=Enum.GetNames(typeof(ScreenId)).Select(name=>Full(canvas.transform,name+"Screen")).ToArray();
        foreach(var root in roots)if(root.GetComponent<CanvasGroup>()==null)Undo.AddComponent<CanvasGroup>(root.gameObject);
        var shared=Full(canvas.transform,"SharedHeader");if(shared.GetComponent<CanvasGroup>()==null)shared.gameObject.AddComponent<CanvasGroup>();
        foreach(var t in canvas.transform.Cast<Transform>().ToArray())
        {
            if(roots.Contains(t) || t==shared)continue;
            var parent=t.name=="Preparation"?roots[1]:t.name=="DayResults"?roots[3]:t.name=="SaveChoice"?roots[0]:t.name=="Cash" || t.name=="CampaignDay"?shared:roots[2];
            Move(t,parent);
            if(t.name=="Preparation" || t.name=="DayResults" || t.name=="SaveChoice") {t.gameObject.SetActive(true);if(t.GetComponent<CanvasGroup>()==null)t.gameObject.AddComponent<CanvasGroup>();}
        }
        var router=Manager.GetComponent<ScreenRouter>()??Undo.AddComponent<ScreenRouter>(Manager);
        Bind(router,"campaign",Manager.GetComponent<CampaignManager>());Bind(router,"clock",Manager.GetComponent<GameClock>());Bind(router,"sharedHeader",shared.GetComponent<CanvasGroup>());
        var so=new SerializedObject(router);var array=so.FindProperty("screens");array.arraySize=roots.Length;for(int i=0;i<roots.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=roots[i].GetComponent<CanvasGroup>();so.ApplyModifiedProperties();
        foreach(var root in roots)UIVisibility.Set(root.gameObject,root.name=="HubScreen");
        UIVisibility.Set(Find("DayResults").gameObject,false);UIVisibility.Set(Find("SaveChoice").gameObject,false);
        EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
    private static void Grid(Transform viewport)
    {
        var scroll=viewport.GetComponent<ScrollRect>();var content=scroll.content;
        var old=content.GetComponent<VerticalLayoutGroup>();if(old!=null)Undo.DestroyObjectImmediate(old);
        var grid=content.GetComponent<GridLayoutGroup>()??Undo.AddComponent<GridLayoutGroup>(content.gameObject);
        grid.constraint=GridLayoutGroup.Constraint.FixedColumnCount;grid.constraintCount=2;grid.cellSize=new Vector2(680,330);grid.spacing=new Vector2(24,24);
        content.anchorMin=new Vector2(0,1);content.anchorMax=new Vector2(1,1);content.pivot=new Vector2(.5f,1);content.anchoredPosition=Vector2.zero;content.sizeDelta=new Vector2(0,0);
    }
    public static void Hub()
    {
        var canvas=Canvas;canvas.GetComponent<CanvasScaler>().referenceResolution=new Vector2(1920,1080);
        var panel=Find("Preparation");Place(panel,0,88,1920,992);
        Place(Find("PreparationTitle"),32,16,1520,44);Place(Find("PreparationOverview"),32,68,1520,40);
        Place(Find("StartBusiness"),1500,900,370,64);Place(Find("EmploymentMessage"),256,812,1600,72);
        var staff=Full(panel,"StaffTab");var applicants=Full(panel,"ApplicantsTab");var management=Full(panel,"ManagementTab");
        foreach(var tab in new[]{staff,applicants,management}){Place(tab,256,150,1500,650);if(tab.GetComponent<CanvasGroup>()==null)tab.gameObject.AddComponent<CanvasGroup>();}
        Move(Find("TeamViewport"),staff);Place(Find("TeamViewport"),0,0,1440,650);Grid(Find("TeamViewport"));
        Move(Find("ApplicantsViewport"),applicants);Place(Find("ApplicantsViewport"),0,0,1440,650);Grid(Find("ApplicantsViewport"));
        UIVisibility.Set(Find("TeamTitle").gameObject,false);UIVisibility.Set(Find("ApplicantsTitle").gameObject,false);
        var buttons=new[]{M5SceneSetup.MakeButton(panel,"StaffButton","Staff",32,160,190,56),M5SceneSetup.MakeButton(panel,"ApplicantsButton","Applicants",32,232,190,56),M5SceneSetup.MakeButton(panel,"ManagementButton","Management",32,304,190,56)};
        var forecast=Text(panel,"Forecast","Today",32,902,1430,56,24);var history=Text(management,"DayHistory","Previous days",0,152,1400,490,24);
        var slot=Full(Find("BusinessScreen"),"BusinessPolicySlot");Place(slot,320,16,1280,100);
        var ui=Manager.GetComponent<HubScreenUI>()??Undo.AddComponent<HubScreenUI>(Manager);
        Bind(ui,"router",Manager.GetComponent<ScreenRouter>());Bind(ui,"campaign",Manager.GetComponent<CampaignManager>());Bind(ui,"clock",Manager.GetComponent<GameClock>());Bind(ui,"economy",Manager.GetComponent<BusinessEconomy>());Bind(ui,"roster",Manager.GetComponent<DeliveryPersonSelector>());Bind(ui,"forecast",forecast);Bind(ui,"history",history);Bind(ui,"policy",Find("DeliveryPolicy"));Bind(ui,"businessPolicySlot",slot);
        var so=new SerializedObject(ui);var tabs=so.FindProperty("tabs");tabs.arraySize=3;var bs=so.FindProperty("tabButtons");bs.arraySize=3;
        var groups=new[]{staff,applicants,management};for(int i=0;i<3;i++){tabs.GetArrayElementAtIndex(i).objectReferenceValue=groups[i].GetComponent<CanvasGroup>();bs.GetArrayElementAtIndex(i).objectReferenceValue=buttons[i];}so.ApplyModifiedProperties();
        var template=Find("EmploymentRowTemplate");var picker=template.gameObject.AddComponent<TrainingPickerUI>();var controls=template.Find("TrainingControls");var group=controls.GetComponent<CanvasGroup>();if(group==null)group=controls.gameObject.AddComponent<CanvasGroup>();
        var toggle=M5SceneSetup.MakeButton(template,"ChooseTraining","Training...",12,278,230,40);Bind(picker,"choices",group);Bind(picker,"toggle",toggle);
        Place(Find("Cash"),32,16,900,32);Place(Find("CampaignDay"),1100,16,720,32);
        foreach(var tab in groups)UIVisibility.Set(tab.gameObject,tab==staff);
        var stage=new SerializedObject(Manager.GetComponent<ScreenRouter>());stage.FindProperty("implementationStage").intValue=2;stage.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(Manager.scene);
    }
}
