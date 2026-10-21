using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
public static class M6SceneSetup
{
    public static void Apply()
    {
        var manager=GameObject.Find("GameManager");
        M4StageSceneSetup.Bind(manager.GetComponent<OrderSpawner>(),"stats",manager.GetComponent<DayStats>());
        EditorSceneManager.MarkSceneDirty(manager.scene);
    }
}
