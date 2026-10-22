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
}
