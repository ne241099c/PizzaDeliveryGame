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
}
