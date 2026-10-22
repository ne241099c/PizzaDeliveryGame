using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
public static class VisualSceneSetup
{
    public static void Foundation()
    {
        Directory.CreateDirectory("Assets/Resources");Directory.CreateDirectory("Assets/Fonts");AssetDatabase.Refresh();
        var font=AssetDatabase.LoadAssetAtPath<Font>("Assets/Fonts/ZenMaruGothic-Regular.ttf");
        var tmp=TMP_FontAsset.CreateFontAsset(font,48,5,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,AtlasPopulationMode.Dynamic,true);
        tmp.isMultiAtlasTexturesEnabled=true;tmp.name="Zen Maru Gothic Dynamic";AssetDatabase.CreateAsset(tmp,"Assets/Fonts/ZenMaruGothic Dynamic.asset");
        foreach(var atlas in tmp.atlasTextures){atlas.name="ZenMaru Atlas";AssetDatabase.AddObjectToAsset(atlas,tmp);}AssetDatabase.AddObjectToAsset(tmp.material,tmp);
        var theme=ScriptableObject.CreateInstance<UITheme>();theme.font=tmp;AssetDatabase.CreateAsset(theme,"Assets/Resources/UITheme.asset");
        var texture=new Texture2D(64,64,TextureFormat.RGBA32,false);texture.name="Rounded panel (original)";
        for(int y=0;y<64;y++)for(int x=0;x<64;x++){float dx=Mathf.Max(theme.cornerRadius-x,0,x-(63-theme.cornerRadius)),dy=Mathf.Max(theme.cornerRadius-y,0,y-(63-theme.cornerRadius));texture.SetPixel(x,y,new Color(1,1,1,dx*dx+dy*dy<=theme.cornerRadius*theme.cornerRadius?1:0));}texture.Apply();AssetDatabase.AddObjectToAsset(texture,theme);
        theme.roundedPanel=Sprite.Create(texture,new Rect(0,0,64,64),Vector2.one*.5f,100,0,SpriteMeshType.FullRect,Vector4.one*theme.cornerRadius);theme.roundedPanel.name="RoundedPanel";AssetDatabase.AddObjectToAsset(theme.roundedPanel,theme);EditorUtility.SetDirty(theme);AssetDatabase.SaveAssets();
    }
    public static void ApplyViews()
    {
        var theme=UITheme.Active;var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
        StyleHierarchy(canvas.gameObject,theme);
        foreach(string path in new[]{"Assets/Prefabs/OrderItem.prefab","Assets/Prefabs/DeliveryPersonButton.prefab"})
        {
            var root=PrefabUtility.LoadPrefabContents(path);try{StyleHierarchy(root,theme);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        }
        var driver=GameObject.Find("GameManager").GetComponent<UIThemeDriver>();if(driver==null)driver=Undo.AddComponent<UIThemeDriver>(GameObject.Find("GameManager"));ScreensSceneSetup.Bind(driver,"canvas",canvas);EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);AssetDatabase.SaveAssets();
    }
    private static void StyleHierarchy(GameObject root,UITheme theme)
    {
        HideDuplicateAbilityBars(root);
        foreach(var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            var style=text.GetComponent<ThemedText>();if(style==null){style=text.gameObject.AddComponent<ThemedText>();style.size=UIThemeDriver.SizeFor(text);style.numeric=text.name.Contains("Cash") || text.name.Contains("Reputation");}style.Apply(theme);EditorUtility.SetDirty(text);
        }
        foreach(var button in root.GetComponentsInChildren<Button>(true)){var style=button.GetComponent<ThemedButton>();if(style==null)style=button.gameObject.AddComponent<ThemedButton>();style.Apply(theme);}
        foreach(var image in root.GetComponentsInChildren<Image>(true))if(image.GetComponent<Button>()==null && !image.name.Contains("Fill") && !image.name.Contains("Limit") && !image.name.Contains("Band") && !image.name.Contains("Handle")){var panel=image.GetComponent<ThemedPanel>();if(panel==null)panel=image.gameObject.AddComponent<ThemedPanel>();panel.secondary=image.name.Contains("Card") || image.name.Contains("Viewport");panel.Apply(theme);}
    }
    private static void HideDuplicateAbilityBars(GameObject root)
    {
        foreach(var view in root.GetComponentsInChildren<CourierAbilitiesView>(true))
        {
            // Old scene instances can contain both an overridden view and an inherited one.
            // The production presenter calls GetComponent, so retain that same view's targets.
            if(view!=view.GetComponent<CourierAbilitiesView>())continue;
            var so=new SerializedObject(view);var keep=new System.Collections.Generic.HashSet<Transform>();
            foreach(string field in new[]{"labels","fills","limits","experienceFills"})
            {
                var array=so.FindProperty(field);
                for(int i=0;i<array.arraySize;i++)
                {
                    var component=(Component)array.GetArrayElementAtIndex(i).objectReferenceValue;var t=component.transform;
                    while(t.parent!=null && t.parent!=view.transform)t=t.parent;keep.Add(t);
                }
            }
            foreach(Transform child in view.transform)
                if(System.Text.RegularExpressions.Regex.IsMatch(child.name,@"^(Ability\d+|AbilityBar\d+|XP\d+)$"))UIVisibility.Set(child.gameObject,keep.Contains(child));
        }
    }
    public static void Map()
    {
        var theme=new SerializedObject(UITheme.Active);theme.FindProperty("worldLabelSize").floatValue=2.5f;theme.ApplyModifiedProperties();AssetDatabase.SaveAssets();
        var go=GameObject.Find("GameManager");var visuals=go.GetComponent<MapVisuals>();if(visuals==null)visuals=Undo.AddComponent<MapVisuals>(go);ScreensSceneSetup.Bind(visuals,"map",UnityEngine.Object.FindFirstObjectByType<MapManager>());
        var so=new SerializedObject(go.GetComponent<UIThemeDriver>());so.FindProperty("implementationStage").intValue=2;so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(go.scene);
    }
    public static void Feedback()
    {
        var go=GameObject.Find("GameManager");var view=go.GetComponent<DeliveryFeedback>();if(view==null)view=Undo.AddComponent<DeliveryFeedback>(go);
        ScreensSceneSetup.Bind(view,"flow",go.GetComponent<DeliveryFlowController>());ScreensSceneSetup.Bind(view,"clock",go.GetComponent<GameClock>());ScreensSceneSetup.Bind(view,"score",go.GetComponent<ScoreManager>());ScreensSceneSetup.Bind(view,"cooking",go.GetComponent<CookingManager>());ScreensSceneSetup.Bind(view,"economy",go.GetComponent<BusinessEconomy>());ScreensSceneSetup.Bind(view,"reputation",go.GetComponent<ReputationManager>());ScreensSceneSetup.Bind(view,"roster",go.GetComponent<DeliveryPersonSelector>());ScreensSceneSetup.Bind(view,"settlement",go.GetComponent<SettlementScreenUI>());ScreensSceneSetup.Bind(view,"router",go.GetComponent<ScreenRouter>());ScreensSceneSetup.Bind(view,"map",UnityEngine.Object.FindFirstObjectByType<MapManager>());
        ScreensSceneSetup.Bind(view,"cashLabel",ScreensSceneSetup.Find("Cash").GetComponent<TMP_Text>());
        var promise=new SerializedObject(go.GetComponent<DeliveryPromiseUI>());ScreensSceneSetup.Bind(view,"reputationLabel",promise.FindProperty("reputationText").objectReferenceValue);
        var settlement=new SerializedObject(go.GetComponent<SettlementScreenUI>());ScreensSceneSetup.Bind(view,"settlementLabel",settlement.FindProperty("pageText").objectReferenceValue);
        var roster=new SerializedObject(go.GetComponent<DeliveryPersonSelector>());ScreensSceneSetup.Bind(view,"courierContent",roster.FindProperty("deliveryPersonListContent").objectReferenceValue);
        var timer=M4StageSceneSetup.Rect(ScreensSceneSetup.Find("Header"),"BusinessTimeBand",32,116,260,8);var bg=timer.gameObject.AddComponent<Image>();bg.color=UITheme.Active.block;bg.raycastTarget=false;var fill=ScreensSceneSetup.Full(timer,"TimeFill");var image=fill.gameObject.AddComponent<Image>();image.raycastTarget=false;ScreensSceneSetup.Bind(view,"timerFill",image);
        var so=new SerializedObject(go.GetComponent<UIThemeDriver>());so.FindProperty("implementationStage").intValue=3;so.ApplyModifiedProperties();EditorSceneManager.MarkSceneDirty(go.scene);
    }
}
