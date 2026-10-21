using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using static M4StageSceneSetup;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M6SceneSetup
{
    public static void AddAbilities()
    {
        var manager=GameObject.Find("GameManager");
        if(manager.GetComponent<CourierProgression>()==null)Undo.AddComponent<CourierProgression>(manager);
        var selector=manager.GetComponent<DeliveryPersonSelector>();var so=new SerializedObject(selector);so.FindProperty("rowHeight").floatValue=230;so.ApplyModifiedProperties();
        var content=(Transform)so.FindProperty("deliveryPersonListContent").objectReferenceValue;
        foreach(var button in content.GetComponentsInChildren<Button>(true))AddBars(button.gameObject,250,false);
        string path=AssetDatabase.GetAssetPath(so.FindProperty("deliveryPersonItemPrefab").objectReferenceValue);
        var prefab=PrefabUtility.LoadPrefabContents(path);try {AddBars(prefab,250,false);PrefabUtility.SaveAsPrefabAsset(prefab,path);}finally{PrefabUtility.UnloadPrefabContents(prefab);}
        var template=UnityEngine.Object.FindFirstObjectByType<Canvas>().transform.Find("Preparation/EmploymentRowTemplate");AddBars(template.gameObject,598,true);
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
    private static void AddBars(GameObject row,float width,bool wide)
    {
        if(row.GetComponent<CourierAbilitiesView>()!=null)return;
        var layout=row.GetComponent<LayoutElement>()??row.AddComponent<LayoutElement>();layout.minHeight=layout.preferredHeight=wide?270:230;
        var labels=new List<TextMeshProUGUI>();var fills=new List<RectTransform>();var limits=new List<RectTransform>();var xp=new List<RectTransform>();
        for(int i=0;i<6;i++)
        {
            float x=wide?12+(i%2)*286:8,y=wide?122+(i/2)*46:68+i*25,w=wide?270:width-16;
            var label=Text(row.transform,"Ability"+i,"Ability / Potential / XP",x,y,w,19,wide?15:12);label.color=wide?Color.white:new Color(.1f,.14f,.2f);labels.Add(label);
            var bg=Rect(row.transform,"AbilityBar"+i,x,y+19,w,4);bg.gameObject.AddComponent<Image>().color=new Color(.1f,.12f,.15f);bg.GetComponent<Image>().raycastTarget=false;
            var fill=Rect(bg,"Value",0,0,w,4);fill.anchorMin=Vector2.zero;fill.anchorMax=Vector2.one;fill.offsetMin=fill.offsetMax=Vector2.zero;fill.gameObject.AddComponent<Image>().color=new Color(.15f,.7f,.55f);fill.GetComponent<Image>().raycastTarget=false;fills.Add(fill);
            var mark=Rect(bg,"Potential",w,0,2,8);mark.anchorMin=mark.anchorMax=new Vector2(1,.5f);mark.pivot=new Vector2(.5f,.5f);mark.anchoredPosition=Vector2.zero;mark.gameObject.AddComponent<Image>().raycastTarget=false;limits.Add(mark);
            var experience=Rect(row.transform,"XP"+i,x,y+25,w,2);experience.gameObject.AddComponent<Image>().color=new Color(.1f,.12f,.15f);experience.GetComponent<Image>().raycastTarget=false;
            var ef=Rect(experience,"Progress",0,0,w,2);ef.anchorMin=Vector2.zero;ef.anchorMax=Vector2.one;ef.offsetMin=ef.offsetMax=Vector2.zero;ef.gameObject.AddComponent<Image>().color=new Color(.95f,.7f,.25f);ef.GetComponent<Image>().raycastTarget=false;xp.Add(ef);
        }
        var view=row.AddComponent<CourierAbilitiesView>();var so=new SerializedObject(view);
        SetArray(so,"labels",labels.ToArray());SetArray(so,"fills",fills.ToArray());SetArray(so,"limits",limits.ToArray());SetArray(so,"experienceFills",xp.ToArray());so.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void SetArray(SerializedObject so,string name,UnityEngine.Object[] values)
    {var array=so.FindProperty(name);array.arraySize=values.Length;for(int i=0;i<values.Length;i++)array.GetArrayElementAtIndex(i).objectReferenceValue=values[i];}
    public static void AddProgression()
    {
        var m=GameObject.Find("GameManager");var progression=m.GetComponent<CourierProgression>();
        Bind(progression,"flow",m.GetComponent<DeliveryFlowController>(),"roster",m.GetComponent<DeliveryPersonSelector>());
        Bind(m.GetComponent<CampaignManager>(),"progression",progression);
        Bind(m.GetComponent<BusinessEconomy>(),"clock",m.GetComponent<GameClock>());
        EditorSceneManager.MarkSceneDirty(m.scene);
    }
    public static void AddTraining()
    {
        var m=GameObject.Find("GameManager");var training=m.GetComponent<TrainingManager>()??Undo.AddComponent<TrainingManager>(m);
        Bind(training,"clock",m.GetComponent<GameClock>(),"roster",m.GetComponent<DeliveryPersonSelector>(),"economy",m.GetComponent<BusinessEconomy>(),"progression",m.GetComponent<CourierProgression>());
        Bind(m.GetComponent<CampaignManager>(),"training",training);Bind(m.GetComponent<RecruitmentManager>(),"training",training);Bind(m.GetComponent<DeliveryPersonSelector>(),"training",training);
        var panel=UnityEngine.Object.FindFirstObjectByType<Canvas>().transform.Find("Preparation");Bind(panel.GetComponent<PreparationUI>(),"training",training);
        var template=panel.Find("EmploymentRowTemplate");
        if(template.Find("TrainingControls")==null)
        {
            var layout=template.GetComponent<LayoutElement>();Undo.RecordObject(layout,"Training row height");layout.minHeight=layout.preferredHeight=375;
            var controls=Rect(template,"TrainingControls",8,278,582,90);
            for(int i=0;i<6;i++)
            {
                var b=M5SceneSetup.MakeButton(controls,"Train"+i,"Train "+(CourierAbility)i,(i%3)*194,(i/3)*44,186,40);b.GetComponentInChildren<TextMeshProUGUI>().fontSize=13;
            }
            var cancel=M5SceneSetup.MakeButton(template,"CancelTraining","Cancel",486,84,98,30);cancel.GetComponentInChildren<TextMeshProUGUI>().fontSize=16;
        }
        EditorSceneManager.MarkSceneDirty(m.scene);
    }
    public static void AddSaveGame()
    {
        var m=GameObject.Find("GameManager");var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
        var save=m.GetComponent<CampaignSaveManager>()??Undo.AddComponent<CampaignSaveManager>(m);
        var panel=canvas.transform.Find("SaveChoice");
        if(panel==null)
        {
            var bg=Rect(canvas.transform,"SaveChoice",0,0,1280,840);bg.gameObject.AddComponent<Image>().color=new Color(.04f,.07f,.12f,.98f);panel=bg;
            Text(panel,"ChoiceText","CONTINUE CAMPAIGN?",320,230,640,140,30);
            M5SceneSetup.MakeButton(panel,"Continue","Continue",360,420,240,64);M5SceneSetup.MakeButton(panel,"NewGame","Start new",680,420,240,64);panel.gameObject.SetActive(false);
            Text(canvas.transform.Find("Preparation"),"SaveNotice","",650,8,590,42,15);
        }
        Bind(save,"campaign",m.GetComponent<CampaignManager>(),"clock",m.GetComponent<GameClock>(),"economy",m.GetComponent<BusinessEconomy>(),"reputation",m.GetComponent<ReputationManager>(),"roster",m.GetComponent<DeliveryPersonSelector>(),"recruitment",m.GetComponent<RecruitmentManager>(),"training",m.GetComponent<TrainingManager>(),"spawner",m.GetComponent<OrderSpawner>(),"choicePanel",panel.gameObject,"continueButton",panel.Find("Continue").GetComponent<Button>(),"newButton",panel.Find("NewGame").GetComponent<Button>(),"choiceText",panel.Find("ChoiceText").GetComponent<TextMeshProUGUI>(),"notice",canvas.transform.Find("Preparation/SaveNotice").GetComponent<TextMeshProUGUI>());
        Bind(m.GetComponent<CampaignManager>(),"saveGame",save);canvas.transform.Find("DayResults").SetAsLastSibling();EditorSceneManager.MarkSceneDirty(m.scene);
    }
    public static void FinalizeScene()
    {
        var manager=GameObject.Find("GameManager");Bind(manager.GetComponent<RecruitmentManager>(),"dynamics",manager.GetComponent<DeliveryDynamics>());
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
    public static void Apply()
    {
        var manager=GameObject.Find("GameManager");
        M4StageSceneSetup.Bind(manager.GetComponent<OrderSpawner>(),"stats",manager.GetComponent<DayStats>());
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
}
