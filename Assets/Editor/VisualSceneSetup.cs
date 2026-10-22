using System;
using System.IO;
using System.Linq;
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
    /// <summary>V-4: one shared 1920x1080 layout that scales to 1280x720, plus a full-screen clear behind the map camera.</summary>
    public static void FinishLayout()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var theme=UITheme.Active;var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();
        EnsureClearCamera(theme);
        StretchButtonLabels(canvas.gameObject,theme);
        var path="Assets/Prefabs/DeliveryPersonButton.prefab";var root=PrefabUtility.LoadPrefabContents(path);
        try{StretchButtonLabels(root,theme);CourierCard(root,theme);PrefabUtility.SaveAsPrefabAsset(root,path);}finally{PrefabUtility.UnloadPrefabContents(root);}
        // Scene preview cards carry their own overridden header rects, so lay them out like the prefab.
        foreach(var card in canvas.GetComponentsInChildren<CourierAbilitiesView>(true).Where(v=>v==v.GetComponent<CourierAbilitiesView>() && v.GetComponent<Button>()!=null))
        {Undo.RegisterFullObjectHierarchyUndo(card.gameObject,"Courier card layout");CourierCard(card.gameObject,theme);}
        // Headings that were sized as page titles overflowed their bands.
        SetTextRole("OrdersTitle",ThemeTextSize.Subheading);ScreensSceneSetup.Place(ScreensSceneSetup.Find("OrdersTitle"),12,8,256,40);
        SetTextRole("CourierBandTitle",ThemeTextSize.Subheading);ScreensSceneSetup.Place(ScreensSceneSetup.Find("CourierBandTitle"),24,792,1800,34);
        SetTextRole("RouteTitle",ThemeTextSize.Subheading);ScreensSceneSetup.Place(ScreensSceneSetup.Find("RouteTitle"),16,10,300,34);
        SetTextRole("NextRoadsTitle",ThemeTextSize.Subheading);ScreensSceneSetup.Place(ScreensSceneSetup.Find("NextRoadsTitle"),16,204,350,32);
        ScreensSceneSetup.Place(ScreensSceneSetup.Find("UseSuggestedRoute"),16,410,350,46);
        ScreensSceneSetup.Place(ScreensSceneSetup.Find("Status"),12,2,360,54);
        foreach(string name in new[]{"GameName","ChoiceText"}){var text=ScreensSceneSetup.Find(name).GetComponent<TMP_Text>();Undo.RecordObject(text,"Center title");text.alignment=TextAlignmentOptions.Center;}
        TrainingPopup(theme);PromiseSlider(theme);
        var stage=new SerializedObject(GameObject.Find("GameManager").GetComponent<UIThemeDriver>());stage.FindProperty("implementationStage").intValue=4;stage.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);AssetDatabase.SaveAssets();
    }
    private static void EnsureClearCamera(UITheme theme)
    {
        var main=Camera.main;var existing=UnityEngine.Object.FindFirstObjectByType<ScreenClearCamera>();if(existing!=null)return;
        var go=new GameObject("ScreenClearCamera");Undo.RegisterCreatedObjectUndo(go,"Screen clear camera");
        var view=go.AddComponent<Camera>();view.depth=main.depth-1;view.rect=new Rect(0,0,1,1);view.cullingMask=0;view.clearFlags=CameraClearFlags.SolidColor;view.backgroundColor=theme.background;view.orthographic=true;
        go.AddComponent<ScreenClearCamera>();
    }
    private static void StretchButtonLabels(GameObject root,UITheme theme)
    {
        foreach(var button in root.GetComponentsInChildren<Button>(true))
        {
            // Composite cards lay out their own text.
            if(button.GetComponent<CourierAbilitiesView>()!=null || button.GetComponent<CookingCardUI>()!=null)continue;
            var labels=button.GetComponentsInChildren<TMP_Text>(true);if(labels.Length!=1 || labels[0].transform.parent!=button.transform)continue;
            var rect=labels[0].rectTransform;Undo.RecordObject(rect,"Stretch button label");Undo.RecordObject(labels[0],"Center button label");
            rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.pivot=new Vector2(.5f,.5f);rect.offsetMin=new Vector2(theme.smallSpace,theme.smallSpace/2f);rect.offsetMax=-rect.offsetMin;
            labels[0].alignment=TextAlignmentOptions.Center;EditorUtility.SetDirty(labels[0]);
        }
    }
    private static void CourierCard(GameObject root,UITheme theme)
    {
        var view=root.GetComponent<CourierAbilitiesView>();if(view==null)throw new InvalidOperationException("DeliveryPersonButton has no CourierAbilitiesView.");
        var layout=root.GetComponent<LayoutElement>();float width=layout!=null && layout.preferredWidth>0?layout.preferredWidth:((RectTransform)root.transform).rect.width;
        // The header rect is set at runtime by DeliveryPersonSelector.RebuildRoster (66 tall); only its alignment is authored here.
        var header=root.transform.Find("Text (TMP)");if(header!=null)header.GetComponent<TMP_Text>().alignment=TextAlignmentOptions.Top;
        // Saved scene cards keep an older StaminaBar placed over the first ability row; use the card's bottom edge like RebuildRoster's default.
        var stamina=root.transform.Find("StaminaBar") as RectTransform;
        if(stamina!=null){stamina.anchorMin=Vector2.zero;stamina.anchorMax=new Vector2(1,0);stamina.pivot=new Vector2(.5f,0);stamina.anchoredPosition=new Vector2(0,theme.smallSpace/2f);stamina.sizeDelta=new Vector2(-2*theme.smallSpace,theme.smallSpace/2f);}
        var so=new SerializedObject(view);
        for(int i=0;i<6;i++)
        {
            var label=(TMP_Text)so.FindProperty("labels").GetArrayElementAtIndex(i).objectReferenceValue;var fill=(RectTransform)so.FindProperty("fills").GetArrayElementAtIndex(i).objectReferenceValue;var xp=(RectTransform)so.FindProperty("experienceFills").GetArrayElementAtIndex(i).objectReferenceValue;
            float y=68+i*25,w=width-2*theme.smallSpace;
            PlaceLocal(label.transform,theme.smallSpace,y,w,19);PlaceLocal(fill.parent,theme.smallSpace,y+19,w,3);PlaceLocal(xp.parent,theme.smallSpace,y+23,w,2);
            var style=label.GetComponent<ThemedText>();if(style!=null){style.size=ThemeTextSize.Caption;style.Apply(theme);}
        }
    }
    // Prefab contents are not in the scene, so ScreensSceneSetup.Place (which records scene Undo) is not used here.
    private static void PlaceLocal(Transform t,float x,float y,float width,float height){var r=(RectTransform)t;r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,-y);r.sizeDelta=new Vector2(width,height);}
    private static void TrainingPopup(UITheme theme)
    {
        // The choices used to sit under the toggle button. Show them over the ability rows of the same row instead,
        // keeping their sibling order (PreparationUI takes the first Button as Fire/Hire, validation uses TrainingControls/Train0).
        var controls=ScreensSceneSetup.Find("TrainingControls");ScreensSceneSetup.Place(controls,8,122,582,144);
        var panel=controls.GetComponent<Image>();if(panel==null)panel=Undo.AddComponent<Image>(controls.gameObject);
        panel.sprite=theme.roundedPanel;panel.type=Image.Type.Sliced;panel.color=theme.background;
        if(controls.GetComponent<ThemedPanel>()==null)Undo.AddComponent<ThemedPanel>(controls.gameObject);
        for(int i=0;i<6;i++)ScreensSceneSetup.Place(controls.Find("Train"+i),16+(i%3)*184,16+(i/3)*60,176,52);
    }
    private static void PromiseSlider(UITheme theme)
    {
        var policy=new SerializedObject(UnityEngine.Object.FindFirstObjectByType<DeliveryPromiseUI>());var slider=(Slider)policy.FindProperty("promiseSlider").objectReferenceValue;
        if(slider==null)throw new InvalidOperationException("DeliveryPromiseUI has no promiseSlider.");
        // A 4px track is drawn by ThemedPanel's thin-bar branch; the former 9px track was painted the panel color and vanished.
        ScreensSceneSetup.Place(slider.transform.Find("Background"),0,8,404,4);
        var handle=slider.handleRect;Undo.RecordObject(handle,"Promise handle");handle.sizeDelta=new Vector2(theme.padding,-5);
    }
    private static void SetTextRole(string name,ThemeTextSize size){var style=ScreensSceneSetup.Find(name).GetComponent<ThemedText>();Undo.RecordObject(style,"Text role");style.size=size;style.Apply(UITheme.Active);EditorUtility.SetDirty(style);}
}
