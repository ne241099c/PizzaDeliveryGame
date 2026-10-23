using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>One-time split of the single SampleScene into the Title and Game scenes (2026-10-23), kept as the record
/// of how they were made and to rebuild the Title scene. The scenes are the source afterwards; edit them directly.</summary>
public static class GameScenesBuilder
{
    public const string TitlePath="Assets/Scenes/Title.unity";
    public const string GamePath="Assets/Scenes/Game.unity";
    private const string OldGamePath="Assets/Scenes/SampleScene.unity";
    /// <summary>Screens of the first UI, replaced by the prefab screens under ShopUI.</summary>
    private static readonly string[] LegacyRoots={"TitleScreen","HubScreen","BusinessScreen","SettlementScreen","GameOverScreen","SharedHeader","OverlayScreens"};

    [MenuItem("Tools/Scenes/Split Into Title And Game")]
    public static void SplitMenu()=>Debug.Log(Split());
    [MenuItem("Tools/Scenes/Rebuild Title Scene")]
    public static void TitleMenu()=>Debug.Log(BuildTitleScene());

    public static string Split()
    {
        if(Application.isPlaying)return "exit Play Mode first";
        var log=new List<string>();
        if(File.Exists(OldGamePath) && !File.Exists(GamePath))
        {
            if(SceneManager.GetActiveScene().path==OldGamePath && SceneManager.GetActiveScene().isDirty)return "save or discard "+OldGamePath+" first";
            string moved=AssetDatabase.MoveAsset(OldGamePath,GamePath);if(moved.Length>0)return "rename failed: "+moved;
            log.Add("renamed SampleScene -> Game");
        }
        var game=EditorSceneManager.OpenScene(GamePath,OpenSceneMode.Single);
        log.Add(CleanGameScene(game));
        EditorSceneManager.SaveScene(game);
        log.Add(RemoveMissingScripts(TitleViewBuilder.PrefabPath));
        log.Add(BuildTitleScene());
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(TitlePath,true),new EditorBuildSettingsScene(GamePath,true)};
        log.Add("build settings: Title, Game");
        EditorSceneManager.OpenScene(GamePath,OpenSceneMode.Single);
        return string.Join(" | ",log);
    }

    /// <summary>Removes the first UI's screens and the components whose scripts were deleted, and points the router at
    /// the prefab screens (Hub, Business, Settlement, GameOver under ShopUI).</summary>
    private static string CleanGameScene(Scene scene)
    {
        var canvas=Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);if(canvas==null)return "missing Canvas";
        var shop=Object.FindFirstObjectByType<ShopUiRoot>(FindObjectsInactive.Include);if(shop==null)return "missing ShopUI";
        int removedRoots=0,removedChildren=0,missing=0;
        foreach(string name in LegacyRoots)
        {
            var root=canvas.transform.Find(name);
            if(root!=null)
            {
                Object.DestroyImmediate(root.gameObject);
                removedRoots++;
            }
        }
        // The modal panels keep their CanvasGroup; their first layout (hidden children) goes.
        foreach(string name in new[]{"PauseMenu","HelpOverlay","ReturnTitleConfirmation"})
        {
            var panel=shop.transform.Find(name);if(panel==null)continue;
            for(int i=panel.childCount-1;i>=0;i--){Object.DestroyImmediate(panel.GetChild(i).gameObject);removedChildren++;}
        }
        foreach(var go in scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).Select(t=>t.gameObject))
            missing+=GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        // The in-game menu's modal panels (an empty CanvasGroup each; GameMenu builds the cards).
        var menu=Object.FindFirstObjectByType<GameMenu>(FindObjectsInactive.Include);
        if(menu!=null)
        {
            var menuSettings=new SerializedObject(menu);
            foreach(var (field,name) in new[]{("menu","PauseMenu"),("help","HelpOverlay"),("confirmation","ReturnTitleConfirmation")})
            {
                var p=menuSettings.FindProperty(field);if(p.objectReferenceValue!=null)continue;
                var panel=shop.transform.Find(name);if(panel==null){panel=UiKit.Stretch(shop.transform,name);panel.gameObject.AddComponent<Image>();}
                var group=UiKit.Group((RectTransform)panel);group.alpha=0;group.interactable=group.blocksRaycasts=false;p.objectReferenceValue=group;
            }
            menuSettings.ApplyModifiedPropertiesWithoutUndo();
        }
        var router=Object.FindFirstObjectByType<ScreenRouter>(FindObjectsInactive.Include);
        var screens=new[]{"HubView","BusinessView","SettlementView","GameOverView"}.Select(n=>shop.transform.Find(n)?.GetComponent<CanvasGroup>()).ToArray();
        if(screens.Any(s=>s==null))return "missing a prefab screen under ShopUI";
        var so=new SerializedObject(router);var list=so.FindProperty("screens");list.arraySize=screens.Length;
        for(int i=0;i<screens.Length;i++)list.GetArrayElementAtIndex(i).objectReferenceValue=screens[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        return $"Game scene: removed {removedRoots} legacy screens, {removedChildren} old modal children, {missing} missing-script components; router -> {string.Join(",",screens.Select(s=>s.name))}";
    }
    private static string RemoveMissingScripts(string prefabPath)
    {
        var root=PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            int removed=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject));
            if(removed>0)PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            return $"{Path.GetFileName(prefabPath)}: {removed} missing scripts removed";
        }
        finally{PrefabUtility.UnloadPrefabContents(root);}
    }

    /// <summary>Camera, EventSystem, a 1920x1080 Canvas with the title prefab, an overlay layer for modals and TitleScene.</summary>
    public static string BuildTitleScene()
    {
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(TitleViewBuilder.PrefabPath);
        if(prefab==null)return "missing "+TitleViewBuilder.PrefabPath;
        var credits=AssetDatabase.LoadAssetAtPath<TextAsset>(GameInfoSync.CreditsAsset);if(credits==null)return "missing "+GameInfoSync.CreditsAsset;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var cameraObject=new GameObject("Main Camera",typeof(Camera));cameraObject.tag="MainCamera";
        var camera=cameraObject.GetComponent<Camera>();
        camera.orthographic=true;
        camera.clearFlags=CameraClearFlags.SolidColor;
        camera.backgroundColor=UiKit.T.line;
        camera.cullingMask=0;
        new GameObject("EventSystem",typeof(EventSystem),typeof(InputSystemUIInputModule));
        var canvasObject=new GameObject("Canvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        canvasObject.layer=5;
        canvasObject.GetComponent<Canvas>().renderMode=RenderMode.ScreenSpaceOverlay;
        var scaler=canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1920,1080);
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
        var view=(GameObject)PrefabUtility.InstantiatePrefab(prefab,canvasObject.transform);
        var rect=(RectTransform)view.transform;rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
        var overlay=UiKit.Stretch(canvasObject.transform,"Overlay");
        var controller=canvasObject.AddComponent<TitleScene>();var so=new SerializedObject(controller);
        so.FindProperty("view").objectReferenceValue=view.GetComponent<TitleView>();
        so.FindProperty("creditsText").objectReferenceValue=credits;
        so.FindProperty("overlay").objectReferenceValue=overlay;
        so.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.SaveScene(scene,TitlePath);
        return "saved "+TitlePath;
    }
}
