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
}
