using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
public static class M7SceneSetup
{
    public static void Cooking()
    {
        var go=GameObject.Find("GameManager");var cook=Undo.AddComponent<CookingManager>(go);
        ScreensSceneSetup.Bind(cook,"clock",go.GetComponent<GameClock>());ScreensSceneSetup.Bind(cook,"orders",go.GetComponent<OrderListUI>());ScreensSceneSetup.Bind(go.GetComponent<OrderSpawner>(),"cooking",cook);
        var so=new SerializedObject(go.GetComponent<DaySimulationSettings>());so.FindProperty("resultsDirectory").stringValue="Docs/M7_Simulations/legacy";so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
    public static void Quality()
    {
        var go=GameObject.Find("GameManager");ScreensSceneSetup.Bind(go.GetComponent<DeliveryFlowController>(),"cooking",go.GetComponent<CookingManager>());
        var orderUI=go.GetComponent<OrderListUI>();var so=new SerializedObject(orderUI);so.FindProperty("rowHeight").floatValue=146;so.ApplyModifiedProperties();
        string path=AssetDatabase.GetAssetPath(so.FindProperty("orderItemPrefab").objectReferenceValue);var prefab=PrefabUtility.LoadPrefabContents(path);
        try
        {
            var view=prefab.AddComponent<CookingCardUI>();var bar=M4StageSceneSetup.Rect(prefab.transform,"BakingGauge",8,114,132,16);bar.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.gray;
            var fill=ScreensSceneSetup.Full(bar,"BakingFill");fill.gameObject.AddComponent<UnityEngine.UI.Image>().color=Color.yellow;
            var band=ScreensSceneSetup.Full(bar,"PerfectBand");band.gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.3f,.9f,.5f,.6f);
            var button=M5SceneSetup.MakeButton(prefab.transform,"TakeOut","Take out",148,108,100,30);button.GetComponentInChildren<TMPro.TextMeshProUGUI>().fontSize=14;
            var label=M4StageSceneSetup.Text(prefab.transform,"BakeQuality","",8,90,130,20,14);
            ScreensSceneSetup.Bind(view,"fill",fill);ScreensSceneSetup.Bind(view,"perfectBand",band);ScreensSceneSetup.Bind(view,"takeOut",button);ScreensSceneSetup.Bind(view,"quality",label);PrefabUtility.SaveAsPrefabAsset(prefab,path);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
    public static void Facilities()
    {
        var go=GameObject.Find("GameManager");var facilities=Undo.AddComponent<FacilityManager>(go);
        ScreensSceneSetup.Bind(facilities,"clock",go.GetComponent<GameClock>());ScreensSceneSetup.Bind(facilities,"economy",go.GetComponent<BusinessEconomy>());
        foreach(var component in new UnityEngine.Object[]{go.GetComponent<CampaignManager>(),go.GetComponent<BusinessEconomy>(),go.GetComponent<CampaignSaveManager>()})ScreensSceneSetup.Bind(component,"facilities",facilities);
        var saving=new SerializedObject(go.GetComponent<CampaignSaveManager>());saving.FindProperty("formatVersion").intValue=2;saving.ApplyModifiedProperties();
        var preparation=ScreensSceneSetup.Find("Preparation");var tab=ScreensSceneSetup.Full(preparation,"FacilitiesTab");ScreensSceneSetup.Place(tab,256,150,1500,650);var group=tab.gameObject.AddComponent<CanvasGroup>();
        var tabButton=M5SceneSetup.MakeButton(preparation,"FacilitiesButton","Facilities",32,376,190,56);
        var hub=new SerializedObject(go.GetComponent<HubScreenUI>());var tabs=hub.FindProperty("tabs");tabs.arraySize=4;tabs.GetArrayElementAtIndex(3).objectReferenceValue=group;var buttons=hub.FindProperty("tabButtons");buttons.arraySize=4;buttons.GetArrayElementAtIndex(3).objectReferenceValue=tabButton;hub.ApplyModifiedProperties();
        var ui=go.AddComponent<FacilityScreenUI>();ScreensSceneSetup.Bind(ui,"facilities",facilities);
        var cards=new RectTransform[4];var labels=new TMPro.TextMeshProUGUI[4];
        for(int n=0;n<4;n++){cards[n]=M4StageSceneSetup.Rect(tab,"FacilityCard"+n,(n%2)*736,(n/2)*310,712,290);cards[n].gameObject.AddComponent<UnityEngine.UI.Image>().color=new Color(.14f,.21f,.28f);labels[n]=M4StageSceneSetup.Text(cards[n],"FacilityDescription"+n,"",16,16,680,180,22);}
        var oven=M5SceneSetup.MakeButton(cards[0],"BuyOven","Buy oven",16,218,300,52);var bike=M5SceneSetup.MakeButton(cards[1],"BuyBike","Upgrade bikes",16,218,300,52);var small=M5SceneSetup.MakeButton(cards[2],"BuyAdSmall","Small",16,218,208,52);var medium=M5SceneSetup.MakeButton(cards[2],"BuyAdMedium","Medium",248,218,208,52);var large=M5SceneSetup.MakeButton(cards[2],"BuyAdLarge","Large",480,218,208,52);var shop=M5SceneSetup.MakeButton(cards[3],"BuyShop","Expand shop",16,218,300,52);
        var message=M4StageSceneSetup.Text(tab,"FacilityMessage","",0,620,1450,38,20);ScreensSceneSetup.Bind(ui,"message",message);ScreensSceneSetup.Bind(ui,"oven",oven);ScreensSceneSetup.Bind(ui,"bike",bike);ScreensSceneSetup.Bind(ui,"shop",shop);ScreensSceneSetup.Bind(ui,"small",small);ScreensSceneSetup.Bind(ui,"medium",medium);ScreensSceneSetup.Bind(ui,"large",large);
        var so=new SerializedObject(ui);var descriptions=so.FindProperty("descriptions");descriptions.arraySize=4;for(int n=0;n<4;n++)descriptions.GetArrayElementAtIndex(n).objectReferenceValue=labels[n];so.ApplyModifiedProperties();UIVisibility.Set(tab.gameObject,false);EditorSceneManager.MarkSceneDirty(go.scene);
    }
    public static void Effects()
    {
        var go=GameObject.Find("GameManager");var facilities=go.GetComponent<FacilityManager>();
        foreach(var component in new UnityEngine.Object[]{go.GetComponent<CookingManager>(),UnityEngine.Object.FindFirstObjectByType<MapManager>(),go.GetComponent<DeliveryDynamics>(),go.GetComponent<OrderSpawner>(),go.GetComponent<RecruitmentManager>()})ScreensSceneSetup.Bind(component,"facilities",facilities);
        ScreensSceneSetup.Bind(go.GetComponent<BusinessEconomy>(),"stats",go.GetComponent<DayStats>());
        var so=new SerializedObject(facilities);so.FindProperty("implementationStage").intValue=4;so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
