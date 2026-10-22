using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Shop scene (店内シーン) setup through Editor APIs. Each step is idempotent and saves only through the scene.</summary>
public static class ShopSceneSetup
{
    private static GameObject Manager=>GameObject.Find("GameManager");
    private static T Ensure<T>(GameObject go) where T:Component{var c=go.GetComponent<T>();if(c==null)c=Undo.AddComponent<T>(go);return c;}
    private static void Bind(UnityEngine.Object target,string property,UnityEngine.Object value)
    {
        var so=new SerializedObject(target);var p=so.FindProperty(property);
        if(p==null)throw new InvalidOperationException(target.GetType().Name+" has no field "+property);
        p.objectReferenceValue=value;so.ApplyModifiedProperties();
    }
    /// <summary>S2 logic: kitchen staff roster, handover bags and the save format.</summary>
    public static void Logic()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var go=Manager;if(go==null)throw new InvalidOperationException("GameManager not found.");
        var crew=Ensure<KitchenCrew>(go);var handover=Ensure<HandoverManager>(go);
        Bind(handover,"orders",go.GetComponent<OrderListUI>());Bind(handover,"roster",go.GetComponent<DeliveryPersonSelector>());Bind(handover,"flow",go.GetComponent<DeliveryFlowController>());Bind(handover,"clock",go.GetComponent<GameClock>());
        var cooking=go.GetComponent<CookingManager>();Bind(cooking,"crew",crew);Bind(cooking,"handover",handover);
        Bind(go.GetComponent<BusinessEconomy>(),"crew",crew);Bind(go.GetComponent<RecruitmentManager>(),"crew",crew);
        var campaign=go.GetComponent<CampaignManager>();Bind(campaign,"crew",crew);Bind(campaign,"cooking",cooking);Bind(campaign,"handover",handover);
        var save=go.GetComponent<CampaignSaveManager>();Bind(save,"crew",crew);
        var so=new SerializedObject(save);so.FindProperty("formatVersion").intValue=3;so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(go.scene);
    }
    /// <summary>Redesigned screens (設計書_画面デザイン): fonts, art import, theme v2, new screen roots and the router.</summary>
    public static void Presentation()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play first.");
        var theme=UITheme.Active;if(theme==null)throw new InvalidOperationException("UITheme missing.");
        ImportArt();Fonts(theme);Theme(theme);
        var canvas=UnityEngine.Object.FindFirstObjectByType<Canvas>();if(canvas==null)throw new InvalidOperationException("Canvas missing.");
        var shop=canvas.transform.Find("ShopUI") as RectTransform;
        if(shop==null){shop=Rect(canvas.transform,"ShopUI");Undo.RegisterCreatedObjectUndo(shop.gameObject,"Shop UI");}
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(shop.gameObject);if(shop.GetComponent<ShopUiRoot>()==null)Undo.AddComponent<ShopUiRoot>(shop.gameObject);
        shop.SetAsLastSibling();
        var hub=View<HubView>(shop,"HubView");var business=View<BusinessView>(shop,"BusinessView");var settlement=View<SettlementView>(shop,"SettlementView");var gameOver=View<GameOverView>(shop,"GameOverView");
        var header=Rect(shop,"SharedHeaderV2",true);if(header.GetComponent<CanvasGroup>()==null)header.gameObject.AddComponent<CanvasGroup>();
        var mapArea=business.transform.Find("MapArea") as RectTransform;if(mapArea==null){mapArea=Rect(business.transform,"MapArea");mapArea.SetAsFirstSibling();}
        // Overlays (menu, help) stay above the new screens.
        var overlays=canvas.transform.Find("OverlayScreens");if(overlays!=null)overlays.SetAsLastSibling();
        var go=Manager;var router=go.GetComponent<ScreenRouter>();var so=new SerializedObject(router);var screens=so.FindProperty("screens");
        var old=new CanvasGroup[screens.arraySize];for(int i=0;i<old.Length;i++)old[i]=(CanvasGroup)screens.GetArrayElementAtIndex(i).objectReferenceValue;
        var oldHeader=(CanvasGroup)so.FindProperty("sharedHeader").objectReferenceValue;
        var title=old[(int)ScreenId.Title];if(title==null || title.name!="TitleScreen")title=canvas.transform.Find("TitleScreen").GetComponent<CanvasGroup>();
        var next=new[]{title,hub.GetComponent<CanvasGroup>(),business.GetComponent<CanvasGroup>(),settlement.GetComponent<CanvasGroup>(),gameOver.GetComponent<CanvasGroup>()};
        for(int i=0;i<next.Length;i++)screens.GetArrayElementAtIndex(i).objectReferenceValue=next[i];
        so.FindProperty("sharedHeader").objectReferenceValue=header.GetComponent<CanvasGroup>();so.ApplyModifiedProperties();
        // The previous screens remain as hidden controls (existing validations still drive their buttons).
        foreach(var group in old.Concat(new[]{oldHeader}).Where(g=>g!=null && !next.Contains(g)))
        {Undo.RecordObject(group,"Hide old screen");group.alpha=0;group.interactable=false;group.blocksRaycasts=false;}
        foreach(var group in next.Skip(1)){Undo.RecordObject(group,"Hide until shown");group.alpha=0;group.interactable=false;group.blocksRaycasts=false;}
        var viewport=UnityEngine.Object.FindFirstObjectByType<MapViewportUI>();Bind(viewport,"viewport",mapArea);
        var businessUi=go.GetComponent<BusinessScreenUI>();var b=new SerializedObject(businessUi);b.FindProperty("planningZoom").floatValue=1;b.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(go.scene);AssetDatabase.SaveAssets();
    }
    private static RectTransform Rect(Transform parent,string name,bool reuse=false)
    {
        var existing=parent.Find(name) as RectTransform;if(existing!=null)return existing;
        var go=new GameObject(name,typeof(RectTransform));go.layer=5;var r=(RectTransform)go.transform;r.SetParent(parent,false);
        r.anchorMin=Vector2.zero;r.anchorMax=Vector2.one;r.offsetMin=r.offsetMax=Vector2.zero;Undo.RegisterCreatedObjectUndo(go,"Shop UI "+name);return r;
    }
    private static T View<T>(Transform parent,string name) where T:Component
    {
        var r=Rect(parent,name);if(r.GetComponent<CanvasGroup>()==null)r.gameObject.AddComponent<CanvasGroup>();
        var view=r.GetComponent<T>();if(view==null)view=r.gameObject.AddComponent<T>();return view;
    }
    private static void ImportArt()
    {
        AssetDatabase.Refresh();
        foreach(var guid in AssetDatabase.FindAssets("t:Texture2D",new[]{"Assets/Resources/ShopArt"}))
        {
            var path=AssetDatabase.GUIDToAssetPath(guid);var importer=(TextureImporter)AssetImporter.GetAtPath(path);bool tile=path.EndsWith("floor_tile.png");
            if(importer.textureType==TextureImporterType.Sprite && importer.mipmapEnabled==false && importer.textureCompression==TextureImporterCompression.Uncompressed && (importer.wrapMode==TextureWrapMode.Repeat)==tile)continue;
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.filterMode=FilterMode.Bilinear;importer.spritePixelsPerUnit=100;importer.wrapMode=tile?TextureWrapMode.Repeat:TextureWrapMode.Clamp;
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }
    private static TMPro.TMP_FontAsset FontAsset(string ttf,string asset,string name)
    {
        var existing=AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(asset);if(existing!=null)return existing;
        var font=AssetDatabase.LoadAssetAtPath<Font>(ttf);if(font==null)throw new InvalidOperationException("Font missing: "+ttf);
        var tmp=TMPro.TMP_FontAsset.CreateFontAsset(font,64,6,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,1024,1024,TMPro.AtlasPopulationMode.Dynamic,true);
        tmp.isMultiAtlasTexturesEnabled=true;tmp.name=name;AssetDatabase.CreateAsset(tmp,asset);
        foreach(var atlas in tmp.atlasTextures){atlas.name=name+" Atlas";AssetDatabase.AddObjectToAsset(atlas,tmp);}AssetDatabase.AddObjectToAsset(tmp.material,tmp);
        return tmp;
    }
    private static void Fonts(UITheme theme)
    {
        AssetDatabase.Refresh();
        var display=FontAsset("Assets/Fonts/MochiyPopOne-Regular.ttf","Assets/Fonts/MochiyPopOne Dynamic.asset","Mochiy Pop One Dynamic");
        var bold=FontAsset("Assets/Fonts/ZenMaruGothic-Bold.ttf","Assets/Fonts/ZenMaruGothic Bold Dynamic.asset","Zen Maru Gothic Bold Dynamic");
        var black=FontAsset("Assets/Fonts/ZenMaruGothic-Black.ttf","Assets/Fonts/ZenMaruGothic Black Dynamic.asset","Zen Maru Gothic Black Dynamic");
        // Missing glyphs fall back to the Zen Maru family.
        display.fallbackFontAssetTable=new System.Collections.Generic.List<TMPro.TMP_FontAsset>{black,theme.font};
        bold.fallbackFontAssetTable=new System.Collections.Generic.List<TMPro.TMP_FontAsset>{theme.font};
        black.fallbackFontAssetTable=new System.Collections.Generic.List<TMPro.TMP_FontAsset>{bold,theme.font};
        EditorUtility.SetDirty(display);EditorUtility.SetDirty(bold);EditorUtility.SetDirty(black);
        var t=new SerializedObject(theme);t.FindProperty("displayFont").objectReferenceValue=display;t.FindProperty("boldFont").objectReferenceValue=bold;t.FindProperty("blackFont").objectReferenceValue=black;t.ApplyModifiedProperties();
    }
    private static void Theme(UITheme theme)
    {
        // The older screens that remain (title, menu, help) use the same v2 palette.
        var t=new SerializedObject(theme);
        void C(string name,string hex){ColorUtility.TryParseHtmlString(hex,out var c);t.FindProperty(name).colorValue=c;}
        C("background","#FFF6E5");C("ink","#3A2A20");C("accent","#E04B2D");C("good","#4C9A52");C("money","#F7BE3D");C("bad","#B3261E");C("muted","#7A6652");C("block","#E7E1D3");C("park","#C9E3B4");
        var colors=t.FindProperty("courierColors");string[] palette={"#3C79C2","#4C9A52","#8E5BB5","#E0812D","#2E8F8A","#C2477A"};colors.arraySize=palette.Length;
        for(int i=0;i<palette.Length;i++){ColorUtility.TryParseHtmlString(palette[i],out var c);colors.GetArrayElementAtIndex(i).colorValue=c;}
        t.FindProperty("mapMainRoad").floatValue=.26f;t.FindProperty("mapBackRoad").floatValue=.14f;t.FindProperty("mapRoute").floatValue=.22f;t.FindProperty("mapRoadEdge").floatValue=.05f;
        t.ApplyModifiedProperties();EditorUtility.SetDirty(theme);
    }
}
